-- CreateSchema
CREATE SCHEMA IF NOT EXISTS "tenant_demo";

-- CreateTable
CREATE TABLE "public"."tenants" (
    "id" UUID NOT NULL,
    "subdomain" VARCHAR(100) NOT NULL,
    "company_name" VARCHAR(255) NOT NULL,
    "cnpj" VARCHAR(18),
    "active" BOOLEAN NOT NULL DEFAULT true,
    "plan" VARCHAR(50) NOT NULL DEFAULT 'basic',
    "max_terminals" INTEGER NOT NULL DEFAULT 1,
    "max_users" INTEGER NOT NULL DEFAULT 5,
    "features" JSONB NOT NULL DEFAULT '{}',
    "created_at" TIMESTAMP(3) NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "updated_at" TIMESTAMP(3) NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "deleted_at" TIMESTAMP(3),

    CONSTRAINT "tenants_pkey" PRIMARY KEY ("id")
);

-- CreateTable
CREATE TABLE "tenant_demo"."usuarios" (
    "id" UUID NOT NULL,
    "nome" VARCHAR(255) NOT NULL,
    "email" VARCHAR(255) NOT NULL,
    "password_hash" VARCHAR(255) NOT NULL,
    "role" VARCHAR(50) NOT NULL DEFAULT 'operator',
    "ativo" BOOLEAN NOT NULL DEFAULT true,
    "created_at" TIMESTAMP(3) NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "updated_at" TIMESTAMP(3) NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT "usuarios_pkey" PRIMARY KEY ("id")
);

-- CreateTable
CREATE TABLE "tenant_demo"."empresas" (
    "id" TEXT NOT NULL,
    "razao_social" VARCHAR(255) NOT NULL,
    "nome_fantasia" VARCHAR(255),
    "cnpj" VARCHAR(18) NOT NULL,
    "inscricao_estadual" VARCHAR(20),
    "inscricao_municipal" VARCHAR(20),
    "logradouro" VARCHAR(255) NOT NULL,
    "numero" VARCHAR(20) NOT NULL,
    "complemento" VARCHAR(100),
    "bairro" VARCHAR(100) NOT NULL,
    "cidade" VARCHAR(100) NOT NULL,
    "uf" VARCHAR(2) NOT NULL,
    "cep" VARCHAR(9) NOT NULL,
    "telefone" VARCHAR(20),
    "email" VARCHAR(255),
    "site" VARCHAR(255),
    "regime_tributario" VARCHAR(50) NOT NULL,
    "certificado_digital" TEXT,
    "senha_certificado" VARCHAR(255),
    "ambiente_fiscal" VARCHAR(20) NOT NULL DEFAULT 'homologacao',
    "logo" TEXT,
    "ativo" BOOLEAN NOT NULL DEFAULT true,
    "created_at" TIMESTAMP(3) NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "updated_at" TIMESTAMP(3) NOT NULL,

    CONSTRAINT "empresas_pkey" PRIMARY KEY ("id")
);

-- CreateTable
CREATE TABLE "tenant_demo"."produtos" (
    "id" TEXT NOT NULL,
    "codigo_barras" TEXT,
    "codigo_interno" TEXT,
    "nome" TEXT NOT NULL,
    "descricao" TEXT,
    "unidade_medida" TEXT NOT NULL,
    "categoria" TEXT,
    "ncm" TEXT,
    "cest" TEXT,
    "cfop" TEXT,
    "ativo" BOOLEAN NOT NULL DEFAULT true,
    "created_at" TIMESTAMP(3) NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "updated_at" TIMESTAMP(3) NOT NULL,

    CONSTRAINT "produtos_pkey" PRIMARY KEY ("id")
);

-- CreateTable
CREATE TABLE "tenant_demo"."produtos_precos" (
    "id" TEXT NOT NULL,
    "produto_id" TEXT NOT NULL,
    "preco_venda" DOUBLE PRECISION NOT NULL,
    "preco_custo" DOUBLE PRECISION,
    "ativo" BOOLEAN NOT NULL DEFAULT true,
    "created_at" TIMESTAMP(3) NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "updated_at" TIMESTAMP(3) NOT NULL,

    CONSTRAINT "produtos_precos_pkey" PRIMARY KEY ("id")
);

-- CreateIndex
CREATE UNIQUE INDEX "tenants_subdomain_key" ON "public"."tenants"("subdomain");

-- CreateIndex
CREATE UNIQUE INDEX "tenants_cnpj_key" ON "public"."tenants"("cnpj");

-- CreateIndex
CREATE INDEX "tenants_subdomain_active_idx" ON "public"."tenants"("subdomain", "active");

-- CreateIndex
CREATE INDEX "tenants_active_idx" ON "public"."tenants"("active");

-- CreateIndex
CREATE UNIQUE INDEX "usuarios_email_key" ON "tenant_demo"."usuarios"("email");

-- CreateIndex
CREATE INDEX "usuarios_email_idx" ON "tenant_demo"."usuarios"("email");

-- CreateIndex
CREATE INDEX "usuarios_ativo_idx" ON "tenant_demo"."usuarios"("ativo");

-- CreateIndex
CREATE INDEX "empresas_cnpj_idx" ON "tenant_demo"."empresas"("cnpj");

-- CreateIndex
CREATE INDEX "empresas_ativo_idx" ON "tenant_demo"."empresas"("ativo");

-- CreateIndex
CREATE INDEX "produtos_codigo_barras_idx" ON "tenant_demo"."produtos"("codigo_barras");

-- CreateIndex
CREATE INDEX "produtos_codigo_interno_idx" ON "tenant_demo"."produtos"("codigo_interno");

-- CreateIndex
CREATE INDEX "produtos_nome_idx" ON "tenant_demo"."produtos"("nome");

-- CreateIndex
CREATE INDEX "produtos_precos_produto_id_idx" ON "tenant_demo"."produtos_precos"("produto_id");

-- AddForeignKey
ALTER TABLE "tenant_demo"."produtos_precos" ADD CONSTRAINT "produtos_precos_produto_id_fkey" FOREIGN KEY ("produto_id") REFERENCES "tenant_demo"."produtos"("id") ON DELETE CASCADE ON UPDATE CASCADE;
