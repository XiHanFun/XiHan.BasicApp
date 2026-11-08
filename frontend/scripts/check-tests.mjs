#!/usr/bin/env node
// 门禁：测试只放各包与模块的 tests/ 目录，命名 *.spec.ts
// 位置是 packages/<包>/tests/、src/modules/<模块>/tests/ 或 src/<目录>/tests/，目录内按被测文件的子路径摆放
import { readdirSync } from 'node:fs'
import { dirname, join, relative, resolve, sep } from 'node:path'
import process from 'node:process'
import { fileURLToPath } from 'node:url'

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), '..')
const SCAN = ['src', 'packages', 'scripts']
const SKIP = new Set(['node_modules', 'dist'])
const TEST = /\.(?:test|spec)\.[cm]?[jt]sx?$/
const PLACE = /^(?:packages\/[^/]+|src\/modules\/[^/]+|src\/(?!modules\/)[^/]+)\/tests\/(?:.+\/)?[^/]+\.spec\.ts$/

function files(dir, out = []) {
  for (const entry of readdirSync(dir, { withFileTypes: true })) {
    if (SKIP.has(entry.name) || entry.name.startsWith('.'))
      continue
    const path = join(dir, entry.name)
    if (entry.isDirectory())
      files(path, out)
    else if (TEST.test(entry.name))
      out.push(path)
  }
  return out
}

let total = 0
let failures = 0
for (const file of SCAN.flatMap(dir => files(join(ROOT, dir)))) {
  total++
  const path = relative(ROOT, file).split(sep).join('/')
  if (!PLACE.test(path)) {
    failures++
    console.error(`[tests:check] ${path} 不在规定位置：放进所属包或模块的 tests/，按被测文件的子路径摆放，命名 *.spec.ts`)
  }
}

if (failures > 0)
  process.exit(1)
console.log(`[tests:check] 通过（${total} 个测试文件）`)
