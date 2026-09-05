#!/usr/bin/env node
// 检查：引用的 --xh-* 令牌在当前钉住的组件库版本里都存在。
//
// CSS 解析不到 var() 的值时整条声明作废——不是退化成默认值，是那条规则根本不存在。
// 所以引一个上游还没发版的令牌名，症状不是"样式不对"而是"那条规则凭空消失"，
// 页面上看不出报错。实测踩过一次：110 处动效引用指向未发版的令牌，12 条动画一帧不播。
//
// 真源是 node_modules 里那份 tokens.css 加本仓 packages/design/ 自己声明的名字。
import { readdirSync, readFileSync, statSync } from 'node:fs'
import { join } from 'node:path'
import process from 'node:process'

const PKG_TOKENS = 'node_modules/@xihan-ui/tokens/tokens.css'
const PKG_STYLES = 'node_modules/@xihan-ui/styles/index.css'
const SCAN_DIRS = ['packages', 'src']
const SOURCE_EXT = /\.(?:vue|css|ts)$/

/** 目录里的源码文件，跳过依赖与产物。 */
function sourceFiles(dir, out = []) {
  let entries
  try {
    entries = readdirSync(dir, { withFileTypes: true })
  }
  catch {
    return out
  }
  for (const entry of entries) {
    if (entry.name === 'node_modules' || entry.name === 'dist' || entry.name.startsWith('.'))
      continue
    const path = join(dir, entry.name)
    if (entry.isDirectory())
      sourceFiles(path, out)
    else if (SOURCE_EXT.test(entry.name))
      out.push(path)
  }
  return out
}

function declaredIn(path) {
  try {
    statSync(path)
  }
  catch {
    return new Set()
  }
  const css = readFileSync(path, 'utf8')
  return new Set([...css.matchAll(/(--xh-[a-z0-9-]+)\s*:/g)].map(m => m[1]))
}

const declared = new Set([
  ...declaredIn(PKG_TOKENS),
  ...declaredIn(PKG_STYLES),
])

if (declared.size === 0) {
  console.error(`[check-token-availability] ✗ 读不到 ${PKG_TOKENS}——先跑 pnpm install`)
  process.exit(1)
}

// 本仓自己声明的也算数：动效令牌桥那类补位就在这里
const files = SCAN_DIRS.flatMap(dir => sourceFiles(dir))
for (const file of files) {
  for (const m of readFileSync(file, 'utf8').matchAll(/(--xh-[a-z0-9-]+)\s*:/g))
    declared.add(m[1])
}

const missing = new Map()
for (const file of files) {
  const src = readFileSync(file, 'utf8')
  for (const m of src.matchAll(/var\(\s*(--xh-[a-z0-9-]+)/g)) {
    if (declared.has(m[1]))
      continue
    const line = src.slice(0, m.index).split('\n').length
    if (!missing.has(m[1]))
      missing.set(m[1], [])
    missing.get(m[1]).push(`${file}:${line}`)
  }
}

if (missing.size > 0) {
  console.error('[check-token-availability] ✗ 引用了当前钉版里不存在的令牌：')
  for (const [token, sites] of [...missing].sort((a, b) => b[1].length - a[1].length)) {
    console.error(`  ${token}（${sites.length} 处）`)
    for (const site of sites.slice(0, 4))
      console.error(`    ${site}`)
    if (sites.length > 4)
      console.error(`    …另有 ${sites.length - 4} 处`)
  }
  console.error('  含未定义 var() 的声明整条作废。要么等上游发版并抬 catalog，')
  console.error('  要么在 packages/design/motion.css 的钉版补位里加一行（那一段排在 xihan.tokens 之前，发版后自动失效）。')
  process.exit(1)
}

console.log(
  `[check-token-availability] 通过：${files.length} 份源码引用的 --xh-* 令牌都在钉版或本仓声明里`
  + `（真源 ${declared.size} 个名字）`,
)
