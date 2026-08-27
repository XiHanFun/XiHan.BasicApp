/**
 * 业务模块目录约定测试（src/modules/<模块>/）。
 *
 * 职责边界：把 scripts/validate-modules.mjs 的构建门禁口径固化成用例——目录结构白名单、
 * 视图键唯一、locales 成对——并补齐脚本没覆盖的部分：两语言 key 集合一致、文案非空、
 * setup.ts 默认导出钩子、模块名规范。
 * 路径枚举走 import.meta.glob（构建期展开），不在用例里做任何文件系统读写。
 */
import { describe, expect, it } from 'vitest'

/** 模块目录顶层允许出现的条目（与 scripts/validate-modules.mjs 的白名单一致） */
const ALLOWED_ENTRIES = new Set(['views', 'api', 'locales', 'setup.ts', 'README.md'])
/** locales 目录只允许这两个语言文件，且必须成对 */
const ALLOWED_LOCALES = ['zh-CN.ts', 'en-US.ts']

// 非 eager：只要文件路径清单，不加载任何内容
const modulePaths = Object.keys(import.meta.glob('/src/modules/**/*', { query: '?raw', import: 'default' }))
const appViewPaths = Object.keys(import.meta.glob('/src/views/**/*.vue', { query: '?raw', import: 'default' }))
const setupSources = import.meta.glob<string>('/src/modules/*/setup.ts', {
  query: '?raw',
  import: 'default',
  eager: true,
})
const localeModules = import.meta.glob<{ default?: Record<string, unknown> }>(
  '/src/modules/*/locales/*.ts',
  { eager: true },
)

/** 模块名 → 顶层条目集合 */
const moduleEntries = new Map<string, Set<string>>()
for (const path of modulePaths) {
  const rest = path.slice('/src/modules/'.length).split('/')
  const [moduleName, entry] = rest
  if (!moduleName || !entry) {
    continue
  }
  const entries = moduleEntries.get(moduleName) ?? new Set<string>()
  entries.add(entry)
  moduleEntries.set(moduleName, entries)
}

const moduleNames = [...moduleEntries.keys()].sort()

function moduleFiles(moduleName: string, subDir: string) {
  const prefix = `/src/modules/${moduleName}/${subDir}/`
  return modulePaths.filter(path => path.startsWith(prefix))
}

/** 把嵌套文案对象拍平成 `a.b.c` → 叶子值 */
function flatten(input: Record<string, unknown>, prefix = '', out = new Map<string, unknown>()) {
  for (const [key, value] of Object.entries(input)) {
    const path = prefix ? `${prefix}.${key}` : key
    if (typeof value === 'object' && value !== null && !Array.isArray(value)) {
      flatten(value as Record<string, unknown>, path, out)
    }
    else {
      out.set(path, value)
    }
  }
  return out
}

function localeMessages(moduleName: string, locale: string) {
  const messages = localeModules[`/src/modules/${moduleName}/locales/${locale}.ts`]?.default
  if (!messages) {
    throw new Error(`模块 ${moduleName} 缺少 ${locale} 文案`)
  }
  return flatten(messages)
}

describe('模块目录结构白名单', () => {
  it('至少注册了一个业务模块，且模块清单可被枚举到', () => {
    expect(moduleNames.length).toBeGreaterThanOrEqual(5)
    expect(moduleNames).toContain('printing')
  })

  it('模块名一律小写短横线，不含空格、下划线与大写', () => {
    const bad = moduleNames.filter(name => !/^[a-z][a-z0-9]*(?:-[a-z0-9]+)*$/.test(name))

    expect(bad).toEqual([])
  })

  it('模块目录顶层只允许 views / api / locales / setup.ts / README.md', () => {
    const bad: string[] = []
    for (const [moduleName, entries] of moduleEntries) {
      for (const entry of entries) {
        if (!ALLOWED_ENTRIES.has(entry)) {
          bad.push(`${moduleName}/${entry}`)
        }
      }
    }

    expect(bad).toEqual([])
  })

  it('每个模块至少提供 views 或 api，不存在空壳模块', () => {
    const empty = moduleNames.filter((name) => {
      const entries = moduleEntries.get(name)
      return !entries?.has('views') && !entries?.has('api')
    })

    expect(empty).toEqual([])
  })
})

describe('模块视图键的唯一性', () => {
  it('模块视图重键为 /src/views/** 后，既不与应用视图撞键，也不在模块之间互撞', () => {
    const owners = new Map<string, string>()
    const conflicts: string[] = []

    for (const path of appViewPaths) {
      owners.set(path.slice('/src/views/'.length), 'src/views')
    }

    for (const moduleName of moduleNames) {
      const prefix = `/src/modules/${moduleName}/views/`
      for (const path of modulePaths) {
        if (!path.startsWith(prefix) || !path.endsWith('.vue')) {
          continue
        }
        const key = path.slice(prefix.length)
        const owner = owners.get(key)
        if (owner) {
          conflicts.push(`${key} 同时来自 ${owner} 与 modules/${moduleName}`)
        }
        else {
          owners.set(key, `modules/${moduleName}`)
        }
      }
    }

    expect(conflicts).toEqual([])
  })

  it('模块视图路径里不出现 index.ts 之类的路由入口文件，视图目录只放 .vue 与其配套 .ts', () => {
    const bad: string[] = []
    for (const moduleName of moduleNames) {
      for (const path of moduleFiles(moduleName, 'views')) {
        if (!/\.(?:vue|ts)$/.test(path)) {
          bad.push(path)
        }
      }
    }

    expect(bad).toEqual([])
  })
})

describe('模块文案的成对与齐全', () => {
  it('locales 目录只放 zh-CN.ts 与 en-US.ts，且两语言必须成对', () => {
    const bad: string[] = []
    for (const moduleName of moduleNames) {
      const files = moduleFiles(moduleName, 'locales').map(path => path.split('/').at(-1) ?? '')
      if (files.length === 0) {
        continue
      }
      for (const file of files) {
        if (!ALLOWED_LOCALES.includes(file)) {
          bad.push(`${moduleName}/locales/${file} 不在允许清单`)
        }
      }
      for (const required of ALLOWED_LOCALES) {
        if (!files.includes(required)) {
          bad.push(`${moduleName}/locales 缺少 ${required}`)
        }
      }
    }

    expect(bad).toEqual([])
  })

  it('每个模块的两语言 key 集合完全一致——缺 key 会在切语言时渲染出裸键', () => {
    const bad: string[] = []
    let comparedKeys = 0
    for (const moduleName of moduleNames) {
      if (moduleFiles(moduleName, 'locales').length === 0) {
        continue
      }
      const zh = localeMessages(moduleName, 'zh-CN')
      const en = localeMessages(moduleName, 'en-US')
      comparedKeys += zh.size
      for (const key of zh.keys()) {
        if (!en.has(key)) {
          bad.push(`${moduleName}: en-US 缺 ${key}`)
        }
      }
      for (const key of en.keys()) {
        if (!zh.has(key)) {
          bad.push(`${moduleName}: zh-CN 缺 ${key}`)
        }
      }
    }

    expect(comparedKeys).toBeGreaterThan(500)
    expect(bad).toEqual([])
  })

  it('所有文案叶子值都是非空字符串，不允许 null / 空串占位', () => {
    const bad: string[] = []
    for (const moduleName of moduleNames) {
      if (moduleFiles(moduleName, 'locales').length === 0) {
        continue
      }
      for (const locale of ALLOWED_LOCALES) {
        const messages = localeMessages(moduleName, locale.replace('.ts', ''))
        for (const [key, value] of messages) {
          if (typeof value !== 'string' || value.trim().length === 0) {
            bad.push(`${moduleName}/${locale}: ${key}`)
          }
        }
      }
    }

    expect(bad).toEqual([])
  })

  it('模块文案覆盖到菜单键：删除模块目录时菜单文案随之消失', () => {
    const withMenu = moduleNames.filter((name) => {
      if (moduleFiles(name, 'locales').length === 0) {
        return false
      }
      return [...localeMessages(name, 'zh-CN').keys()].some(key => key.startsWith('menu.'))
    })

    expect(withMenu.length).toBeGreaterThan(0)
  })
})

describe('模块启动钩子约定', () => {
  it('存在 setup.ts 的模块都默认导出一个启动函数——main.ts 只认 default 调用', () => {
    const bad: string[] = []
    for (const [path, source] of Object.entries(setupSources)) {
      if (!/export default function\s+\w+\s*\(\s*\)/.test(source)) {
        bad.push(path)
      }
    }

    expect(Object.keys(setupSources).length).toBeGreaterThan(0)
    expect(bad).toEqual([])
  })

  it('setup.ts 的启动函数不接收参数——注册钩子不依赖调用方传上下文', () => {
    const bad = Object.entries(setupSources)
      .filter(([, source]) => /export default function\s+\w+\s*\([^)]+\)/.test(source))
      .map(([path]) => path)

    expect(bad).toEqual([])
  })

  it('声明了 setup.ts 的模块与实际存在 setup.ts 的模块清单一致', () => {
    const declared = moduleNames.filter(name => moduleEntries.get(name)?.has('setup.ts'))
    const globbed = Object.keys(setupSources)
      .map(path => path.slice('/src/modules/'.length).split('/')[0] ?? '')
      .sort()

    expect(globbed).toEqual(declared.sort())
  })
})
