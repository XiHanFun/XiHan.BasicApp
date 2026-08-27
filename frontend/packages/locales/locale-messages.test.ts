/**
 * packages/locales 语言包结构性校验。
 *
 * 职责边界：只校验 packages（admin shell）自带的两份聚合语言包
 * （langs/zh-CN.ts 与 langs/en-US.ts）的**结构契约**——键集对称、值形态、占位符一致、命名空间集合，
 * 不校验译文是否地道、也不涉及 src 业务文案（那部分由 scripts/validate-i18n.mjs 全库门禁负责）。
 *
 * 口径与 scripts/validate-i18n.mjs 对齐：把嵌套对象 flatten 成 `a.b.c` 扁平键后比较集合，
 * 差异必须报出具体 key，而不是只报一个数量。
 */
import { describe, expect, it } from 'vitest'
import enUS from './langs/en-US'
import zhCN from './langs/zh-CN'

type LocaleTree = Record<string, unknown>

/** 与 validate-i18n.mjs 的 flatten 同口径：数组视为叶子，普通对象继续下钻 */
function flatten(tree: LocaleTree, prefix = '', acc = new Map<string, unknown>()): Map<string, unknown> {
  for (const key of Object.keys(tree)) {
    const value = tree[key]
    const path = prefix ? `${prefix}.${key}` : key
    if (value && typeof value === 'object' && !Array.isArray(value)) {
      flatten(value as LocaleTree, path, acc)
    }
    else {
      acc.set(path, value)
    }
  }
  return acc
}

/** 抽出 vue-i18n 具名插值占位符 `{name}`，返回去重后排序的名字列表 */
function placeholdersOf(text: string): string[] {
  return [...new Set([...text.matchAll(/\{(\w+)\}/gu)].map(match => match[1] as string))].sort()
}

const zhFlat = flatten(zhCN as unknown as LocaleTree)
const enFlat = flatten(enUS as unknown as LocaleTree)

/** packages 只带 shell 自身的命名空间，业务命名空间住 src/locales（见 langs/*.ts 顶部注释） */
const SHELL_NAMESPACES = [
  'check_updates',
  'common',
  'component',
  'error',
  'header',
  'island',
  'menu',
  'page',
  'preference',
  'tabbar',
] as const

describe('语言包键集对称性', () => {
  it('zh-CN 有而 en-US 缺的键必须为空，缺失时报出具体 key', () => {
    const onlyZh = [...zhFlat.keys()].filter(key => !enFlat.has(key)).sort()

    expect(onlyZh).toStrictEqual([])
  })

  it('en-US 有而 zh-CN 缺的键必须为空，多余时报出具体 key', () => {
    const onlyEn = [...enFlat.keys()].filter(key => !zhFlat.has(key)).sort()

    expect(onlyEn).toStrictEqual([])
  })

  it('两份语言包的扁平键总数相同，杜绝一边嵌套一边扁平导致的形状分叉', () => {
    expect(enFlat.size).toBe(zhFlat.size)
    expect(zhFlat.size).toBeGreaterThan(0)
  })

  it('同一 key 在两份语言包中都落在叶子层，不出现一边是字符串一边是子树', () => {
    const shapeConflicts = [...zhFlat.keys()].filter((key) => {
      const zhValue = zhFlat.get(key)
      const enValue = enFlat.get(key)
      return typeof zhValue !== typeof enValue
    })

    expect(shapeConflicts).toStrictEqual([])
  })
})

describe('语言包取值形态', () => {
  it('所有叶子值都是字符串，不掺入数字、布尔、null 或函数', () => {
    const nonString = [...zhFlat, ...enFlat]
      .filter(([, value]) => typeof value !== 'string')
      .map(([key, value]) => `${key} => ${typeof value}`)
      .sort()

    expect(nonString).toStrictEqual([])
  })

  it('没有空串或纯空白的译文，避免界面渲染出空白按钮/标题', () => {
    const blank = [...zhFlat, ...enFlat]
      .filter(([, value]) => typeof value === 'string' && !value.trim())
      .map(([key]) => key)
      .sort()

    expect(blank).toStrictEqual([])
  })

  it('除三个声明过的连接符外，译文首尾不带空白，防止拼接标题时出现双空格', () => {
    const padded = [...new Set(
      [...zhFlat, ...enFlat]
        .filter(([, value]) => typeof value === 'string' && value !== value.trim())
        .map(([key]) => key),
    )].sort()

    // 这三条是刻意留边距的连接符（英文版为 ', ' / '; ' / ' · '），不是漏改的文案
    expect(padded).toStrictEqual([
      'component.schema_import.column_join',
      'component.schema_import.error_reason_join',
      'tabbar.tab_hint_sep',
    ])
  })

  it('键的每一段不含点号与空白，保证 t(\'a.b.c\') 的层级寻址不被打断', () => {
    const badSegments = [...zhFlat.keys()]
      .filter(key => key.split('.').some(segment => !segment || /\s/u.test(segment)))
      .sort()

    expect(badSegments).toStrictEqual([])
  })
})

describe('占位符跨语言一致', () => {
  it('同一 key 的具名占位符集合在两份语言包中完全一致', () => {
    const mismatched: string[] = []
    for (const [key, zhValue] of zhFlat) {
      const enValue = enFlat.get(key)
      if (typeof zhValue !== 'string' || typeof enValue !== 'string') {
        continue
      }
      const zhNames = placeholdersOf(zhValue)
      const enNames = placeholdersOf(enValue)
      if (zhNames.join(',') !== enNames.join(',')) {
        mismatched.push(`${key}: zh={${zhNames.join('|')}} en={${enNames.join('|')}}`)
      }
    }

    expect(mismatched).toStrictEqual([])
  })

  it('占位符没有写成 {} 或 { name } 这类 vue-i18n 无法解析的形态', () => {
    const malformed = [...zhFlat, ...enFlat]
      .filter(([, value]) => typeof value === 'string' && /\{\s*\}|\{\s+\w|\w\s+\}/u.test(value))
      .map(([key]) => key)
      .sort()

    expect(malformed).toStrictEqual([])
  })

  it('确有带占位符的文案参与校验，避免这组断言在零样本上空转', () => {
    const withPlaceholder = [...zhFlat].filter(
      ([, value]) => typeof value === 'string' && placeholdersOf(value).length > 0,
    )

    expect(withPlaceholder.length).toBeGreaterThan(10)
  })
})

describe('命名空间边界', () => {
  it('两份语言包的顶层命名空间就是 shell 自带的那一组，业务命名空间不得混入', () => {
    expect(Object.keys(zhCN).sort()).toStrictEqual([...SHELL_NAMESPACES])
    expect(Object.keys(enUS).sort()).toStrictEqual([...SHELL_NAMESPACES])
  })

  it('检查更新命名空间对外是 check_updates 蛇形键，而非导入名 checkUpdates', () => {
    expect(Object.hasOwn(zhCN, 'check_updates')).toBe(true)
    expect(Object.hasOwn(zhCN, 'checkUpdates')).toBe(false)
    expect(Object.hasOwn(enUS, 'check_updates')).toBe(true)
    expect(Object.hasOwn(enUS, 'checkUpdates')).toBe(false)
  })

  it('每个命名空间在两份语言包里都非空，避免整块文案漏译', () => {
    const emptyNamespaces: string[] = []
    for (const namespace of SHELL_NAMESPACES) {
      for (const [locale, tree] of [['zh-CN', zhCN], ['en-US', enUS]] as const) {
        const branch = (tree as unknown as LocaleTree)[namespace]
        if (!branch || typeof branch !== 'object' || flatten(branch as LocaleTree).size === 0) {
          emptyNamespaces.push(`${locale}.${namespace}`)
        }
      }
    }

    expect(emptyNamespaces).toStrictEqual([])
  })
})
