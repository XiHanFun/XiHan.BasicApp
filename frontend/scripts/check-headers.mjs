#!/usr/bin/env node
// 门禁：src 与 packages 下的 .ts、.vue、.css 源码以版权头开头，与 XiHan.UI 同一写法；测试不加
// --write 按文件类型补齐：.vue 用 <!-- -->，其余用 /* */；生成物由 gen-contracts 自己写出版权头
import { execFileSync } from 'node:child_process'
import { readFileSync, writeFileSync } from 'node:fs'
import { dirname, resolve } from 'node:path'
import process from 'node:process'
import { fileURLToPath } from 'node:url'

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), '..')
const WRITE = process.argv.includes('--write')

export const COPYRIGHT_TEXT = 'Copyright (c) 2021-Present XiHanFun and contributors.'
export const LICENSE_TEXT = 'Licensed under the MIT License. See LICENSE in the project root for license information.'

/**
 * 按换行符渲染版权头
 */
export function renderHeader(markup, newline) {
  return markup
    ? `<!--${newline}  ${COPYRIGHT_TEXT}${newline}  ${LICENSE_TEXT}${newline}-->`
    : `/*${newline} * ${COPYRIGHT_TEXT}${newline} * ${LICENSE_TEXT}${newline} */`
}

/**
 * 补上版权头，已有的不重复加
 */
export function applyHeader(path, source) {
  const newline = source.includes('\r\n')
    ? '\r\n'
    : '\n'
  const header = renderHeader(path.endsWith('.vue'), newline)
  if (source.startsWith(`${header}${newline}`))
    return source
  return `${header}${newline}${newline}${source.replace(/^(?:\r?\n)+/, '')}`
}

function targets() {
  return execFileSync('git', [
    'ls-files',
    '--cached',
    '--others',
    '--exclude-standard',
    'src',
    'packages',
  ], { cwd: ROOT, encoding: 'utf8' })
    .split('\n')
    .filter(path => /\.(?:ts|vue|css)$/.test(path) && !/(?:^|\/)tests\//.test(path))
}

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  const missing = []
  const files = targets()
  for (const path of files) {
    const file = resolve(ROOT, path)
    const source = readFileSync(file, 'utf8')
    const expected = applyHeader(path, source)
    if (expected === source)
      continue
    missing.push(path)
    if (WRITE)
      writeFileSync(file, expected)
  }

  if (WRITE) {
    console.log(`[headers:check] 已补齐 ${missing.length} 个文件（共 ${files.length} 个）`)
  }
  else if (missing.length > 0) {
    for (const path of missing.slice(0, 40))
      console.error(`[headers:check] ${path} 缺版权头`)
    if (missing.length > 40)
      console.error(`[headers:check] … 其余 ${missing.length - 40} 个`)
    console.error('[headers:check] 运行 node scripts/check-headers.mjs --write 补齐')
    process.exit(1)
  }
  else {
    console.log(`[headers:check] 通过（${files.length} 个文件）`)
  }
}
