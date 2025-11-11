# 🛠️ Scripts Utilitários

A API possui scripts para facilitar tarefas administrativas.

## Criar Usuário Admin

Para criar um usuário administrador em um tenant:

```bash
npm run create-admin <tenant>
```

### Parâmetros

- **tenant** - Subdomain do tenant (obrigatório)

### Exemplos

```bash
# Admin básico
npm run create-admin demo
```

### O que o script faz

1. ✅ Valida os parâmetros
2. ✅ Garante que o schema do tenant está atualizado (migrations)
3. ✅ Verifica se o email já existe
4. ✅ Cria o usuário com senha criptografada

### Roles disponíveis

- **admin** - Acesso total ao sistema
- **manager** - Gerenciamento de produtos, usuários e relatórios  
- **operator** - Operação básica do PDV

### Documentação completa

Veja [scripts/README.md](./scripts/README.md) para mais detalhes e exemplos.

---

## Outros scripts disponíveis

Consulte a pasta `scripts/` para mais utilitários de administração.
