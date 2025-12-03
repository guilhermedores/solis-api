# Sistema de Relatórios Genéricos - Solis API

## Visão Geral

O sistema de relatórios genéricos permite criar relatórios configuráveis através do banco de dados, sem necessidade de codificar cada relatório individualmente. Similar ao CRUD genérico, os relatórios são definidos através de metadata no banco.

## Estrutura de Tabelas

### `reports`
Define o relatório principal:
- `name`: Identificador único (slug)
- `display_name`: Nome amigável para exibição
- `description`: Descrição do relatório
- `category`: Categoria para organização
- `base_table`: Tabela principal ou nome da view
- `base_query`: SQL query customizada (opcional, substitui base_table)
- `active`: Se o relatório está ativo

### `report_fields`
Define os campos que aparecem no relatório:
- `name`: Nome do campo na resposta
- `display_name`: Label para exibição
- `field_type`: Tipo de dado (string, number, decimal, date, datetime, boolean, uuid)
- `data_source`: Expressão SQL do campo (ex: `p.description` ou `SUM(amount)`)
- `format_mask`: Máscara de formatação (ex: `R$ 0.00`)
- `aggregation`: Função de agregação (sum, avg, count, min, max)
- `visible`: Se aparece no resultado
- `sortable`: Se permite ordenação
- `filterable`: Se permite filtro

### `report_filters`
Define os filtros disponíveis:
- `name`: Nome do filtro
- `display_name`: Label para exibição
- `field_type`: Tipo do campo (string, number, date, select, etc)
- `filter_type`: Tipo de comparação (equals, contains, greater_than, between, in, etc)
- `data_source`: Campo SQL a ser filtrado
- `default_value`: Valor padrão
- `required`: Se é obrigatório

### `report_filter_options`
Opções para filtros do tipo select:
- `filter_id`: ID do filtro pai
- `value`: Valor da opção
- `label`: Label para exibição

## Endpoints da API

### GET `/api/reports`
Lista todos os relatórios disponíveis.

**Query Params:**
- `category` (opcional): Filtrar por categoria

**Resposta:**
```json
{
  "reports": [
    {
      "name": "products_list",
      "displayName": "Relatório de Produtos",
      "description": "Lista completa de produtos...",
      "category": "Produtos"
    }
  ]
}
```

### GET `/api/reports/{reportName}/metadata`
Retorna metadata do relatório (campos e filtros).

**Resposta:**
```json
{
  "name": "products_list",
  "displayName": "Relatório de Produtos",
  "fields": [
    {
      "name": "code",
      "displayName": "Código",
      "fieldType": "string",
      "sortable": true,
      "filterable": true
    }
  ],
  "filters": [
    {
      "name": "active",
      "displayName": "Status",
      "fieldType": "select",
      "filterType": "equals",
      "required": false,
      "options": [
        { "value": "true", "label": "Ativo" },
        { "value": "false", "label": "Inativo" }
      ]
    }
  ]
}
```

### POST `/api/reports/{reportName}/execute`
Executa o relatório com filtros.

**Request Body:**
```json
{
  "filters": {
    "active": "true",
    "description": "Produto",
    "created_date": {
      "from": "2025-01-01",
      "to": "2025-12-31"
    }
  },
  "page": 1,
  "pageSize": 50,
  "sortBy": "description",
  "sortDirection": "ASC"
}
```

**Resposta:**
```json
{
  "data": [
    {
      "code": "001",
      "description": "Produto A",
      "active": true,
      "brand_name": "Marca X"
    }
  ],
  "pagination": {
    "page": 1,
    "pageSize": 50,
    "totalRecords": 100,
    "totalPages": 2
  }
}
```

### POST `/api/reports/{reportName}/export`
Exporta o relatório para CSV.

**Request Body:** (mesmo formato do execute)

**Resposta:** Arquivo CSV para download

## Exemplos de Uso

### 1. Relatório Simples de Produtos

```sql
-- Criar relatório
INSERT INTO tenant_demo.reports (id, name, display_name, category, base_table, active)
VALUES (
    uuid_generate_v4(),
    'products_simple',
    'Lista de Produtos',
    'Produtos',
    'products',
    true
);

-- Adicionar campos
INSERT INTO tenant_demo.report_fields (report_id, name, display_name, field_type, data_source, display_order)
VALUES
    ((SELECT id FROM tenant_demo.reports WHERE name = 'products_simple'), 'description', 'Descrição', 'string', 'description', 1),
    ((SELECT id FROM tenant_demo.reports WHERE name = 'products_simple'), 'active', 'Ativo', 'boolean', 'active', 2);
```

### 2. Relatório com Query Customizada e JOINs

```sql
-- Criar relatório com query customizada
INSERT INTO tenant_demo.reports (id, name, display_name, category, base_query, active)
VALUES (
    uuid_generate_v4(),
    'sales_by_product',
    'Vendas por Produto',
    'Vendas',
    'SELECT p.description, COUNT(si.id) as qty_sold, SUM(si.total) as total_sales 
     FROM products p 
     JOIN sale_items si ON p.id = si.product_id 
     JOIN sales s ON si.sale_id = s.id 
     WHERE s.status = ''completed''
     GROUP BY p.id, p.description',
    true
);

-- Campos do relatório
INSERT INTO tenant_demo.report_fields (report_id, name, display_name, field_type, data_source, format_mask, display_order)
VALUES
    ((SELECT id FROM reports WHERE name = 'sales_by_product'), 'description', 'Produto', 'string', 'description', NULL, 1),
    ((SELECT id FROM reports WHERE name = 'sales_by_product'), 'qty_sold', 'Qtd Vendida', 'number', 'qty_sold', '#,##0', 2),
    ((SELECT id FROM reports WHERE name = 'sales_by_product'), 'total_sales', 'Total Vendas', 'decimal', 'total_sales', 'R$ #,##0.00', 3);
```

### 3. Filtros Avançados

```sql
-- Filtro de período (between)
INSERT INTO tenant_demo.report_filters (report_id, name, display_name, field_type, filter_type, data_source)
VALUES
    ((SELECT id FROM reports WHERE name = 'sales_by_product'), 'period', 'Período', 'date', 'between', 's.created_at');

-- Filtro de status (select com opções)
INSERT INTO tenant_demo.report_filters (report_id, name, display_name, field_type, filter_type, data_source)
VALUES
    ((SELECT id FROM reports WHERE name = 'orders_report'), 'status', 'Status', 'select', 'in', 'o.status');

INSERT INTO tenant_demo.report_filter_options (filter_id, value, label)
VALUES
    ((SELECT id FROM report_filters WHERE name = 'status'), 'pending', 'Pendente'),
    ((SELECT id FROM report_filters WHERE name = 'status'), 'completed', 'Concluído'),
    ((SELECT id FROM report_filters WHERE name = 'status'), 'cancelled', 'Cancelado');
```

## Tipos de Filtro Suportados

| Filter Type | Descrição | Exemplo |
|------------|-----------|---------|
| `equals` | Igualdade exata | `status = 'active'` |
| `not_equals` | Diferente de | `status <> 'cancelled'` |
| `contains` | Contém (case-insensitive) | `description ILIKE '%produto%'` |
| `starts_with` | Começa com | `code ILIKE 'A%'` |
| `ends_with` | Termina com | `code ILIKE '%001'` |
| `greater_than` | Maior que | `price > 100` |
| `less_than` | Menor que | `price < 50` |
| `between` | Entre dois valores | `date BETWEEN '2025-01-01' AND '2025-12-31'` |
| `in` | Dentro de uma lista | `status IN ('pending', 'completed')` |
| `is_null` | É nulo | `deleted_at IS NULL` |
| `is_not_null` | Não é nulo | `confirmed_at IS NOT NULL` |

## Agregações Suportadas

- `sum`: Soma
- `avg`: Média
- `count`: Contagem
- `min`: Mínimo
- `max`: Máximo

## Boas Práticas

1. **Performance**: Use índices nas colunas que serão filtradas frequentemente
2. **Segurança**: Nunca exponha queries raw diretamente ao frontend
3. **Paginação**: Sempre use paginação para relatórios grandes
4. **Cache**: Considere cachear relatórios que não mudam frequentemente
5. **Validação**: Valide os filtros no backend antes de executar
6. **Limite de Export**: Configure um limite máximo para exports (padrão: 10.000 registros)

## Frontend Integration Example

```typescript
// Listar relatórios
const reports = await fetch('/api/reports?category=Produtos', {
  headers: {
    'Authorization': `Bearer ${token}`,
    'X-Tenant-Subdomain': 'demo'
  }
}).then(r => r.json());

// Obter metadata
const metadata = await fetch('/api/reports/products_list/metadata', {
  headers: {
    'Authorization': `Bearer ${token}`,
    'X-Tenant-Subdomain': 'demo'
  }
}).then(r => r.json());

// Executar relatório
const result = await fetch('/api/reports/products_list/execute', {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${token}`,
    'X-Tenant-Subdomain': 'demo',
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    filters: {
      active: 'true',
      description: 'Produto'
    },
    page: 1,
    pageSize: 50,
    sortBy: 'description',
    sortDirection: 'ASC'
  })
}).then(r => r.json());

// Exportar para CSV
const blob = await fetch('/api/reports/products_list/export', {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${token}`,
    'X-Tenant-Subdomain': 'demo',
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    filters: { active: 'true' }
  })
}).then(r => r.blob());

// Download do arquivo
const url = window.URL.createObjectURL(blob);
const a = document.createElement('a');
a.href = url;
a.download = 'produtos.csv';
a.click();
```

## Próximos Passos

1. Adicionar suporte a gráficos (bar, line, pie)
2. Implementar agendamento de relatórios
3. Envio automático por email
4. Exportação para PDF e Excel
5. Relatórios com sub-totais e totalizadores
6. Dashboard com widgets de relatórios
