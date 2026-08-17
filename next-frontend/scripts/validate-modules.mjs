#!/usr/bin/env node
// ----------------------------------------------------------------
// 模块目录门禁（零依赖）。校验 src/modules/<模块>/ 的约定结构：
//   node scripts/validate-modules.mjs
// 校验三件事：
//   1. 模块目录只允许出现约定条目：views/ api/ locales/ setup.ts README.md；
//   2. 模块视图重键后（/src/modules/<m>/views/** → /src/views/**）不得与 src/views
//      既有文件同键，模块之间也不得互相同键——冲突即构建门禁失败；
//   3. locales/ 下只允许 zh-CN.ts / en-US.ts，且两语言文件必须成对。
// 退出码非 0 表示校验失败，挂 CI。
// ----------------------------------------------------------------
import { existsSync, readdirSync, statSync } from 'node:fs'
import { dirname, join, relative, resolve } from 'node:path'
import process from 'node:process'
import { fileURLToPath } from 'node:url'

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), '..')
const MODULES = join(ROOT, 'src/modules')
const VIEWS = join(ROOT, 'src/views')
const ALLOWED_ENTRIES = new Set(['views', 'api', 'locales', 'setup.ts', 'README.md'])
const ALLOWED_LOCALES = ['zh-CN.ts', 'en-US.ts']

function walkVues(dir, out = []) {
  if (!existsSync(dir))
    return out
  for (const f of readdirSync(dir)) {
    const p = join(dir, f)
    if (statSync(p).isDirectory())
      walkVues(p, out)
    else if (p.endsWith('.vue'))
      out.push(p)
  }
  return out
}

function main() {
  const errors = []
  if (!existsSync(MODULES)) {
    console.log('src/modules 不存在，跳过校验\nPASS')
    return
  }

  const viewKeys = new Map()
  for (const p of walkVues(VIEWS))
    viewKeys.set(relative(VIEWS, p).replaceAll('\\', '/'), 'src/views')

  const modules = readdirSync(MODULES).filter(m => statSync(join(MODULES, m)).isDirectory())
  for (const m of modules) {
    const dir = join(MODULES, m)

    // 1) 结构白名单
    for (const entry of readdirSync(dir)) {
      if (!ALLOWED_ENTRIES.has(entry))
        errors.push(`模块 ${m} 含未约定条目：${entry}（只允许 views/ api/ locales/ setup.ts README.md）`)
    }

    // 2) 视图键冲突
    for (const p of walkVues(join(dir, 'views'))) {
      const key = relative(join(dir, 'views'), p).replaceAll('\\', '/')
      const owner = viewKeys.get(key)
      if (owner)
        errors.push(`视图键冲突：${key} 同时来自 ${owner} 与 modules/${m}`)
      else
        viewKeys.set(key, `modules/${m}`)
    }

    // 3) locales 成对
    const localesDir = join(dir, 'locales')
    if (existsSync(localesDir)) {
      const files = readdirSync(localesDir)
      for (const f of files) {
        if (!ALLOWED_LOCALES.includes(f))
          errors.push(`模块 ${m} 的 locales 含未约定文件：${f}（只允许 ${ALLOWED_LOCALES.join(' / ')}）`)
      }
      for (const required of ALLOWED_LOCALES) {
        if (files.length > 0 && !files.includes(required))
          errors.push(`模块 ${m} 的 locales 缺少 ${required}（两语言必须成对）`)
      }
    }
  }

  console.log(`modules: ${modules.length}（${modules.join(', ') || '无'}），视图 ${viewKeys.size} 个`)
  for (const e of errors) console.log(`  FAIL: ${e}`)
  console.log(errors.length === 0 ? 'PASS' : 'FAIL')
  process.exit(errors.length === 0 ? 0 : 1)
}

main()
