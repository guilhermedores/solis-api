/**
 * API de Login/Autenticação
 * POST /api/auth/login - Autentica usuário e retorna JWT
 */

import { NextRequest, NextResponse } from 'next/server'
import { AuthService } from '@/lib/auth-service'

interface LoginRequest {
  email: string
  password: string
  tenant: string
}

/**
 * Handler para requisições OPTIONS (preflight CORS)
 */
export async function OPTIONS(request: NextRequest) {
  return new NextResponse(null, {
    status: 200,
    headers: {
      'Access-Control-Allow-Origin': '*',
      'Access-Control-Allow-Methods': 'GET, POST, PUT, DELETE, OPTIONS',
      'Access-Control-Allow-Headers': 'Content-Type, Authorization, x-tenant',
    },
  })
}

/**
 * @swagger
 * /api/auth/login:
 *   post:
 *     tags:
 *       - Autenticação
 *     summary: Login de usuário
 *     description: Autentica usuário com email e senha e retorna token JWT
 *     requestBody:
 *       required: true
 *       content:
 *         application/json:
 *           schema:
 *             type: object
 *             required:
 *               - email
 *               - password
 *               - tenant
 *             properties:
 *               email:
 *                 type: string
 *                 format: email
 *                 example: "admin@demo.com"
 *               password:
 *                 type: string
 *                 format: password
 *                 example: "senha123"
 *               tenant:
 *                 type: string
 *                 example: "demo"
 *                 description: Subdomain do tenant
 *     responses:
 *       200:
 *         description: Login realizado com sucesso
 *         content:
 *           application/json:
 *             schema:
 *               type: object
 *               properties:
 *                 success:
 *                   type: boolean
 *                   example: true
 *                 token:
 *                   type: string
 *                   example: "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
 *                 expiresAt:
 *                   type: string
 *                   format: date-time
 *                 usuario:
 *                   type: object
 *                   properties:
 *                     id:
 *                       type: string
 *                       format: uuid
 *                     nome:
 *                       type: string
 *                     email:
 *                       type: string
 *                     role:
 *                       type: string
 *       400:
 *         description: Dados inválidos
 *       401:
 *         description: Credenciais inválidas
 *       500:
 *         description: Erro interno
 */
export async function POST(request: NextRequest) {
  try {
    const body = await request.json() as LoginRequest

    // Validação dos campos obrigatórios
    if (!body.email || !body.password || !body.tenant) {
      return NextResponse.json(
        { 
          success: false,
          error: 'Email, senha e tenant são obrigatórios' 
        },
        { status: 400 }
      )
    }

    // Valida formato do email
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
    if (!emailRegex.test(body.email)) {
      return NextResponse.json(
        { 
          success: false,
          error: 'Email inválido' 
        },
        { status: 400 }
      )
    }

    // Realiza o login
    const result = await AuthService.login(
      body.email,
      body.password,
      body.tenant
    )

    if (!result.success) {
      return NextResponse.json(
        { 
          success: false,
          error: result.error 
        },
        { status: 401 }
      )
    }

    return NextResponse.json({
      success: true,
      token: result.token,
      expiresAt: result.expiresAt,
      usuario: result.usuario
    })

  } catch (error) {
    console.error('Erro no login:', error)
    return NextResponse.json(
      { 
        success: false,
        error: 'Erro ao realizar login' 
      },
      { status: 500 }
    )
  }
}
