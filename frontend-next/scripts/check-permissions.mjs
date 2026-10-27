#!/usr/bin/env node
// 门禁：视图与包里引用的权限码必须存在于生成的 packages/contracts/permissions.ts（方案 6.3）。
// 扫描 src 与 packages 里形如 'identity.users.read' 的三段点分字面量（排除生成物与契约 JSON）。
import { readdirSync, readFileSync } from 'node:fs'
import { dirname, join, relative, resolve } from 'node:path'
import process from 'node:process'
import { fileURLToPath } from 'node:url'

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), '..')
const PERMISSIONS = join(ROOT, 'packages', 'contracts', 'permissions.ts')
const SCAN = ['src', 'packages']
const SKIP = new Set(['node_modules', 'dist', 'contracts'])
// 只认带权限语义的位置：permission: '…'、permission="…"、can('…')、hasPermission('…')；消息键（identity.users.title 之类）同为三段点分，不在这些位置不算
const CODE = /(?:\bpermissions?\s*[:=]\s*|\b(?:can|hasPermission)\(\s*)['"`]([a-z][a-z0-9-]*\.[a-z][a-z0-9-]*\.[a-z][a-z0-9-]*)['"`]/g

function files(dir, out = []) {
  for (const entry of readdirSync(dir, { withFileTypes: true })) {
    if (SKIP.has(entry.name) || entry.name.startsWith('.'))
      continue
    const path = join(dir, entry.name)
    if (entry.isDirectory())
      files(path, out)
    else if (/\.(?:vue|ts)$/.test(entry.name) && !entry.name.endsWith('.d.ts'))
      out.push(path)
  }
  return out
}

const declared = new Set([...readFileSync(PERMISSIONS, 'utf8').matchAll(/^\s+'([a-z][a-z0-9-]*\.[a-z][a-z0-9-]*\.[a-z][a-z0-9-]*)': \{ code/gm)].map(m => m[1]))

let failures = 0
for (const file of SCAN.flatMap(dir => files(join(ROOT, dir)))) {
  const source = readFileSync(file, 'utf8')
  source.split('\n').forEach((line, index) => {
    for (const match of line.matchAll(CODE)) {
      if (!declared.has(match[1])) {
        failures++
        console.error(`[permissions:check] ${relative(ROOT, file)}:${index + 1} 引用了契约里没有的权限码 '${match[1]}'`)
      }
    }
  })
}

if (failures > 0)
  process.exit(1)
console.log(`[permissions:check] 通过（契约里有 ${declared.size} 个权限码）`)
