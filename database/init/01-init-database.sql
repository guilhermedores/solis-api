-- =============================================================================
-- SCRIPT DE INICIALIZAÇÃO DO BANCO DE DADOS SOLIS PDV
-- =============================================================================

-- Extensões necessárias
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pg_trgm";

-- =============================================================================
-- TABELAS DE AUTENTICAÇÃO E USUÁRIOS
-- =============================================================================

-- Usuários do sistema
CREATE TABLE usuarios (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    nome VARCHAR(100) NOT NULL,
    email VARCHAR(150) UNIQUE NOT NULL,
    senha_hash VARCHAR(255) NOT NULL,
    perfil VARCHAR(20) DEFAULT 'OPERADOR' CHECK (perfil IN ('ADMIN', 'GERENTE', 'OPERADOR', 'SUPORTE')),
    foto_url VARCHAR(500),
    telefone VARCHAR(20),
    ativo BOOLEAN DEFAULT true,
    ultimo_login TIMESTAMP,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

-- =============================================================================
-- TABELAS DE ESTABELECIMENTO
-- =============================================================================

-- Estabelecimentos/Lojas
CREATE TABLE estabelecimentos (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    nome VARCHAR(150) NOT NULL,
    razao_social VARCHAR(200),
    cnpj VARCHAR(18) UNIQUE,
    inscricao_estadual VARCHAR(20),
    inscricao_municipal VARCHAR(20),
    endereco_cep VARCHAR(9),
    endereco_logradouro VARCHAR(200),
    endereco_numero VARCHAR(20),
    endereco_complemento VARCHAR(100),
    endereco_bairro VARCHAR(100),
    endereco_cidade VARCHAR(100),
    endereco_estado VARCHAR(2),
    telefone VARCHAR(20),
    email VARCHAR(150),
    ativo BOOLEAN DEFAULT true,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

-- PDVs/Caixas
CREATE TABLE pdvs (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    estabelecimento_id UUID REFERENCES estabelecimentos(id) ON DELETE CASCADE,
    nome VARCHAR(100) NOT NULL,
    numero INTEGER NOT NULL,
    ip_address INET,
    mac_address VARCHAR(17),
    status VARCHAR(20) DEFAULT 'OFFLINE' CHECK (status IN ('ONLINE', 'OFFLINE', 'MANUTENCAO')),
    ultimo_heartbeat TIMESTAMP,
    ativo BOOLEAN DEFAULT true,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW(),
    UNIQUE(estabelecimento_id, numero)
);

-- =============================================================================
-- TABELAS DE PRODUTOS
-- =============================================================================

-- Categorias de produtos
CREATE TABLE categorias (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    nome VARCHAR(100) NOT NULL,
    descricao TEXT,
    categoria_pai_id UUID REFERENCES categorias(id) ON DELETE SET NULL,
    ordem INTEGER DEFAULT 0,
    ativo BOOLEAN DEFAULT true,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

-- Produtos
CREATE TABLE produtos (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    codigo_barras VARCHAR(50),
    nome VARCHAR(200) NOT NULL,
    descricao TEXT,
    categoria_id UUID REFERENCES categorias(id) ON DELETE SET NULL,
    unidade_medida VARCHAR(10) DEFAULT 'UN',
    preco_venda DECIMAL(10,2) NOT NULL,
    preco_custo DECIMAL(10,2),
    margem_lucro DECIMAL(5,2),
    estoque_atual DECIMAL(10,3) DEFAULT 0,
    estoque_minimo DECIMAL(10,3) DEFAULT 0,
    foto_url VARCHAR(500),
    ativo BOOLEAN DEFAULT true,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

-- =============================================================================
-- TABELAS DE VENDAS
-- =============================================================================

-- Vendas/Cupons Fiscais
CREATE TABLE vendas (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    numero_cupom BIGINT NOT NULL,
    estabelecimento_id UUID REFERENCES estabelecimentos(id),
    pdv_id UUID REFERENCES pdvs(id),
    usuario_id UUID REFERENCES usuarios(id),
    
    -- Dados do cliente
    cliente_cpf VARCHAR(14),
    cliente_nome VARCHAR(150),
    cliente_email VARCHAR(150),
    cliente_telefone VARCHAR(20),
    
    -- Valores
    valor_bruto DECIMAL(10,2) NOT NULL,
    valor_desconto DECIMAL(10,2) DEFAULT 0,
    valor_acrescimo DECIMAL(10,2) DEFAULT 0,
    valor_liquido DECIMAL(10,2) NOT NULL,
    
    -- Status e datas
    status VARCHAR(20) DEFAULT 'FINALIZADA' CHECK (status IN ('ABERTA', 'FINALIZADA', 'CANCELADA')),
    motivo_cancelamento TEXT,
    data_cancelamento TIMESTAMP,
    
    -- Observações
    observacoes TEXT,
    
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW(),
    
    UNIQUE(estabelecimento_id, numero_cupom)
);

-- Itens da venda
CREATE TABLE venda_itens (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    venda_id UUID REFERENCES vendas(id) ON DELETE CASCADE,
    produto_id UUID REFERENCES produtos(id),
    sequencia INTEGER NOT NULL,
    codigo_produto VARCHAR(50),
    nome_produto VARCHAR(200) NOT NULL,
    quantidade DECIMAL(10,3) NOT NULL,
    preco_unitario DECIMAL(10,2) NOT NULL,
    desconto_item DECIMAL(10,2) DEFAULT 0,
    valor_total DECIMAL(10,2) NOT NULL,
    created_at TIMESTAMP DEFAULT NOW(),
    
    UNIQUE(venda_id, sequencia)
);

-- =============================================================================
-- TABELAS DE PAGAMENTO
-- =============================================================================

-- Formas de pagamento
CREATE TABLE formas_pagamento (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    nome VARCHAR(50) NOT NULL,
    tipo VARCHAR(20) CHECK (tipo IN ('DINHEIRO', 'CARTAO_DEBITO', 'CARTAO_CREDITO', 'PIX', 'OUTROS')),
    permite_troco BOOLEAN DEFAULT false,
    taxa_percentual DECIMAL(5,2) DEFAULT 0,
    dias_recebimento INTEGER DEFAULT 0,
    ativo BOOLEAN DEFAULT true,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

-- Pagamentos da venda
CREATE TABLE venda_pagamentos (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    venda_id UUID REFERENCES vendas(id) ON DELETE CASCADE,
    forma_pagamento_id UUID REFERENCES formas_pagamento(id),
    valor DECIMAL(10,2) NOT NULL,
    valor_troco DECIMAL(10,2) DEFAULT 0,
    nsu VARCHAR(50),
    autorizacao VARCHAR(50),
    bandeira VARCHAR(50),
    created_at TIMESTAMP DEFAULT NOW()
);

-- =============================================================================
-- TABELAS DE SINCRONIZAÇÃO
-- =============================================================================

-- Log de sincronização entre PDV e Nuvem
CREATE TABLE sync_log (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    pdv_id UUID REFERENCES pdvs(id),
    tabela VARCHAR(50) NOT NULL,
    operacao VARCHAR(10) CHECK (operacao IN ('INSERT', 'UPDATE', 'DELETE')),
    registro_id UUID,
    dados JSONB,
    sincronizado BOOLEAN DEFAULT false,
    tentativas INTEGER DEFAULT 0,
    erro TEXT,
    created_at TIMESTAMP DEFAULT NOW(),
    sincronizado_at TIMESTAMP
);

-- =============================================================================
-- TABELAS DE CONFIGURAÇÃO
-- =============================================================================

-- Configurações de periféricos
CREATE TABLE perifericos_config (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    pdv_id UUID REFERENCES pdvs(id) ON DELETE CASCADE,
    tipo VARCHAR(30) CHECK (tipo IN ('IMPRESSORA_TERMICA', 'IMPRESSORA_FISCAL', 'GAVETA', 'TEF', 'SAT_MFE', 'BALANCA')),
    nome VARCHAR(100),
    porta VARCHAR(50),
    configuracao JSONB DEFAULT '{}',
    ativo BOOLEAN DEFAULT true,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

-- =============================================================================
-- ÍNDICES PARA PERFORMANCE
-- =============================================================================

-- Índices para vendas
CREATE INDEX idx_vendas_estabelecimento ON vendas(estabelecimento_id, created_at DESC);
CREATE INDEX idx_vendas_pdv ON vendas(pdv_id, created_at DESC);
CREATE INDEX idx_vendas_status ON vendas(status, created_at DESC);
CREATE INDEX idx_vendas_cliente_cpf ON vendas(cliente_cpf) WHERE cliente_cpf IS NOT NULL;

-- Índices para produtos
CREATE INDEX idx_produtos_codigo_barras ON produtos(codigo_barras) WHERE codigo_barras IS NOT NULL;
CREATE INDEX idx_produtos_nome ON produtos USING gin (nome gin_trgm_ops);
CREATE INDEX idx_produtos_categoria ON produtos(categoria_id) WHERE ativo = true;

-- Índices para sincronização
CREATE INDEX idx_sync_log_pendente ON sync_log(pdv_id, sincronizado, created_at) WHERE NOT sincronizado;

-- =============================================================================
-- TRIGGERS
-- =============================================================================

-- Função para atualizar updated_at
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ language 'plpgsql';

-- Aplicar triggers
CREATE TRIGGER update_usuarios_updated_at BEFORE UPDATE ON usuarios FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
CREATE TRIGGER update_estabelecimentos_updated_at BEFORE UPDATE ON estabelecimentos FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
CREATE TRIGGER update_pdvs_updated_at BEFORE UPDATE ON pdvs FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
CREATE TRIGGER update_categorias_updated_at BEFORE UPDATE ON categorias FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
CREATE TRIGGER update_produtos_updated_at BEFORE UPDATE ON produtos FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
CREATE TRIGGER update_vendas_updated_at BEFORE UPDATE ON vendas FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- =============================================================================
-- DADOS INICIAIS
-- =============================================================================

-- Usuário administrador padrão
-- Senha: admin123 (hash bcrypt)
INSERT INTO usuarios (nome, email, senha_hash, perfil) VALUES 
('Administrador', 'admin@solis.com', '$2b$10$rKvHMZqYJZqZqHqYJZqZqOqYJZqZqHqYJZqZqHqYJZqZqHqYJZqZq', 'ADMIN');

-- Estabelecimento padrão
INSERT INTO estabelecimentos (nome, razao_social, cnpj, endereco_cidade, endereco_estado) VALUES 
('Loja Matriz', 'Solis PDV LTDA', '12.345.678/0001-90', 'São Paulo', 'SP');

-- Formas de pagamento padrão
INSERT INTO formas_pagamento (nome, tipo, permite_troco, dias_recebimento) VALUES 
('Dinheiro', 'DINHEIRO', true, 0),
('Débito', 'CARTAO_DEBITO', false, 1),
('Crédito à Vista', 'CARTAO_CREDITO', false, 30),
('PIX', 'PIX', false, 0);

-- Categorias padrão
INSERT INTO categorias (nome, descricao, ordem) VALUES 
('Alimentos', 'Produtos alimentícios', 1),
('Bebidas', 'Bebidas em geral', 2),
('Limpeza', 'Produtos de limpeza', 3),
('Higiene', 'Produtos de higiene pessoal', 4);

-- Produtos de exemplo
INSERT INTO produtos (codigo_barras, nome, categoria_id, preco_venda, preco_custo, unidade_medida) VALUES 
('7891234567890', 'Produto Exemplo 1', (SELECT id FROM categorias WHERE nome = 'Alimentos' LIMIT 1), 10.50, 7.00, 'UN'),
('7891234567891', 'Produto Exemplo 2', (SELECT id FROM categorias WHERE nome = 'Bebidas' LIMIT 1), 5.90, 3.50, 'UN');