#!/usr/bin/env node
// 开发用：按后端 Development 配置的密钥自签一枚 HS256 JWT（N1 的 auth.login 落地前用它联调）。
//   node scripts/dev-token.mjs --sub alice --tenant acme --role admin [--hours 8]
// 密钥读自 backend-next/src/XiHan.BasicApp.Host/appsettings.Development.json（XiHan:Security:Jwt:Secret），签发者与受众读 appsettings.json。
import { Buffer } from 'node:buffer'
import { createHmac } from 'node:crypto'
import { readFileSync } from 'node:fs'
import { dirname, join, resolve } from 'node:path'
import process from 'node:process'
import { fileURLToPath } from 'node:url'

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), '..', '..')
const HOST = join(ROOT, 'backend-next', 'src', 'XiHan.BasicApp.Host')

function option(name, fallback) {
  const index = process.argv.indexOf(`--${name}`)
  return index >= 0 && process.argv[index + 1] ? process.argv[index + 1] : fallback
}

function base64url(input) {
  return Buffer.from(input).toString('base64url')
}

const settings = JSON.parse(readFileSync(join(HOST, 'appsettings.json'), 'utf8'))
const development = JSON.parse(readFileSync(join(HOST, 'appsettings.Development.json'), 'utf8'))
const jwt = { ...settings.XiHan.Security.Jwt, ...development.XiHan?.Security?.Jwt }
if (!jwt.Secret)
  throw new Error('appsettings.Development.json 里没有 XiHan:Security:Jwt:Secret')

const now = Math.floor(Date.now() / 1000)
const roles = process.argv.flatMap((arg, index) => (arg === '--role' ? [process.argv[index + 1]] : []))
const payload = {
  sub: option('sub', 'alice'),
  name: option('sub', 'alice'),
  iss: jwt.Issuer,
  aud: jwt.Audience,
  iat: now,
  nbf: now - 60,
  exp: now + Number(option('hours', '8')) * 3600,
}
const tenant = option('tenant', 'acme')
if (tenant !== '-')
  payload.tenant = tenant
if (roles.length === 1)
  payload.role = roles[0]
else if (roles.length > 1)
  payload.role = roles
else payload.role = 'admin'

const header = base64url(JSON.stringify({ alg: 'HS256', typ: 'JWT' }))
const body = base64url(JSON.stringify(payload))
const signature = createHmac('sha256', jwt.Secret).update(`${header}.${body}`).digest('base64url')
process.stdout.write(`${header}.${body}.${signature}\n`)
