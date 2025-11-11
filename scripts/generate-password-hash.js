/**
 * Script para gerar hash bcrypt de uma senha
 */

const bcrypt = require('bcryptjs')

const password = 'admin123'
const salt = bcrypt.genSaltSync(10)
const hash = bcrypt.hashSync(password, salt)

console.log('='.repeat(70))
console.log('HASH BCRYPT GERADO')
console.log('='.repeat(70))
console.log('')
console.log('Senha:', password)
console.log('Hash:', hash)
console.log('')
console.log('Use este hash no SQL:')
console.log(`'${hash}'`)
console.log('')

// Verificar se o hash funciona
const isValid = bcrypt.compareSync(password, hash)
console.log('Validação:', isValid ? '✅ Hash válido' : '❌ Hash inválido')
console.log('')
