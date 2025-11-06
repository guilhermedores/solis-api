'use client'

import { useEffect, useState } from 'react'
import dynamic from 'next/dynamic'
import 'swagger-ui-react/swagger-ui.css'

const SwaggerUI = dynamic(() => import('swagger-ui-react'), { ssr: false })

export default function SwaggerPage() {
  const [spec, setSpec] = useState<any>(null)

  useEffect(() => {
    // Buscar spec apenas uma vez no mount
    fetch('/api/docs')
      .then((res) => res.json())
      .then((data) => setSpec(data))
      .catch((error) => console.error('Erro ao carregar spec:', error))
  }, []) // Array vazio garante execução única

  if (!spec) {
    return (
      <div style={{ 
        display: 'flex', 
        justifyContent: 'center', 
        alignItems: 'center', 
        height: '100vh',
        fontFamily: 'sans-serif',
        flexDirection: 'column',
        gap: '1rem'
      }}>
        <div style={{ fontSize: '1.2rem' }}>⏳ Carregando documentação...</div>
        <div style={{ fontSize: '0.9rem', color: '#666' }}>Swagger UI</div>
      </div>
    )
  }

  return <SwaggerUI spec={spec} />
}
