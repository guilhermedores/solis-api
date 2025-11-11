# Scripts Utilitários - Solis API

## create-admin-user.js

Script para criar um usuário administrador em um tenant específico.

### Como usar

Execute o script passando os parâmetros via linha de comando:

```bash
npm run create-admin <tenant> <email> <senha> [nome] [role]
```

### Parâmetros

- **tenant** (obrigatório) - Subdomain do tenant (ex: `demo`, `empresa1`)
- **email** (obrigatório) - Email do usuário
- **senha** (obrigatório) - Senha (mínimo 6 caracteres)
- **nome** (opcional) - Nome completo do usuário (padrão: "Administrador")
- **role** (opcional) - Nível de acesso: `admin`, `manager` ou `operator` (padrão: `admin`)

### Exemplos

```bash
# Criar admin básico
npm run create-admin demo admin@demo.com senha123

# Criar com nome personalizado
npm run create-admin demo admin@demo.com senha123 "João Silva"

# Criar manager
npm run create-admin empresa1 gerente@empresa.com senha456 "Maria Santos" manager

# Criar operator
npm run create-admin loja1 operador@loja.com senha789 "Pedro Costa" operator
```

### O que o script faz

1. ✅ Valida os parâmetros fornecidos
2. ✅ Verifica e garante que o schema do tenant está atualizado
3. ✅ Conecta ao tenant especificado
4. ✅ Verifica se o email já existe
5. ✅ Gera hash seguro da senha usando bcrypt
6. ✅ Cria o usuário no banco de dados
7. ✅ Exibe as credenciais de acesso

### Exemplo de saída

```
======================================================================
Script de Criação de Usuário Admin
======================================================================

Configuração:
   Tenant: demo
   Nome:   João Silva
   Email:  admin@demo.com
   Role:   admin

🔍 Verificando schema do tenant "demo"...
📦 Garantindo que o schema "tenant_demo" existe e está atualizado...
⏳ Executando migrations...
✅ Schema "tenant_demo" verificado e atualizado

⏳ Conectando ao tenant "demo"...
✅ Conectado ao banco de dados

🔍 Verificando se o email "admin@demo.com" já existe...
✅ Email disponível

🔐 Gerando hash da senha...
✅ Hash gerado com sucesso

💾 Criando usuário no banco de dados...
✅ Usuário criado com sucesso!

======================================================================
📋 Dados do usuário criado:
======================================================================
ID:        07a37226-fb23-486b-a851-739f2ef136eb
Nome:      João Silva
Email:     admin@demo.com
Role:      admin
Ativo:     Sim
Criado em: 2025-11-11T15:30:45.123Z
======================================================================

🎉 Processo concluído com sucesso!

🔑 Credenciais para login:
   Tenant: demo
   Email:  admin@demo.com
   Senha:  senha123

💡 Lembre-se de alterar a senha após o primeiro login!
```

### Níveis de permissão (roles)

- **admin** - Acesso total ao sistema
- **manager** - Gerenciamento de usuários e relatórios
- **operator** - Operação do PDV (padrão)

### Troubleshooting

**Erro: "Argumentos insuficientes"**
- Verifique se você passou pelo menos tenant, email e senha
- Exemplo correto: `npm run create-admin demo admin@demo.com senha123`

**Erro: "Email inválido"**
- O email deve estar no formato válido (ex: usuario@dominio.com)

**Erro: "Senha deve ter no mínimo 6 caracteres"**
- Use uma senha com pelo menos 6 caracteres

**Erro: "Role inválida"**
- Use apenas: `admin`, `manager` ou `operator`

**Erro: "Usuário já existe"**
- O email já está cadastrado no tenant
- Use outro email ou delete o usuário existente primeiro

**Erro: "Schema do tenant pode não existir"**
- Execute manualmente: `npx prisma migrate deploy`
- Verifique se o banco de dados está acessível

### Segurança

⚠️ **IMPORTANTE**: 
- Altere a senha padrão após o primeiro login
- Não commite senhas em texto plano no código
- Use senhas fortes em produção
