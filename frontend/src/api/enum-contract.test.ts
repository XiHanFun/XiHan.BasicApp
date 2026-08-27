/**
 * 前后端枚举线值契约测试。
 *
 * 职责边界：后端开了全局 JsonStringEnumConverter——业务枚举一律以「成员名」作为线上值。
 * 前端凡是字符串枚举就必须满足 `成员名 === 取值`，否则筛选/回显会静默失配（不报错、只是永远筛不出）。
 * 这里遍历 src/api/modules 与 src/modules/<模块>/api 下的全部枚举做统一校验，
 * 数字枚举（[Flags] 与查询算子）另按数字口径校验。
 */
import { describe, expect, it } from 'vitest'

type EnumLike = Record<string, number | string>

interface DiscoveredEnum {
  source: string
  name: string
  target: EnumLike
}

/**
 * 判定一个导出是否是 TypeScript 枚举对象：
 * 纯对象、成员非空、取值只有字符串或数字，且不含函数/嵌套对象。
 */
function isEnumLike(value: unknown): value is EnumLike {
  if (typeof value !== 'object' || value === null || Array.isArray(value)) {
    return false
  }
  const entries = Object.entries(value)
  return entries.length > 0
    && entries.every(([, item]) => typeof item === 'string' || typeof item === 'number')
}

// chat.types.ts 会值转出 `~/chat`（链到没有构建产物的 @xihan-ui），必须排除
const globbed = {
  ...import.meta.glob<Record<string, unknown>>('/src/api/modules/**/*.ts', { eager: true }),
  ...import.meta.glob<Record<string, unknown>>(
    ['/src/modules/*/api/**/*.ts', '!/src/modules/chat/api/chat.types.ts'],
    { eager: true },
  ),
}

const discovered: DiscoveredEnum[] = []
const seen = new Set<object>()
for (const [source, moduleExports] of Object.entries(globbed)) {
  for (const [name, exported] of Object.entries(moduleExports)) {
    if (!isEnumLike(exported) || seen.has(exported)) {
      continue
    }
    seen.add(exported)
    discovered.push({ source, name, target: exported })
  }
}

/** 字符串枚举：全部成员取值都是字符串 */
const stringEnums = discovered.filter(item => Object.values(item.target).every(value => typeof value === 'string'))
/** 数字枚举：存在数字取值（TS 会额外生成反向映射键） */
const numericEnums = discovered.filter(item => Object.values(item.target).some(value => typeof value === 'number'))

/** 取数字枚举的正向成员（过滤掉 TS 生成的反向映射） */
function forwardEntries(target: EnumLike) {
  return Object.entries(target).filter(([key]) => !/^\d+$/.test(key))
}

describe('枚举发现的完整性', () => {
  it('扫描到的枚举数量与业务规模相符——整块模块被漏扫时这里先失败', () => {
    expect(discovered.length).toBeGreaterThan(40)
    expect(stringEnums.length).toBeGreaterThan(35)
    expect(numericEnums.length).toBeGreaterThan(0)
  })

  it('字符串枚举与数字枚举互斥，不存在混合取值的枚举', () => {
    const mixed = discovered
      .filter(item => Object.values(item.target).some(value => typeof value === 'string')
        && Object.values(item.target).some(value => typeof value === 'number'))
      .filter(item => forwardEntries(item.target).some(([, value]) => typeof value === 'string'))
      .map(item => `${item.source} → ${item.name}`)

    expect(mixed).toEqual([])
  })
})

describe('字符串枚举的线值约定', () => {
  it('成员名即线上值——后端 JsonStringEnumConverter 按成员名序列化', () => {
    const bad: string[] = []
    for (const { source, name, target } of stringEnums) {
      for (const [key, value] of Object.entries(target)) {
        if (key !== value) {
          bad.push(`${source} → ${name}.${key} = ${String(value)}`)
        }
      }
    }

    expect(bad).toEqual([])
  })

  it('成员名是大驼峰标识符，不含空格、短横线与中文；仅两个平台名沿用官方小写写法', () => {
    // 后端 DeviceType 沿用 Apple 官方写法（iOS / macOS），线上值也是这两个串，不能改成大驼峰
    const platformExceptions = new Set(['iOS', 'macOS'])
    const bad: string[] = []
    for (const { source, name, target } of stringEnums) {
      for (const key of Object.keys(target)) {
        if (!/^[A-Z][A-Za-z0-9]*$/.test(key) && !platformExceptions.has(key)) {
          bad.push(`${source} → ${name}.${key}`)
        }
      }
    }

    expect(bad).toEqual([])
  })

  it('小写开头的成员只有 DeviceType 的两个平台名，别处不得照此放宽', () => {
    const lowercaseLeading: string[] = []
    for (const { name, target } of stringEnums) {
      for (const key of Object.keys(target)) {
        if (!/^[A-Z]/.test(key)) {
          lowercaseLeading.push(`${name}.${key}`)
        }
      }
    }

    expect([...new Set(lowercaseLeading)].sort()).toEqual(['DeviceType.iOS', 'DeviceType.macOS'])
  })

  it('同一个枚举内取值不重复', () => {
    const bad = stringEnums
      .filter(({ target }) => new Set(Object.values(target)).size !== Object.keys(target).length)
      .map(({ source, name }) => `${source} → ${name}`)

    expect(bad).toEqual([])
  })
})

describe('数字枚举的取值约定', () => {
  it('正向成员名唯一且取值不重复', () => {
    const bad: string[] = []
    for (const { source, name, target } of numericEnums) {
      const forward = forwardEntries(target)
      if (new Set(forward.map(([, value]) => value)).size !== forward.length) {
        bad.push(`${source} → ${name}`)
      }
    }

    expect(bad).toEqual([])
  })

  it('反向映射与正向映射一一对应，取值都是有限整数', () => {
    const bad: string[] = []
    for (const { source, name, target } of numericEnums) {
      for (const [key, value] of forwardEntries(target)) {
        if (typeof value !== 'number' || !Number.isInteger(value)) {
          bad.push(`${source} → ${name}.${key} 不是整数`)
          continue
        }
        if (target[value] !== key) {
          bad.push(`${source} → ${name}.${key} 缺少反向映射`)
        }
      }
    }

    expect(bad).toEqual([])
  })
})
