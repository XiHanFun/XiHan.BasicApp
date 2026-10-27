#!/usr/bin/env node
// 门禁：样式里不许有裸 px（方案 6.7：白名单从 0 起步，不允许增长）。
// 计数口径：.css 全文与 .vue 的 <style> 块，去掉注释后的 <数字>px 字面量；不计 0px、1px / 2px（描边与发丝线）、9999px（胶囊圆角）。
import { readdirSync, readFileSync } from 'node:fs'
import { dirname, join, relative, resolve } from 'node:path'
import process from 'node:process'
import { fileURLToPath } from 'node:url'

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), '..')
const SCAN = ['src', 'packages']
const SKIP = new Set(['node_modules', 'dist'])
const PX = /(?<![\w-])(-?\d+(?:\.\d+)?)px\b/g
const STYLE_BLOCK = /<style[^>]*>([\s\S]*?)<\/style>/g

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

let failures = 0
for (const file of SCAN.flatMap(dir => files(join(ROOT, dir)))) {
  const source = readFileSync(file, 'utf8')
  const css = file.endsWith('.vue') ? [...source.matchAll(STYLE_BLOCK)].map(m => m[1]).join('\n') : source
  const body = css.replace(/\/\*[\s\S]*?\*\//g, '')
  const hits = [...body.matchAll(PX)].filter(m => counts(m[1]))
  if (hits.length > 0) {
    failures += hits.length
    console.error(`[px:check] ${relative(ROOT, file)}: ${hits.length} 处裸 px（${hits.map(h => h[0]).join(', ')}），改用 XiHan.UI 的 --xh-space-* / --xh-radius-* 令牌或 Tailwind 刻度`)
  }
}

if (failures > 0)
  process.exit(1)
console.log('[px:check] 通过（0 处裸 px）')
