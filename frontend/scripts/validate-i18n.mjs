#!/usr/bin/env node
// ----------------------------------------------------------------
// i18n 校验门禁（零依赖）。每次改 locale/组件后必跑：
//   node scripts/validate-i18n.mjs
// 校验三件事：
//   1. 按 langs/<locale>/index.ts 的实际合并逻辑（自动识别 nest `x,` 与 spread `...x`）重建 zh-CN/en-US，
//      flatten 成扁平 key 集；两语言 key 集必须完全对称（互无缺失）。
//   2. 全库（packages + src）扫描 t('...') / $t('...') 字面量 key，凡“首段是已知模块、但完整 key 不在 locale”
//      的即“孤儿键”（引用了但未定义，vue-i18n 运行期只返回 key 字符串、type-check/eslint 抓不到）。孤儿必须为 0。
//   3. 退出码非 0 表示校验失败，可挂 CI / 批末门禁。
// 注：只能校验静态字面量 key；动态 key（变量/模板串拼接）无法静态验证，跳过。
// ----------------------------------------------------------------
import { readFileSync, readdirSync, statSync } from 'node:fs'
import { dirname, join, resolve } from 'node:path'
import { fileURLToPath } from 'node:url'

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), '..')
const LANGS = join(ROOT, 'packages/locales/langs')
const LOCALES = ['zh-CN', 'en-US']

function read(p) {
  return readFileSync(p, 'utf8')
}

/** strip `export default` + 尾分号，eval 出模块默认导出对象 */
function loadModule(locale, name) {
  let t = read(join(LANGS, locale, `${name}.ts`))
  t = t.replace(/^\s*export\s+default\s*/m, '').trim().replace(/;$/, '')
  // eslint-disable-next-line no-eval
  return (0, eval)(`(${t})`)
}

/** 解析 index.ts：得出 nest 模块名、spread 模块名、以及内联字面量对象（如 checkUpdates） */
function parseIndex(locale) {
  const idx = read(join(LANGS, locale, 'index.ts'))
  let body = idx.slice(idx.indexOf('export default') + 'export default'.length)
  const nest = []
  const spread = []
  for (const line of body.split('\n')) {
    let m
    if ((m = line.match(/^\s*\.\.\.(\w+),/))) spread.push(m[1])
    else if ((m = line.match(/^\s*(\w+),\s*$/))) nest.push(m[1])
  }
  // 去掉 `  name,` 与 `  ...name,` 标识符行，剩下内联字面量（含注释）
  const inlineSrc = body
    .replace(/^\s*\.\.\.\w+,\s*$/gm, '')
    .replace(/^\s*\w+,\s*$/gm, '')
    .trim()
    .replace(/;$/, '')
  let inline = {}
  try {
    // eslint-disable-next-line no-eval
    inline = (0, eval)(`(${inlineSrc})`)
  }
  catch {
    inline = {}
  }
  return { nest, spread, inline }
}

function buildMerged(locale) {
  const { nest, spread, inline } = parseIndex(locale)
  const m = {}
  for (const n of nest) m[n] = loadModule(locale, n)
  for (const n of spread) Object.assign(m, loadModule(locale, n))
  Object.assign(m, inline)
  return m
}

function flatten(o, prefix = '', acc = {}) {
  for (const k of Object.keys(o)) {
    const v = o[k]
    const key = prefix ? `${prefix}.${k}` : k
    if (v && typeof v === 'object' && !Array.isArray(v)) flatten(v, key, acc)
    else acc[key] = v
  }
  return acc
}

function walk(dir, out = []) {
  for (const f of readdirSync(dir)) {
    const p = join(dir, f)
    const st = statSync(p)
    if (st.isDirectory()) {
      if (!/[\\/](node_modules|dist|\.git|\.turbo)([\\/]|$)/.test(p)) walk(p, out)
    }
    else if (/\.(vue|ts|tsx)$/.test(p) && !p.endsWith('.d.ts')) {
      out.push(p)
    }
  }
  return out
}

function main() {
  const merged = {}
  const keys = {}
  for (const loc of LOCALES) {
    merged[loc] = flatten(buildMerged(loc))
    keys[loc] = new Set(Object.keys(merged[loc]))
  }

  // 1) 对称性
  const onlyZh = [...keys['zh-CN']].filter(k => !keys['en-US'].has(k))
  const onlyEn = [...keys['en-US']].filter(k => !keys['zh-CN'].has(k))

  // 2) 孤儿键：扫全库 t()/$t() 字面量
  const topModules = new Set([...keys['zh-CN']].map(k => k.split('.')[0]))
  const re = /(?:^|[^\w$])\$?t\(\s*['"]([a-z][\w]*(?:\.[\w]+)+)['"]/g
  const used = new Map()
  for (const dir of ['packages', 'src']) {
    const base = join(ROOT, dir)
    let files = []
    try { files = walk(base) }
    catch { continue }
    for (const f of files) {
      const txt = read(f)
      let m
      while ((m = re.exec(txt))) {
        const key = m[1]
        if (topModules.has(key.split('.')[0])) used.set(key, f)
      }
    }
  }
  const orphans = [...used].filter(([k]) => !keys['zh-CN'].has(k))

  // 报告
  console.log(`locale keys: zh-CN=${keys['zh-CN'].size} en-US=${keys['en-US'].size}`)
  console.log(`t() refs (known-module, static): ${used.size}`)
  console.log(`asymmetry: onlyZh=${onlyZh.length} onlyEn=${onlyEn.length}`)
  console.log(`orphans (referenced but undefined): ${orphans.length}`)
  for (const k of onlyZh.slice(0, 30)) console.log(`  onlyZh: ${k}`)
  for (const k of onlyEn.slice(0, 30)) console.log(`  onlyEn: ${k}`)
  for (const [k, f] of orphans.slice(0, 50)) console.log(`  ORPHAN: ${k}  <- ${f.replace(ROOT, '').replace(/^[\\/]/, '')}`)

  const ok = onlyZh.length === 0 && onlyEn.length === 0 && orphans.length === 0
  console.log(ok ? 'PASS' : 'FAIL')
  process.exit(ok ? 0 : 1)
}

main()
