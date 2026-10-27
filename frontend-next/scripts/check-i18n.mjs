#!/usr/bin/env node
// 门禁：文案按 10 种区域（与后端一致）各一份，键集完全对称；源码里静态引用的 t('…') 键必须存在。
// 文案根：packages/locales/langs/<区域>.ts（壳层）与 src/modules/<包>/locales/<区域>.ts（各能力包），都是 `export default { … }` 的字面量对象。
import { readdirSync, readFileSync, statSync } from 'node:fs'
import { dirname, join, relative, resolve } from 'node:path'
import process from 'node:process'
import { fileURLToPath } from 'node:url'

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), '..')
const LOCALES = ['en-US', 'zh-CN', 'zh-TW', 'ja-JP', 'ko-KR', 'de-DE', 'es-ES', 'fr-FR', 'pt-BR', 'ru-RU']
const SCAN = ['src', 'packages']
const SKIP = new Set(['node_modules', 'dist', 'contracts'])

function loadModule(path) {
  let text = readFileSync(path, 'utf8')
  text = text.replace(/^\s*export\s+default\s*/m, '').trim().replace(/;$/, '')
  // eslint-disable-next-line no-eval
  return (0, eval)(`(${text})`)
}

function flatten(value, prefix = '', out = new Set()) {
  for (const [key, child] of Object.entries(value)) {
    const path = prefix ? `${prefix}.${key}` : key
    if (child && typeof child === 'object')
      flatten(child, path, out)
    else out.add(path)
  }
  return out
}

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

const roots = [join(ROOT, 'packages', 'locales', 'langs')]
const modules = join(ROOT, 'src', 'modules')
try {
  for (const name of readdirSync(modules)) {
    const dir = join(modules, name, 'locales')
    try {
      if (statSync(dir).isDirectory())
        roots.push(dir)
    }
    catch {}
  }
}
catch {}

let failures = 0
const known = new Set()
for (const root of roots) {
  const keysByLocale = new Map()
  for (const locale of LOCALES) {
    const path = join(root, `${locale}.ts`)
    try {
      keysByLocale.set(locale, flatten(loadModule(path)))
    }
    catch (error) {
      failures++
      console.error(`[i18n:check] ${relative(ROOT, root)} 缺少 ${locale}.ts（${error.message}）`)
    }
  }
  const reference = keysByLocale.get('en-US') ?? new Set()
  for (const [locale, keys] of keysByLocale) {
    const missing = [...reference].filter(k => !keys.has(k))
    const extra = [...keys].filter(k => !reference.has(k))
    if (missing.length > 0 || extra.length > 0) {
      failures++
      console.error(`[i18n:check] ${relative(ROOT, root)}/${locale}.ts 与 en-US 键集不对称：缺 [${missing.join(', ')}]，多 [${extra.join(', ')}]`)
    }
  }
  for (const key of reference)
    known.add(key)
}

const T_CALL = /\b\$?t\(\s*['"`]([a-z][\w-]*(?:\.[\w-]+)+)['"`]/g
for (const file of SCAN.flatMap(dir => files(join(ROOT, dir)))) {
  if (file.includes(`${join('locales')}${process.platform === 'win32' ? '\\' : '/'}`))
    continue
  const source = readFileSync(file, 'utf8')
  for (const match of source.matchAll(T_CALL)) {
    if (!known.has(match[1])) {
      failures++
      console.error(`[i18n:check] ${relative(ROOT, file)} 引用了未定义的文案键 '${match[1]}'`)
    }
  }
}

if (failures > 0)
  process.exit(1)
console.log(`[i18n:check] 通过（${roots.length} 个文案根，${known.size} 个键，${LOCALES.length} 种区域）`)
