/**
 * 页面码一致性检查。
 *
 * 页面 schema 的 pageCode 是列设置、搜索设置、个人视图与导入历史的存储键，导出的 businessType 由后端按页面码分发。
 * 二者都取该页面在后端页面登记表（各模块 PageRegistry）里的页面码；一页有多张表时用「页面码.子表」区分。
 * 前端曾各起一套（system.role、platform.tenant、develop.ai.assistant……），与菜单、按钮、导出的页面码对不上，
 * 改名还要随升级脚本迁移存储，所以这里把对应关系钉住。
 */
import { existsSync, readdirSync, readFileSync, statSync } from 'node:fs'
import { join, relative, resolve } from 'node:path'
import { describe, expect, it } from 'vitest'

const SRC_ROOT = join(process.cwd(), 'src')
/**
 * 后端放项目的分组目录：平台模块在 modules，业务模块在 business（代码生成「生成到项目」也写进这里）
 */
const BACKEND_PROJECT_ROOTS = ['modules', 'business'].map(group => resolve(process.cwd(), '..', 'backend', 'src', group))

/** 后端全部项目目录 */
function listBackendProjects(): string[] {
  return BACKEND_PROJECT_ROOTS
    .filter(root => existsSync(root))
    .flatMap(root => readdirSync(root).map(name => join(root, name)))
    .filter(project => statSync(project).isDirectory())
}

/** 后端菜单页登记：new("页面码", "标题", "i18n 键"|null, MenuType.Menu, "路径", "路由名", "组件路径", ... */
const MENU_DESCRIPTOR = /new\("([a-z][\w.-]*)",\s*"[^"]*",\s*(?:"[^"]*"|null),\s*MenuType\.Menu,\s*"[^"]*",\s*"[^"]*",\s*"([^"]+)"/g

const PAGE_CODE_LITERAL = /\bpageCode:\s*'([^']+)'/g

const EXPORT_BUSINESS_TYPE = /\bexport:\s*\{\s*businessType:\s*'([^']+)'/g

/** 与 packages/router/dynamic.ts 的 toKebabCase 同一规则：后端组件路径的大驼峰段落到前端目录名 */
function toKebabCase(input: string) {
  return input
    .replace(/([A-Z]+)([A-Z][a-z])/g, '$1-$2')
    .replace(/([a-z0-9])([A-Z])/g, '$1-$2')
    .replace(/_/g, '-')
    .toLowerCase()
}

/** 组件路径 → 视图目录（去掉末段 index） */
function toViewDirectory(component: string) {
  return component
    .split('/')
    .map(segment => toKebabCase(segment))
    .slice(0, -1)
    .join('/')
}

/** 扫出后端各模块登记的菜单页：视图目录 → 页面码 */
function readRegisteredPages(): Map<string, string> {
  const pages = new Map<string, string>()
  for (const project of listBackendProjects()) {
    const registry = join(project, 'Application', 'Pages', 'PageRegistry.cs')
    if (!existsSync(registry)) {
      continue
    }
    for (const match of readFileSync(registry, 'utf8').matchAll(MENU_DESCRIPTOR)) {
      if (match[1] && match[2]) {
        pages.set(toViewDirectory(match[2]), match[1])
      }
    }
  }
  return pages
}

function listVueFiles(dir: string, acc: string[] = []): string[] {
  for (const entry of readdirSync(dir)) {
    const full = join(dir, entry)
    if (statSync(full).isDirectory()) {
      listVueFiles(full, acc)
      continue
    }
    if (entry.endsWith('.vue')) {
      acc.push(full)
    }
  }
  return acc
}

/** 源码文件所在的视图目录：模块视图与 src/views 并入同一张表（见 app/context.ts） */
function viewDirectoryOf(rel: string) {
  return rel
    .replace(/^modules\/[^/]+\/views\//, '')
    .replace(/^views\//, '')
    .split('/')
    .slice(0, -1)
    .join('/')
}

/** 找出文件归属的菜单页：自身目录或最近的上级目录登记过的那一页 */
function owningPageCode(pages: Map<string, string>, rel: string) {
  const segments = viewDirectoryOf(rel).split('/')
  for (let length = segments.length; length > 0; length--) {
    const code = pages.get(segments.slice(0, length).join('/'))
    if (code) {
      return code
    }
  }
  return undefined
}

/** 页面里 schema 的 pageCode 字面量与导出 businessType */
function listPageCodes(): Array<{ file: string, pageCodes: string[], businessTypes: string[] }> {
  const result: Array<{ file: string, pageCodes: string[], businessTypes: string[] }> = []
  for (const file of listVueFiles(SRC_ROOT)) {
    const source = readFileSync(file, 'utf8')
    const pageCodes = [...source.matchAll(PAGE_CODE_LITERAL)].map(match => match[1] ?? '')
    if (pageCodes.length === 0) {
      continue
    }
    const businessTypes = [...source.matchAll(EXPORT_BUSINESS_TYPE)].map(match => match[1] ?? '')
    result.push({ file: relative(SRC_ROOT, file).replaceAll('\\', '/'), pageCodes, businessTypes })
  }
  return result
}

describe('页面码一致性', () => {
  const pages = readRegisteredPages()
  const entries = listPageCodes()

  it('扫得到后端菜单页登记与前端 schema 页面', () => {
    expect(pages.size, '没扫到后端菜单页登记，检查 PageRegistry 的路径或格式').toBeGreaterThan(40)
    expect(entries.length, '没扫到带 pageCode 的页面').toBeGreaterThan(40)
  })

  it('schema 的 pageCode 取所在页面的页面码，一页多表时用「页面码.子表」', () => {
    const offenders: string[] = []
    for (const { file, pageCodes } of entries) {
      const pageCode = owningPageCode(pages, file)
      if (!pageCode) {
        offenders.push(`${file}: 找不到登记这个视图的菜单页`)
        continue
      }
      for (const code of pageCodes) {
        if (code !== pageCode && !code.startsWith(`${pageCode}.`)) {
          offenders.push(`${file}: ${code}（页面码 ${pageCode}）`)
        }
      }
    }

    expect(offenders, `以下 pageCode 与后端页面码不一致：\n${offenders.join('\n')}`).toEqual([])
  })

  it('pageCode 全局唯一：重复就会共用同一份列设置与导入历史', () => {
    const seen = new Map<string, string>()
    const duplicated: string[] = []
    for (const { file, pageCodes } of entries) {
      for (const code of pageCodes) {
        const previous = seen.get(code)
        if (previous) {
          duplicated.push(`${code}: ${previous} / ${file}`)
        }
        seen.set(code, file)
      }
    }

    expect(duplicated, `以下 pageCode 重复：\n${duplicated.join('\n')}`).toEqual([])
  })

  it('导出的 businessType 就是页面码（后端 IExportProvider.BusinessType 同一口径）', () => {
    const offenders: string[] = []
    for (const { file, businessTypes } of entries) {
      const pageCode = owningPageCode(pages, file)
      for (const businessType of businessTypes) {
        if (businessType !== pageCode) {
          offenders.push(`${file}: ${businessType}（页面码 ${pageCode}）`)
        }
      }
    }

    expect(offenders, `以下导出 businessType 与页面码不一致：\n${offenders.join('\n')}`).toEqual([])
  })
})
