#!/usr/bin/env node
// 门禁：样式里的裸 px 不许悄悄变多。
//
// 本站的间距、字号、圆角在 variables.css 与组件库令牌里都有对应档位，写死 px 的地方
// 换主题、调圆角、改密度时都跟不上。存量一次改不完，先按文件登记基线：登记过的文件
// 数量不许涨，没登记的文件一个裸 px 都不许有。
//
// 计数口径：.css 全文与 .vue 的 <style> 块，去掉 /* */ 注释后的 <数字>px 字面量。
// 不计三类——0px（等价于无单位零）、绝对值 1px 与 2px（描边与发丝线，令牌层同样是
// 写死的 1px/2px）、9999px（胶囊圆角的惯用写法）。
//
// 登记表 .px-allowlist.json 由 `pnpm px:update` 生成并入库，reason 手写。
// 表两侧都反查：登记了但文件没了或已清零、文件有裸 px 却没登记，都判红；
// 新登记的条目 reason 为空也判红。
import { execSync } from 'node:child_process'
import { readFile, writeFile } from 'node:fs/promises'
import process from 'node:process'

const TABLE = '.px-allowlist.json'
const SCAN_DIRS = ['packages', 'src']
const PX = /(?<![\w-])(-?\d+(?:\.\d+)?)px\b/g
const STYLE_BLOCK = /<style[^>]*>([\s\S]*?)<\/style>/g

/** 不计入的字面量：零、发丝线与描边、胶囊圆角。 */
function counts(value) {
  const n = Math.abs(Number(value))
  return n > 2 && n !== 9999
}

/** 一份文件里的裸 px 个数。 */
async function measure(file) {
  const source = await readFile(file, 'utf8')
  const css = file.endsWith('.vue')
    ? [...source.matchAll(STYLE_BLOCK)].map(m => m[1]).join('\n')
    : source
  const body = css.replace(/\/\*[\s\S]*?\*\//g, '')
  return [...body.matchAll(PX)].filter(m => counts(m[1])).length
}

// --others 把还没 git add 的新文件也算进来：新写的那份文件正是本门禁要拦的
const files = execSync(
  `git ls-files --cached --others --exclude-standard ${SCAN_DIRS.join(' ')}`,
  { encoding: 'utf8' },
)
  .split('\n')
  .filter(f => /\.(?:css|vue)$/.test(f))
  .sort()

const measured = {}
for (const file of files) {
  const n = await measure(file)
  if (n > 0)
    measured[file] = n
}

let baseline
try {
  baseline = JSON.parse(await readFile(TABLE, 'utf8'))
}
catch {
  baseline = {}
}

if (process.argv.includes('--update')) {
  const next = {}
  for (const [file, cap] of Object.entries(measured))
    next[file] = { cap, reason: baseline[file]?.reason ?? '' }
  await writeFile(TABLE, `${JSON.stringify(next, null, 2)}\n`, 'utf8')
  const total = Object.values(measured).reduce((a, b) => a + b, 0)
  console.log(`[px:update] 已写入 ${TABLE}：${Object.keys(next).length} 份文件，合计 ${total} 处裸 px`)
  process.exit(0)
}

const problems = []

for (const [file, n] of Object.entries(measured)) {
  const entry = baseline[file]
  if (!entry) {
    problems.push(
      `${file} 有 ${n} 处裸 px 却没登记——`
      + `能换成令牌就换（间距 --xh-space-*、字号 --xh-text-*、圆角 var(--radius)）；`
      + `确实得写死就跑 pnpm px:update 并在 ${TABLE} 里补 reason`,
    )
    continue
  }
  if (n > entry.cap) {
    problems.push(
      `${file} 有 ${n} 处裸 px，登记 ${entry.cap} 处——`
      + `新写的那几处先看能不能引令牌；确实得写死就跑 pnpm px:update 重落基线`,
    )
  }
}

for (const [file, entry] of Object.entries(baseline)) {
  if (!(file in measured)) {
    problems.push(
      `${file} 登记在 ${TABLE} 里却已经没有裸 px（或文件不在了）——跑 pnpm px:update 把它摘掉`,
    )
    continue
  }
  if (!entry.reason)
    problems.push(`${file} 登记了却没写 reason——补一句为什么这些 px 得写死`)
}

if (problems.length) {
  console.error('[check-px-allowlist] ✗ 裸 px 对不上登记表：')
  for (const p of problems)
    console.error(`  ${p}`)
  process.exit(1)
}

const total = Object.values(measured).reduce((a, b) => a + b, 0)
console.log(`[check-px-allowlist] ✓ ${Object.keys(measured).length} 份文件、${total} 处裸 px 都在登记表内`)
