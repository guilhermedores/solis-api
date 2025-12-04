# Módulo **Vendas + Fiscal** — API (.NET 10 + PostgreSQL)

**Visão geral**

Documento que descreve a implementação do módulo _Vendas + Fiscal_ para a API já existente em **.NET 10** com **PostgreSQL** como banco. Contém: modelagem (DDL), endpoints (contrato), regras de cálculo fiscal, fluxo de sincronização offline, idempotência, testes, observabilidade e critérios de aceitação.

> **Contexto**: o projeto .NET 10 já existe — este documento descreve a feature a ser adicionada e os artefatos necessários (migrations, OpenAPI, seed e testes).

---

## 1. Tabelas (resumo lógico)

Tabelas necessárias:

- `sales` (header da venda)
- `sale_items` (itens)
- `sale_payments` (pagamentos)
- `sale_taxes` (impostos aplicados por item)
- `sale_cancellations` (registro de cancelamentos)
- `tax_types` (domínio de tipos de impostos) - CRUD Generico
- `tax_rules` (regras por UF / produto / vigência) - CRUD Generico

Detalhes de cada tabela (campos essenciais):

### `sales`
- `sale_id` UUID PK (gen_random_uuid())
- `client_sale_id` UUID NULL
- `store_id` UUID NOT NULL
- `pos_id` UUID NULL
- `operator_id` UUID NULL
- `sale_datetime` timestamptz NOT NULL
- `status` varchar NOT NULL DEFAULT 'pending'
- `subtotal` numeric(12,2) NOT NULL
- `discount_total` numeric(12,2) NOT NULL
- `tax_total` numeric(12,2) NOT NULL
- `total` numeric(12,2) NOT NULL
- `payment_status` varchar NOT NULL DEFAULT 'unpaid'
- `created_at` timestamptz DEFAULT now()
- `updated_at` timestamptz DEFAULT now()

Índices recomendados: `sale_datetime`, `(store_id, sale_datetime)`, `client_sale_id`.

### `sale_items`
- `sale_item_id` UUID PK
- `sale_id` UUID FK -> sales
- `product_id` UUID NOT NULL
- `sku` varchar NULL
- `description` text NULL
- `quantity` numeric(12,4) NOT NULL
- `unit_price` numeric(12,4) NOT NULL
- `discount_amount` numeric(12,2) DEFAULT 0
- `tax_amount` numeric(12,2) DEFAULT 0
- `total` numeric(12,2) NOT NULL
- `created_at` timestamptz DEFAULT now()

### `sale_payments`
- `payment_id` UUID PK
- `sale_id` UUID FK -> sales
- `payment_type` varchar -- e.g. cash, card, pix, voucher
- `amount` numeric(12,2)
- `acquirer_txn_id` varchar NULL
- `authorization_code` varchar NULL
- `change_amount` numeric(12,2) NULL
- `status` varchar DEFAULT 'processed'
- `processed_at` timestamptz NULL
- `created_at` timestamptz DEFAULT now()

### `sale_taxes`
- `tax_id` UUID PK
- `sale_item_id` UUID FK -> sale_items
- `tax_type_id` UUID FK -> tax_types
- `tax_rule_id` UUID FK -> tax_rules NULL
- `base_amount` numeric(12,2) NOT NULL
- `rate` numeric(10,4) NOT NULL
- `amount` numeric(12,2) NOT NULL
- `created_at` timestamptz DEFAULT now()

### `sale_cancellations`
- `cancellation_id` UUID PK
- `sale_id` UUID FK -> sales
- `operator_id` UUID NULL
- `reason` text NULL
- `canceled_at` timestamptz DEFAULT now()
- `source` varchar NULL -- api, pos, system
- `cancellation_type` varchar DEFAULT 'total' -- total, partial
- `refund_amount` numeric(12,2) NULL
- `payment_reversal_id` UUID NULL

### `tax_types`
- `tax_type_id` UUID PK
- `code` varchar(20) UNIQUE NOT NULL -- ICMS, PIS, COFINS, ISS, IPI
- `description` varchar(255)
- `category` varchar(50) -- federal/estadual/municipal
- `calculation_type` varchar NOT NULL -- percentage, fixed, mva, reduced_base
- `created_at`, `updated_at`

### `tax_rules`
- `tax_rule_id` UUID PK
- `tax_type_id` UUID FK -> tax_types
- `state` char(2) NOT NULL
- `product_id` UUID NULL (NULL = regra genérica por estado)
- `cst_code` varchar NULL
- `rate` numeric(10,4) NOT NULL
- `base_modality` varchar NULL
- `base_reduction_rate` numeric(10,4) NULL
- `mva_rate` numeric(10,4) NULL
- `active_from` date NOT NULL
- `active_to` date NULL
- índices: `(state, product_id, active_from)`

---

## 2. Migrations / DDL

**Recomendação de ferramenta:** usar *EF Core Migrations* (compatível com .NET 10) ou Flyway/DbUp se preferir scripts SQL gerenciados. O DDL deve incluir: FKs, `ON DELETE CASCADE` para itens, constraints básicas e índices.

**Notas práticas:**
- Ative a extensão `pgcrypto` para `gen_random_uuid()`.
- Evite `CHECK` complexos que bloqueiem migrações futuras — valide soma/aritmética na aplicação e implemente triggers apenas se necessário.

---

## 3. Contrato de API (endpoints principais)

Base: `/api`

- `POST /sales`
  - Cabeçalhos: `Authorization: Bearer <token>`, `Idempotency-Key: <uuid>`
  - Payload: header + items + payments opcionais + `client_sale_id` (para sync)
  - Comportamento: valida, aplica regras fiscais (se necessário), grava tudo na mesma transação e retorna `201` com `sale_id`.

- `POST /sales/sync`
  - Batch de vendas (array). Suporta resposta `207 Multi-Status` com resultado por venda. Idempotência por `client_sale_id`.

- `GET /sales` — filtros: `store_id`, `pos_id`, `operator_id`, `date_from`, `date_to`, `status`, `client_sale_id`. Paginação cursor-based.

- `GET /sales/{saleId}` — details (header + items + payments + taxes + cancellation)

- `PATCH /sales/{saleId}` — atualizações permitidas (ex.: endereço, status em transições válidas)

- `POST /sales/{saleId}/payments` — adiciona pagamento; quando total pagos >= total -> `payment_status = paid`.

- `POST /sales/{saleId}/cancel` — registra cancelamento em `sale_cancellations`, altera `status = canceled`, publica evento.

- CRUD GENERICO Admin: `/tax_types` e `/tax_rules` (GET/POST/PUT/DELETE)

---

## 4. Lógica de cálculo de impostos (resumo)

1. Para cada item, busque `tax_rules` aplicável por prioridade:
   - `product_id + state + vigência` (mais específico)
   - `state + vigência` (genérico)
   - fallback: alíquota 0 (log/alerta)

2. Aplique `base_reduction_rate` e `mva_rate` quando aplicável. Pseudocálculo simplificado:

```
base = unit_price * quantity
if base_reduction_rate: base = base * (1 - base_reduction_rate)
if mva_rate: base_st = base * (1 + mva_rate)
tax_amount = base_or_base_st * rate
```

3. Persista cada imposto em `sale_taxes` referenciando `tax_rule_id` e `tax_type_id`.
4. Some `tax_amount` por item para preencher `sale_items.tax_amount` e some por venda para `sales.tax_total`.

**Observação:** implemente funções separadas por `calculation_type` para facilitar cobertura de testes e futuras regras complexas.

---

## 5. Idempotência e sync offline

- `Idempotency-Key` obrigatório para `POST /sales` e `POST /sales/{id}/payments` quando possível.
- `client_sale_id` (UUID gerado pelo PDV) enviado na criação permite reconciliar e evitar duplicação em sync.
- Endpoint `GET /sales/sync/status?client_sale_id=<uuid>` para checar mapeamento.
- `POST /sales/sync` aceita lote e retorna `207` com resultados individuais (created/ignored/conflict/error).

---

## 6. Transações e integração com adquirente

- Criar venda, itens e impostos em uma **única transação DB**.
- Para pagamentos via adquirente: preferir fluxo async:
  1. Cria `sale` com `status = pending` ou `payment_status = unpaid`.
  2. Publica evento `payment.requested` (ou chama gateway).
  3. Atualiza via callback do gateway (webhook) o `sale_payments` e `payment_status`.

---

## 7. Observabilidade e métricas

- Aceitar/propagar `X-Request-Id` (correlation id).
- Logs estruturados com fields: `sale_id`, `client_sale_id`, `store_id`, `operator_id`, `request_id`.
- Métricas: `sales_per_minute`, `sync_errors`, `avg_post_sales_latency`.
- Tracing (OpenTelemetry) recomendado.

---

## 8. Segurança e autorização

- padrão ja existente no sistema

---

## 10. Seed data (sugestão)

Inserir seed básico (script) para facilitar QA/dev:

- `tax_types`: ICMS, PIS, COFINS, ISS, IPI
- `tax_rules` de exemplo (ex.: ICMS MG, PIS 1.65% genérico)

---

## 11. Critérios de aceitação

1. Seeds e tabelas genericas atualizados.
2. Endpoints implementados conforme contrato e documentados (OpenAPI YAML/JSON).
3. Cálculo fiscal aplicado e auditado em `sale_taxes`.
4. Idempotency e `client_sale_id` funcionando.