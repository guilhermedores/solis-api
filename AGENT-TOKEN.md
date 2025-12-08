# Endpoint de Geração de Token para Agente PDV

## Descrição
Endpoint para gerar tokens JWT de longa duração (10 anos) que vinculam agentes PDV a uma **loja (store)** específica dentro de um tenant.

**Hierarquia**: `Tenant → Empresa → Store → Agente PDV`

Cada agente PDV representa uma loja específica, permitindo:
- **Rastreabilidade**: Identificar qual loja realizou cada venda
- **Relatórios**: Análises e relatórios por loja
- **Segurança**: Revogar acesso de uma loja sem afetar outras
- **Configurações**: Configurações específicas por loja

## Endpoint

```
POST /api/auth/generate-agent-token
```

## Headers Obrigatórios

```
X-Tenant-Subdomain: {subdomain-do-tenant}
Content-Type: application/json
```

## Request Body

```json
{
  "storeId": "uuid-da-loja",
  "agentName": "Nome do Agente PDV"
}
```

### Campos

- `storeId` (Guid, obrigatório): ID da loja à qual o agente será vinculado
- `agentName` (string, obrigatório): Nome identificador do agente PDV (ex: "PDV Loja Centro")

## Response

### Sucesso (200 OK)

```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "tenantId": "uuid-do-tenant",
  "tenant": "subdomain-do-tenant",
  "storeId": "uuid-da-loja",
  "agentName": "Nome do Agente PDV",
  "expiresAt": "2035-12-08T10:30:00Z"
}
```

### Erros

#### 400 Bad Request
```json
{
  "success": false,
  "error": "Header X-Tenant-Subdomain é obrigatório"
}
```

```json
{
  "success": false,
  "error": "StoreId é obrigatório"
}
```

```json
{
  "success": false,
  "error": "AgentName é obrigatório"
}
```

#### 404 Not Found
```json
{
  "success": false,
  "error": "Tenant não encontrado"
}
```

```json
{
  "success": false,
  "error": "Loja não encontrada no tenant especificado"
}
```

#### 500 Internal Server Error
```json
{
  "success": false,
  "error": "Erro ao gerar token: {detalhes}"
}
```

## Claims do Token JWT

O token gerado contém as seguintes claims:

- `userId`: `00000000-0000-0000-0000-000000000000` (Guid.Empty - agente não é usuário)
- `empresaId`: `00000000-0000-0000-0000-000000000000` (Guid.Empty - determinado pela store)
- `storeId`: UUID da loja (store)
- `tenantId`: UUID do tenant
- `tenant`: Subdomain do tenant
- `role`: "agent"
- `type`: "agent"
- `agentName`: Nome do agente PDV
- `exp`: Timestamp Unix de expiração (10 anos)

## Exemplo de Uso

### cURL

```bash
curl -X POST https://api.projetosolis.com.br/api/auth/generate-agent-token \
  -H "X-Tenant-Subdomain: meucliente" \
  -H "Content-Type: application/json" \
  -d '{
    "storeId": "550e8400-e29b-41d4-a716-446655440000",
    "agentName": "PDV Loja Centro"
  }'
```

### JavaScript/Fetch

```javascript
const response = await fetch('https://api.projetosolis.com.br/api/auth/generate-agent-token', {
  method: 'POST',
  headers: {
    'X-Tenant-Subdomain': 'meucliente',
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    storeId: '550e8400-e29b-41d4-a716-446655440000',
    agentName: 'PDV Loja Centro'
  })
});

const data = await response.json();
console.log('Token:', data.token);
```

### C# (.NET)

```csharp
using System.Net.Http.Json;

var client = new HttpClient();
client.DefaultRequestHeaders.Add("X-Tenant-Subdomain", "meucliente");

var request = new
{
    StoreId = Guid.Parse("550e8400-e29b-41d4-a716-446655440000"),
    AgentName = "PDV Loja Centro"
};

var response = await client.PostAsJsonAsync(
    "https://api.projetosolis.com.br/api/auth/generate-agent-token",
    request
);

if (response.IsSuccessStatusCode)
{
    var result = await response.Content.ReadFromJsonAsync<GenerateAgentTokenResponse>();
    Console.WriteLine($"Token: {result.Token}");
    Console.WriteLine($"StoreId: {result.StoreId}");
}
```

## Integração com o Agente PDV

O agente PDV espera receber esse token e armazená-lo localmente usando o método `SalvarTokenAsync` do `ConfiguracaoService`.

### Fluxo de Configuração do Agente

1. Usuário acessa a interface de administração
2. Seleciona a **loja (store)** que o agente representará
3. Informa o nome do agente
4. Sistema chama o endpoint `/api/auth/generate-agent-token`
5. Token retornado é enviado para o agente PDV
6. Agente armazena o token no banco local SQLite com os seguintes campos:
   - `Token`: Token JWT completo
   - `TenantId`: ID do tenant
   - `Tenant`: Subdomain do tenant
   - `StoreId`: **ID da loja vinculada**
   - `EmpresaId`: ID da empresa (se disponível)
   - `NomeAgente`: Nome do agente
   - `TokenValidoAte`: Data de expiração
7. Token é usado em todas as requisições subsequentes à API

## Validação do Token

O token pode ser validado usando o middleware `UseJwtAuth()` já existente na API. O middleware verificará:

- Assinatura do token
- Data de expiração
- Claims obrigatórias (tenantId, storeId, etc.)

## Segurança

⚠️ **IMPORTANTE**:
- Este token tem validade de 10 anos
- O token vincula o agente permanentemente a uma **loja específica**
- Não há autenticação de usuário - o token identifica apenas o agente e a loja
- Proteja o token adequadamente no agente PDV
- Em caso de comprometimento, será necessário gerar um novo token

## Diferença entre Token de Usuário e Token de Agente

| Característica | Token de Usuário | Token de Agente |
|----------------|------------------|-----------------|
| Validade | 30 dias | 10 anos |
| `userId` | ID do usuário real | Guid.Empty |
| `empresaId` | ID da empresa do usuário | Guid.Empty |
| `storeId` | Pode não ter | **ID da loja vinculada** |
| `type` | "user" | "agent" |
| `role` | admin/manager/operator | "agent" |
| Claim `agentName` | Não presente | Nome do agente |
| Uso | Autenticação de usuários | Vinculação de dispositivos PDV |
| Granularidade | Por usuário | **Por loja** |

## Vantagens do Token por Store

✅ **Rastreabilidade**: Todas as vendas ficam vinculadas à loja específica  
✅ **Segurança**: Revogar token de uma loja não afeta outras lojas  
✅ **Auditoria**: Saber exatamente qual loja realizou cada operação  
✅ **Configurações**: Cada loja pode ter suas próprias configurações  
✅ **Relatórios**: Análises precisas por ponto de venda  
✅ **Escalabilidade**: Adicionar novos PDVs sem afetar os existentes  

## Manutenção

Para renovar um token de agente expirado ou comprometido:
1. Gere um novo token usando o mesmo endpoint com o `storeId` correspondente
2. Configure o novo token no agente PDV
3. O token antigo será invalidado automaticamente pela data de expiração

## Estrutura de Dados no Agente

O modelo `Configuracao` no agente armazena:

```csharp
public class Configuracao
{
    public string? Token { get; set; }              // Token JWT completo
    public string? TenantId { get; set; }           // ID do tenant
    public string? Tenant { get; set; }             // Subdomain do tenant
    public string? StoreId { get; set; }            // ID da loja (NOVO)
    public string? EmpresaId { get; set; }          // ID da empresa (compatibilidade)
    public string? NomeAgente { get; set; }         // Nome do agente PDV
    public DateTime? TokenValidoAte { get; set; }   // Data de expiração
}
```
