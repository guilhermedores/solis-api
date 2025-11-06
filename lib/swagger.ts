import swaggerJSDoc from 'swagger-jsdoc'

const swaggerDefinition = {
  openapi: '3.0.0',
  info: {
    title: 'Solis API - Multi-Tenant REST API',
    version: '1.0.0',
    description: 'API backend do sistema Solis PDV com suporte a multi-tenancy híbrida (schemas compartilhados ou bancos dedicados)',
    contact: {
      name: 'Solis PDV',
      url: 'https://github.com/guilhermedores/solis',
    },
    license: {
      name: 'Proprietary',
      url: 'https://example.com/license',
    },
  },
  servers: [
    {
      url: 'http://localhost:3000',
      description: 'Development server',
    },
    {
      url: 'https://api.solis.com.br',
      description: 'Production server',
    },
  ],
  components: {
    securitySchemes: {
      bearerAuth: {
        type: 'http',
        scheme: 'bearer',
        bearerFormat: 'JWT',
        description: 'JWT token obtido via autenticação',
      },
    },
    parameters: {
      tenantQuery: {
        name: 'tenant',
        in: 'query',
        required: true,
        schema: {
          type: 'string',
          example: 'demo',
        },
        description: 'Identificador do tenant (cliente/empresa)',
      },
      tenantHeader: {
        name: 'x-tenant',
        in: 'header',
        required: false,
        schema: {
          type: 'string',
          example: 'demo',
        },
        description: 'Identificador do tenant via header (alternativa ao query param)',
      },
    },
    schemas: {
      Error: {
        type: 'object',
        properties: {
          error: {
            type: 'string',
            description: 'Mensagem de erro',
          },
          details: {
            type: 'string',
            description: 'Detalhes adicionais do erro',
          },
        },
      },
      HealthCheck: {
        type: 'object',
        properties: {
          tenant: {
            type: 'string',
            description: 'Tenant identificado',
            example: 'demo',
          },
          isValid: {
            type: 'boolean',
            description: 'Se o tenant existe e está ativo',
            example: true,
          },
          isolation: {
            type: 'object',
            properties: {
              type: {
                type: 'string',
                enum: ['SCHEMA', 'DATABASE'],
                description: 'Tipo de isolamento do tenant',
              },
              detail: {
                type: 'string',
                description: 'Nome do schema ou database',
                example: 'tenant_demo',
              },
              description: {
                type: 'string',
                description: 'Descrição do tipo de isolamento',
              },
            },
          },
          timestamp: {
            type: 'string',
            format: 'date-time',
            description: 'Timestamp da verificação',
          },
          message: {
            type: 'string',
            description: 'Mensagem descritiva',
          },
        },
      },
      Produto: {
        type: 'object',
        properties: {
          id: {
            type: 'string',
            format: 'uuid',
            description: 'ID único do produto',
          },
          codigo_barras: {
            type: 'string',
            description: 'Código de barras (EAN)',
            example: '7891000100103',
          },
          codigo_interno: {
            type: 'string',
            description: 'Código interno da empresa',
            example: 'PROD001',
          },
          nome: {
            type: 'string',
            description: 'Nome do produto',
            example: 'COCA-COLA 2L',
          },
          descricao: {
            type: 'string',
            description: 'Descrição detalhada',
            example: 'Refrigerante Coca-Cola 2 Litros',
          },
          ncm: {
            type: 'string',
            description: 'Código NCM (Nomenclatura Comum do Mercosul)',
          },
          cest: {
            type: 'string',
            description: 'Código CEST',
          },
          unidade_medida: {
            type: 'string',
            description: 'Unidade de medida',
            example: 'UN',
          },
          preco_venda: {
            type: 'number',
            format: 'decimal',
            description: 'Preço de venda',
            example: 8.90,
          },
          ativo: {
            type: 'boolean',
            description: 'Se o produto está ativo',
            example: true,
          },
          created_at: {
            type: 'string',
            format: 'date-time',
          },
          updated_at: {
            type: 'string',
            format: 'date-time',
          },
        },
      },
      Venda: {
        type: 'object',
        properties: {
          id: {
            type: 'string',
            format: 'uuid',
          },
          numero_cupom: {
            type: 'integer',
            description: 'Número do cupom fiscal',
          },
          user_id: {
            type: 'string',
            format: 'uuid',
            description: 'ID do usuário que realizou a venda',
          },
          cliente_cpf: {
            type: 'string',
            description: 'CPF do cliente (opcional)',
          },
          cliente_nome: {
            type: 'string',
            description: 'Nome do cliente (opcional)',
          },
          valor_bruto: {
            type: 'number',
            format: 'decimal',
            description: 'Valor bruto da venda',
          },
          valor_desconto: {
            type: 'number',
            format: 'decimal',
            description: 'Valor de desconto aplicado',
          },
          valor_liquido: {
            type: 'number',
            format: 'decimal',
            description: 'Valor líquido (bruto - desconto)',
          },
          status: {
            type: 'string',
            enum: ['ABERTA', 'FINALIZADA', 'CANCELADA'],
            description: 'Status da venda',
          },
          itens: {
            type: 'array',
            items: {
              type: 'object',
              properties: {
                produto_id: {
                  type: 'string',
                  format: 'uuid',
                },
                codigo_produto: {
                  type: 'string',
                },
                nome_produto: {
                  type: 'string',
                },
                quantidade: {
                  type: 'number',
                  format: 'decimal',
                },
                preco_unitario: {
                  type: 'number',
                  format: 'decimal',
                },
                valor_total: {
                  type: 'number',
                  format: 'decimal',
                },
              },
            },
          },
          pagamentos: {
            type: 'array',
            items: {
              type: 'object',
              properties: {
                forma_pagamento_id: {
                  type: 'string',
                  format: 'uuid',
                },
                valor: {
                  type: 'number',
                  format: 'decimal',
                },
                parcelas: {
                  type: 'integer',
                },
              },
            },
          },
          created_at: {
            type: 'string',
            format: 'date-time',
          },
        },
      },
      Empresa: {
        type: 'object',
        properties: {
          id: {
            type: 'string',
            format: 'uuid',
            description: 'ID único da empresa',
          },
          razaoSocial: {
            type: 'string',
            description: 'Razão social da empresa',
            example: 'EMPRESA DEMO LTDA',
          },
          nomeFantasia: {
            type: 'string',
            description: 'Nome fantasia',
            example: 'Loja Demo',
          },
          cnpj: {
            type: 'string',
            description: 'CNPJ (14 dígitos)',
            example: '12345678000190',
          },
          inscricaoEstadual: {
            type: 'string',
            description: 'Inscrição estadual',
          },
          inscricaoMunicipal: {
            type: 'string',
            description: 'Inscrição municipal',
          },
          logradouro: {
            type: 'string',
            description: 'Endereço - logradouro',
            example: 'Rua das Flores',
          },
          numero: {
            type: 'string',
            description: 'Endereço - número',
            example: '123',
          },
          complemento: {
            type: 'string',
            description: 'Endereço - complemento',
            example: 'Sala 1',
          },
          bairro: {
            type: 'string',
            description: 'Endereço - bairro',
            example: 'Centro',
          },
          cidade: {
            type: 'string',
            description: 'Endereço - cidade',
            example: 'São Paulo',
          },
          uf: {
            type: 'string',
            description: 'Endereço - UF',
            example: 'SP',
          },
          cep: {
            type: 'string',
            description: 'CEP (8 dígitos)',
            example: '01234567',
          },
          telefone: {
            type: 'string',
            description: 'Telefone de contato',
            example: '11987654321',
          },
          email: {
            type: 'string',
            format: 'email',
            description: 'E-mail de contato',
            example: 'contato@empresademo.com.br',
          },
          site: {
            type: 'string',
            format: 'uri',
            description: 'Website',
          },
          regimeTributario: {
            type: 'string',
            enum: ['simples_nacional', 'lucro_presumido', 'lucro_real'],
            description: 'Regime tributário',
            example: 'simples_nacional',
          },
          ambienteFiscal: {
            type: 'string',
            enum: ['producao', 'homologacao'],
            description: 'Ambiente fiscal (produção ou homologação)',
            example: 'homologacao',
          },
          logo: {
            type: 'string',
            description: 'Logo em Base64',
          },
          ativo: {
            type: 'boolean',
            description: 'Se a empresa está ativa',
            example: true,
          },
          createdAt: {
            type: 'string',
            format: 'date-time',
          },
          updatedAt: {
            type: 'string',
            format: 'date-time',
          },
        },
      },
    },
  },
  tags: [
    {
      name: 'Health',
      description: 'Health check e status da API',
    },
    {
      name: 'Empresas',
      description: 'Gerenciamento de empresas (dados para cupom fiscal)',
    },
    {
      name: 'Produtos',
      description: 'Gerenciamento de produtos (CRUD)',
    },
    {
      name: 'Vendas',
      description: 'Gerenciamento de vendas e PDV',
    },
    {
      name: 'Formas de Pagamento',
      description: 'Gerenciamento de formas de pagamento',
    },
  ],
}

const options = {
  swaggerDefinition,
  apis: ['./app/api/**/*.ts'], // Caminho para os arquivos com anotações JSDoc
}

export const swaggerSpec = swaggerJSDoc(options)
