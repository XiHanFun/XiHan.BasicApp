#!/usr/bin/env node
// 门禁：src/modules/<包> 与后端能力包同名（kebab）；每个模块有 setup.ts 与 locales/；视图键（去掉模块段的路径）全局唯一，供后端菜单的 component 字段映射（方案 6.5）。
import { readdirSync, statSync } from 'node:fs'
import { dirname, join, relative, resolve } from 'node:path'
import process from 'node:process'
import { fileURLToPath } from 'node:url'

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), '..')
const MODULES = join(ROOT, 'src', 'modules')
// 与方案 5.2 的 19 个能力包一一对应（kebab-case）
const KNOWN = new Set(['identity', 'tenancy', 'organization', 'access', 'settings', 'notification', 'files', 'auditing', 'maintenance', 'open-platform', 'realtime', 'workbench', 'workflow', 'printing', 'codegen', 'chat', 'ai'])

function views(dir, prefix = '', out = []) {
  for (const entry of readdirSync(dir, { withFileTypes: true })) {
    const path = join(dir, entry.name)
    if (entry.isDirectory())
      views(path, `${prefix}${entry.name}/`, out)
    else if (entry.name === 'index.vue')
      out.push(`${prefix}index`)
  }
  return out
}

let failures = 0
const keys = new Map()
for (const name of readdirSync(MODULES)) {
  const dir = join(MODULES, name)
  if (!statSync(dir).isDirectory())
    continue
  if (!KNOWN.has(name)) {
    failures++
    console.error(`[modules:check] src/modules/${name} 不是方案 5.2 的能力包名`)
  }
  for (const required of ['setup.ts', 'locales', 'views']) {
    try {
      statSync(join(dir, required))
    }
    catch {
      failures++
      console.error(`[modules:check] src/modules/${name} 缺少 ${required}`)
    }
  }
  try {
    for (const view of views(join(dir, 'views'))) {
      if (!/^[a-z0-9/-]+$/.test(view)) {
        failures++
        console.error(`[modules:check] 视图路径必须是 kebab-case：${name}/${view}`)
      }
      const key = `${name}/${view}`
      if (keys.has(key)) {
        failures++
        console.error(`[modules:check] 视图键重复：${key}`)
      }
      keys.set(key, relative(ROOT, join(dir, 'views', view)))
    }
  }
  catch {}
}

if (failures > 0)
  process.exit(1)
console.log(`[modules:check] 通过（${keys.size} 个视图）`)
