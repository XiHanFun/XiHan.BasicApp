#!/usr/bin/env node
// 门禁：引用的 --xh-* 令牌在当前安装的组件库版本里都存在。
// CSS 解析不到 var() 的值时整条声明作废，页面上看不出报错；真源是 node_modules 里的 tokens.css 与 styles/index.css，加本仓 packages/design 自己声明的名字。
import { readdirSync, readFileSync } from 'node:fs'
import { dirname, join, relative, resolve } from 'node:path'
import process from 'node:process'
import { fileURLToPath } from 'node:url'

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), '..')
const SOURCES = ['node_modules/@xihan-ui/tokens/tokens.css', 'node_modules/@xihan-ui/styles/index.css']
const SCAN = ['src', 'packages']
const SKIP = new Set(['node_modules', 'dist'])
const DECLARED = /(--xh-[a-z0-9-]+)\s*:/g
const USED = /var\(\s*(--xh-[a-z0-9-]+)/g

function files(dir, out = []) {
  for (const entry of readdirSync(dir, { withFileTypes: true })) {
    if (SKIP.has(entry.name) || entry.name.startsWith('.'))
      continue
    const path = join(dir, entry.name)
    if (entry.isDirectory())
      files(path, out)
    else if (/\.(?:vue|css|ts)$/.test(entry.name))
      out.push(path)
  }
  return out
}

const declared = new Set()
for (const source of SOURCES) {
  const text = readFileSync(join(ROOT, source), 'utf8')
  for (const match of text.matchAll(DECLARED))
    declared.add(match[1])
}

const scanned = SCAN.flatMap(dir => files(join(ROOT, dir)))
for (const file of scanned) {
  for (const match of readFileSync(file, 'utf8').matchAll(DECLARED))
    declared.add(match[1])
}

let failures = 0
for (const file of scanned) {
  for (const match of readFileSync(file, 'utf8').matchAll(USED)) {
    if (!declared.has(match[1])) {
      failures++
      console.error(`[token:check] ${relative(ROOT, file)} 引用了不存在的令牌 ${match[1]}`)
    }
  }
}

if (failures > 0)
  process.exit(1)
console.log(`[token:check] 通过（已知 ${declared.size} 个令牌）`)
