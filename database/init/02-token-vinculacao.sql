-- =============================================================================
-- TABELA DE TOKENS DE VINCULAÇÃO
-- Gerencia tokens JWT para vincular agentes/terminais aos tenants
-- =============================================================================

CREATE TABLE IF NOT EXISTS token_vinculacoes (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    token VARCHAR(500) UNIQUE NOT NULL,
    nome_agente VARCHAR(100) NOT NULL,
    tipo VARCHAR(50) DEFAULT 'terminal' NOT NULL, -- terminal, mobile, web, etc
    ativo BOOLEAN DEFAULT true,
    valido_ate TIMESTAMP,
    ultimo_uso TIMESTAMP,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Índices para performance
CREATE INDEX IF NOT EXISTS idx_token_vinculacoes_tenant ON token_vinculacoes(tenant_id);
CREATE INDEX IF NOT EXISTS idx_token_vinculacoes_token ON token_vinculacoes(token);
CREATE INDEX IF NOT EXISTS idx_token_vinculacoes_ativo ON token_vinculacoes(ativo, valido_ate);

-- Trigger para atualizar updated_at
CREATE OR REPLACE FUNCTION update_token_vinculacoes_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trigger_token_vinculacoes_updated_at
    BEFORE UPDATE ON token_vinculacoes
    FOR EACH ROW
    EXECUTE FUNCTION update_token_vinculacoes_updated_at();

-- =============================================================================
-- DADOS DE EXEMPLO
-- =============================================================================

-- Token de exemplo para o tenant demo
-- Token JWT de exemplo (não use em produção)
INSERT INTO token_vinculacoes (tenant_id, token, nome_agente, tipo, ativo, valido_ate) 
SELECT 
    id,
    'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ0ZW5hbnQiOiJkZW1vIiwiYWdlbnROYW1lIjoiVGVybWluYWwgMDEiLCJ0eXBlIjoidGVybWluYWwiLCJ2YWxpZGFkZSI6IjIwMjYtMTItMzFUMjM6NTk6NTlaIn0.demo-token-example',
    'Terminal 01',
    'terminal',
    true,
    '2026-12-31 23:59:59'
FROM tenants 
WHERE subdomain = 'demo'
ON CONFLICT (token) DO NOTHING;

-- Mensagem de confirmação
DO $$
BEGIN
    RAISE NOTICE '=============================================================================';
    RAISE NOTICE 'Tabela token_vinculacoes criada com sucesso!';
    RAISE NOTICE 'Sistema de vinculação de agentes configurado.';
    RAISE NOTICE '=============================================================================';
END $$;
