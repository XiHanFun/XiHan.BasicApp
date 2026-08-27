/**
 * packages/printing/data-source-registry.ts 的注册期校验与目录查询。
 *
 * 职责边界：本文件专攻「注册时立即失败」的各条校验分支、规范化结果的冻结与归一，
 * 以及目录查询/版本号/tid 生成。重复编码报错与字段目录到 hiprint 协议的映射已在 printing.test.ts 覆盖，
 * 这里不重复。注册表是模块级单例，用例一律使用带序号的唯一编码，保证可任意顺序执行。
 */
import type { PrintDataSourceDefinition, PrintFieldDefinition } from './types'
import { describe, expect, it } from 'vitest'
import {
  getPrintDataSource,
  getPrintDataSourceRegistryVersion,
  getPrintFieldTid,
  listPrintDataSources,
  registerPrintDataSource,
  requirePrintDataSource,
} from './data-source-registry'

let sequence = 0

function uniqueCode(prefix: string): string {
  sequence += 1
  return `${prefix}-${sequence}`
}

function definition(
  patch: Partial<PrintDataSourceDefinition<Record<string, unknown>>> = {},
): PrintDataSourceDefinition<Record<string, unknown>> {
  return {
    code: uniqueCode('ds'),
    name: '数据源',
    fields: [{ key: 'title', label: '标题' }],
    createSampleData: () => ({}),
    ...patch,
  }
}

function fieldOf(patch: Partial<PrintFieldDefinition> = {}): PrintFieldDefinition {
  return { key: 'title', label: '标题', ...patch }
}

describe('数据源级校验', () => {
  it('定义为空或不是对象时立即报错', () => {
    expect(() => registerPrintDataSource(null as unknown as PrintDataSourceDefinition)).toThrow(/不能为空/u)
    expect(() => registerPrintDataSource('ds' as unknown as PrintDataSourceDefinition)).toThrow(/不能为空/u)
  })

  it('编码为空串或纯空白时报错', () => {
    expect(() => registerPrintDataSource(definition({ code: '' }))).toThrow(/编码不能为空/u)
    expect(() => registerPrintDataSource(definition({ code: '   ' }))).toThrow(/编码不能为空/u)
  })

  it('编码含空白字符或超过 100 字时报错', () => {
    expect(() => registerPrintDataSource(definition({ code: 'sales order' }))).toThrow(/不能超过 100 个字符/u)
    expect(() => registerPrintDataSource(definition({ code: 'a\tb' }))).toThrow(/不能超过 100 个字符/u)
    expect(() => registerPrintDataSource(definition({ code: 'x'.repeat(101) }))).toThrow(/不能超过 100 个字符/u)
  })

  it('恰好 100 字的编码可以注册，边界不误杀', () => {
    const code = 'x'.repeat(100)

    expect(registerPrintDataSource(definition({ code })).code).toBe(code)
  })

  it('编码首尾空白被裁掉后作为正式编码落库', () => {
    const code = uniqueCode('trim')

    const registered = registerPrintDataSource(definition({ code: `  ${code}  ` }))

    expect(registered.code).toBe(code)
    expect(getPrintDataSource(code)?.code).toBe(code)
  })

  it('名称为空或纯空白时报错，并在消息里点名是哪个编码', () => {
    const code = uniqueCode('no-name')

    expect(() => registerPrintDataSource(definition({ code, name: '  ' }))).toThrow(new RegExp(`${code} 的名称不能为空`, 'u'))
  })

  it('缺少样例工厂时报错', () => {
    expect(() => registerPrintDataSource(
      definition({ createSampleData: undefined as unknown as () => Record<string, unknown> }),
    )).toThrow(/必须提供 createSampleData/u)
  })

  it('字段列表为空时报错', () => {
    expect(() => registerPrintDataSource(definition({ fields: [] }))).toThrow(/至少需要一个字段/u)
  })
})

describe('字段级校验', () => {
  it('字段编码为空时报错并给出是第几个字段（从 1 数）', () => {
    expect(() => registerPrintDataSource(definition({
      fields: [fieldOf(), fieldOf({ key: '' })],
    }))).toThrow(/第 2 个字段编码不能为空/u)
  })

  it('同一数据源内重复字段编码立即报错', () => {
    expect(() => registerPrintDataSource(definition({
      fields: [fieldOf({ key: 'title' }), fieldOf({ key: 'title', label: '另一个标题' })],
    }))).toThrow(/存在重复字段：title/u)
  })

  it('字段名称为空时报错', () => {
    expect(() => registerPrintDataSource(definition({
      fields: [fieldOf({ label: '   ' })],
    }))).toThrow(/的名称不能为空/u)
  })

  it('字段类型不在受支持集合内时报错并回显该类型', () => {
    expect(() => registerPrintDataSource(definition({
      fields: [fieldOf({ kind: 'chart' as never })],
    }))).toThrow(/类型无效：chart/u)
  })

  it('明细表字段没有列时报错', () => {
    expect(() => registerPrintDataSource(definition({
      fields: [fieldOf({ key: 'details', kind: 'table' })],
    }))).toThrow(/至少需要一列/u)

    expect(() => registerPrintDataSource(definition({
      fields: [fieldOf({ key: 'details', kind: 'table', columns: [] })],
    }))).toThrow(/至少需要一列/u)
  })

  it('明细表列字段为空或列标题为空时报错', () => {
    expect(() => registerPrintDataSource(definition({
      fields: [fieldOf({ key: 'details', kind: 'table', columns: [{ field: '  ', title: '物料' }] })],
    }))).toThrow(/列字段不能为空/u)

    expect(() => registerPrintDataSource(definition({
      fields: [fieldOf({ key: 'details', kind: 'table', columns: [{ field: 'code', title: ' ' }] })],
    }))).toThrow(/列标题不能为空/u)
  })

  it('模拟数据控件类型不在白名单内时报错，字段级与列级都拦', () => {
    expect(() => registerPrintDataSource(definition({
      fields: [fieldOf({ inputType: 'richtext' as never })],
    }))).toThrow(/控件类型无效：richtext/u)

    expect(() => registerPrintDataSource(definition({
      fields: [fieldOf({
        key: 'details',
        kind: 'table',
        columns: [{ field: 'code', title: '编码', inputType: 'richtext' as never }],
      })],
    }))).toThrow(/控件类型无效：richtext/u)
  })

  it('六种受支持的控件类型全部放行', () => {
    const registered = registerPrintDataSource(definition({
      fields: (['boolean', 'date', 'datetime', 'number', 'text', 'textarea'] as const)
        .map(inputType => fieldOf({ key: `f_${inputType}`, label: inputType, inputType })),
    }))

    expect(registered.fields.map(field => field.inputType))
      .toStrictEqual(['boolean', 'date', 'datetime', 'number', 'text', 'textarea'])
  })

  it('校验一旦失败该数据源不进注册表，编码可被后续正确定义重新占用', () => {
    const code = uniqueCode('retry')
    expect(() => registerPrintDataSource(definition({ code, fields: [] }))).toThrow(/至少需要一个字段/u)

    expect(getPrintDataSource(code)).toBeUndefined()
    expect(registerPrintDataSource(definition({ code })).code).toBe(code)
  })
})

describe('规范化结果', () => {
  it('字段编码与名称的首尾空白被裁掉', () => {
    const registered = registerPrintDataSource(definition({
      fields: [fieldOf({ key: '  amount  ', label: '  金额  ' })],
    }))

    expect(registered.fields[0]).toMatchObject({ key: 'amount', label: '金额' })
  })

  it('未声明类型的字段默认按文本处理', () => {
    const registered = registerPrintDataSource(definition({ fields: [fieldOf()] }))

    expect(registered.fields[0]?.kind).toBe('text')
  })

  it('非明细表字段的列一律归一为 undefined，即使调用方传了列', () => {
    const registered = registerPrintDataSource(definition({
      fields: [fieldOf({ kind: 'text', columns: [{ field: 'code', title: '编码' }] })],
    }))

    expect(registered.fields[0]?.columns).toBeUndefined()
  })

  it('纯空白占位文案等同未配置，避免设计器显示一片空白提示', () => {
    const registered = registerPrintDataSource(definition({
      fields: [fieldOf({ placeholder: '   ' }), fieldOf({ key: 'note', label: '备注', placeholder: '  请填写  ' })],
    }))

    expect(registered.fields[0]?.placeholder).toBeUndefined()
    expect(registered.fields[1]?.placeholder).toBe('请填写')
  })

  it('明细表列的标题与字段被裁剪，空白占位同样归一', () => {
    const registered = registerPrintDataSource(definition({
      fields: [fieldOf({
        key: 'details',
        kind: 'table',
        columns: [{ field: '  code  ', title: '  物料编码  ', placeholder: '  ' }],
      })],
    }))

    expect(registered.fields[0]?.columns?.[0]).toMatchObject({ field: 'code', title: '物料编码' })
    expect(registered.fields[0]?.columns?.[0]?.placeholder).toBeUndefined()
  })

  it('注册结果被冻结，拿到手后改不动字段契约', () => {
    const registered = registerPrintDataSource(definition())

    expect(Object.isFrozen(registered)).toBe(true)
    expect(Object.isFrozen(registered.fields)).toBe(true)
    expect(Object.isFrozen(registered.fields[0])).toBe(true)
  })

  it('注册后再改传进去的原始定义，不会影响注册表里的快照', () => {
    const fields: PrintFieldDefinition[] = [fieldOf({ key: 'title', label: '标题' })]
    const source = definition({ fields })

    const registered = registerPrintDataSource(source)
    fields.push(fieldOf({ key: 'late', label: '迟到字段' }))

    expect(registered.fields).toHaveLength(1)
    expect(getPrintDataSource(source.code)?.fields).toHaveLength(1)
  })
})

describe('目录查询', () => {
  it('编码为空、null 或 undefined 时查不到任何数据源', () => {
    expect(getPrintDataSource('')).toBeUndefined()
    expect(getPrintDataSource('   ')).toBeUndefined()
    expect(getPrintDataSource(null)).toBeUndefined()
    expect(getPrintDataSource(undefined)).toBeUndefined()
  })

  it('查询时容忍首尾空白', () => {
    const code = uniqueCode('lookup')
    registerPrintDataSource(definition({ code }))

    expect(getPrintDataSource(`  ${code}  `)?.code).toBe(code)
  })

  it('未注册编码查不到，requirePrintDataSource 则直接报错并提示先注册', () => {
    expect(getPrintDataSource('never-registered')).toBeUndefined()
    expect(() => requirePrintDataSource('never-registered')).toThrow(/未注册：never-registered/u)
    expect(() => requirePrintDataSource('never-registered')).toThrow(/registerPrintDataSource/u)
  })

  it('requirePrintDataSource 对空编码先报「编码不能为空」而不是「未注册」', () => {
    expect(() => requirePrintDataSource('  ')).toThrow(/编码不能为空/u)
  })

  it('requirePrintDataSource 命中时返回的就是注册表里的同一个冻结对象', () => {
    const code = uniqueCode('require')
    const registered = registerPrintDataSource(definition({ code }))

    expect(requirePrintDataSource(code)).toBe(registered)
  })

  it('目录列表按编码升序返回，与注册先后无关', () => {
    const suffix = uniqueCode('order')
    registerPrintDataSource(definition({ code: `zzz-${suffix}` }))
    registerPrintDataSource(definition({ code: `aaa-${suffix}` }))

    const codes = listPrintDataSources().map(item => item.code).filter(code => code.endsWith(suffix))
    expect(codes).toStrictEqual([`aaa-${suffix}`, `zzz-${suffix}`])
  })
})

describe('注册表版本号', () => {
  it('每成功注册一次版本号加一，适配层据此决定是否重建 provider', () => {
    const before = getPrintDataSourceRegistryVersion()

    registerPrintDataSource(definition())
    registerPrintDataSource(definition())

    expect(getPrintDataSourceRegistryVersion()).toBe(before + 2)
  })

  it('注册失败不推进版本号，避免白白重建 provider 打断引擎连接', () => {
    const before = getPrintDataSourceRegistryVersion()

    expect(() => registerPrintDataSource(definition({ fields: [] }))).toThrow()

    expect(getPrintDataSourceRegistryVersion()).toBe(before)
  })
})

describe('字段 tid 生成', () => {
  it('按 xihan.数据源编码.字段路径 三段拼接，供 provider 与拖拽元素共用', () => {
    expect(getPrintFieldTid('sales', 'title')).toBe('xihan.sales.title')
  })

  it('嵌套字段路径原样保留点号，不做二次转义', () => {
    expect(getPrintFieldTid('sales', 'customer.name')).toBe('xihan.sales.customer.name')
  })

  it('不同数据源的同名字段得到不同 tid，设计器素材不会互相顶掉', () => {
    expect(getPrintFieldTid('a', 'title')).not.toBe(getPrintFieldTid('b', 'title'))
  })
})
