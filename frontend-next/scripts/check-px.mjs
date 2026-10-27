#!/usr/bin/env node
// 门禁：样式里的裸 px 按文件计数、只许减不许增（方案 6.7 修订（十一）：壳层整体从 4.x 搬入，白名单从搬入时的计数起步）。
// 计数口径：.css 全文与 .vue 的 <style> 块，去掉注释后的 <数字>px 字面量；不计 0px、1px / 2px（描边与发丝线）、9999px（胶囊圆角）。
// 白名单 scripts/px-allowlist.json 记每个文件允许的上限：超出或出现名单外的文件即失败；少于上限时提示把上限降下来（传 --update 自动收紧）。
import { readdirSync, readFileSync, writeFileSync } from 'node:fs'
import { dirname, join, relative, resolve } from 'node:path'
import process from 'node:process'
import { fileURLToPath } from 'node:url'

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), '..')
const ALLOWLIST = join(ROOT, 'scripts', 'px-allowlist.json')
const SCAN = ['src', 'packages']
const SKIP = new Set(['node_modules', 'dist'])
const PX = /(?<![\w-])(-?\d+(?:\.\d+)?)px\b/g
const STYLE_BLOCK = /<style[^>]*>([\s\S]*?)<\/style>/g
const update = process.argv.includes('--update')

function files(dir, out = []) {
  for (const entry of readdirSync(dir, { withFileTypes: true })) {
    if (SKIP.has(entry.name) || entry.name.startsWith('.'))
      continue
    const path = join(dir, entry.name)
    if (entry.isDirectory())
      files(path, out)
    else if (/\.(?:css|vue)$/.test(entry.name))
      out.push(path)
  }
  return out
}

function counts(value) {
  const n = Math.abs(Number(value))
  return n > 2 && n !== 9999
}

/** @type {Record<string, number>} */
const allowed = JSON.parse(readFileSync(ALLOWLIST, 'utf8'))
/** @type {Record<string, number>} */
const actual = {}
for (const file of SCAN.flatMap(dir => files(join(ROOT, dir)))) {
  const source = readFileSync(file, 'utf8')
  const css = file.endsWith('.vue') ? [...source.matchAll(STYLE_BLOCK)].map(m => m[1]).join('\n') : source
  const body = css.replace(/\/\*[\s\S]*?\*\//g, '')
  const hits = [...body.matchAll(PX)].filter(m => counts(m[1]))
  if (hits.length > 0)
    actual[relative(ROOT, file).replaceAll('\\', '/')] = hits.length
}

let failures = 0
let slack = 0
for (const [file, count] of Object.entries(actual).sort()) {
  const limit = allowed[file]
  if (limit === undefined) {
    failures++
    console.error(`[px:check] ${file}: ${count} 处裸 px，不在白名单里；改用 XiHan.UI 的 --xh-space-* / --xh-radius-* 令牌或 Tailwind 刻度`)
  }
  else if (count > limit) {
    failures++
    console.error(`[px:check] ${file}: ${count} 处裸 px，超过白名单上限 ${limit}`)
  }
  else if (count < limit) {
    slack++
    console.warn(`[px:check] ${file}: ${count} 处裸 px，低于白名单上限 ${limit}，请把上限收紧（node scripts/check-px.mjs --update）`)
  }
}
for (const file of Object.keys(allowed)) {
  if (!(file in actual)) {
    slack++
    console.warn(`[px:check] ${file} 已没有裸 px，请从白名单移除（node scripts/check-px.mjs --update）`)
  }
}

if (update) {
  writeFileSync(ALLOWLIST, `${JSON.stringify(Object.fromEntries(Object.entries(actual).sort()), null, 2)}\n`)
  console.log(`[px:check] 白名单已按当前计数收紧（${Object.keys(actual).length} 个文件）`)
  process.exit(0)
}
if (failures > 0 || slack > 0)
  process.exit(1)
const total = Object.values(actual).reduce((sum, n) => sum + n, 0)
console.log(`[px:check] 通过（${Object.keys(actual).length} 个文件、${total} 处裸 px，均在白名单内）`)
