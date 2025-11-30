# Docker Setup - Solis API

## Pré-requisitos

- Docker Desktop instalado
- Docker Compose instalado

## Iniciando o banco de dados

### 1. Iniciar o PostgreSQL

```powershell
docker-compose up -d
```

### 2. Verificar status

```powershell
docker-compose ps
```

### 3. Ver logs do PostgreSQL

```powershell
docker-compose logs -f postgres
```

### 4. Parar o banco de dados

```powershell
docker-compose down
```

### 5. Parar e remover volumes (apaga dados)

```powershell
docker-compose down -v
```

## Conectando à API

O banco de dados estará disponível em:
- **Host**: localhost
- **Port**: 5432
- **Database**: solis_pdv
- **Username**: solis_user
- **Password**: solis2024

A string de conexão já está configurada no `appsettings.Development.json`.

## Executando scripts de inicialização

Os scripts SQL na pasta `database/init/` são executados automaticamente na primeira vez que o container é criado, na ordem alfabética:

1. `01-init-database.sql` ou `01-init-multitenant.sql`
2. `02-token-vinculacao.sql`
3. `03-empresas.sql`

## Comandos úteis

### Acessar o PostgreSQL via psql

```powershell
docker exec -it solis-postgres psql -U solis_user -d solis_pdv
```

### Backup do banco de dados

```powershell
docker exec solis-postgres pg_dump -U solis_user solis_pdv > backup.sql
```

### Restaurar backup

```powershell
docker exec -i solis-postgres psql -U solis_user -d solis_pdv < backup.sql
```

### Ver uso de espaço do volume

```powershell
docker volume inspect solis-api_postgres-data
```

## Troubleshooting

### Erro: "port is already allocated"

Se a porta 5432 já estiver em uso, você pode:

1. Parar o PostgreSQL local:
```powershell
Stop-Service postgresql-x64-16
```

2. Ou alterar a porta no docker-compose.yml:
```yaml
ports:
  - "5433:5432"
```

E atualizar a connection string:
```
Host=localhost;Port=5433;Database=solis_pdv;...
```

### Resetar o banco completamente

```powershell
docker-compose down -v
docker-compose up -d
```

Isso irá apagar todos os dados e recriar o banco do zero.
