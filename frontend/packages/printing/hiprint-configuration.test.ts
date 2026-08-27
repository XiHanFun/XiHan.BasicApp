/**
 * packages/printing/hiprint-adapter.ts 的配置装配与字段元素协议映射边界。
 *
 * 职责边界：只覆盖不需要真实 hiprint 引擎的两块——configurePrinting 的默认值归一与失败前置，
 * 以及 buildHiprintElementDefinitions 对 table 列的补全规则。
 * 引擎动态加载、打印机刷新与事件清理属于设备相关路径，由 printing.test.ts 经适配器替身覆盖。
 *
 * 配置是模块级单例，每个用例都 vi.resetModules() 后重新导入，可任意顺序执行。
 */
import type { PrintDataSourceDefinition, PrintingAdapter, PrintingConfiguration } from './types'
import { afterEach, describe, expect, it, vi } from 'vitest'

type AdapterModule = typeof import('./hiprint-adapter')

async function loadAdapter(): Promise<AdapterModule> {
  vi.resetModules()
  return import('./hiprint-adapter')
}

function baseConfiguration(patch: Partial<PrintingConfiguration> = {}): PrintingConfiguration {
  return {
    resolveTemplate: () => Promise.reject(new Error('未预期的模板解析调用')),
    ...patch,
  } as PrintingConfiguration
}

function source(patch: Partial<PrintDataSourceDefinition> = {}): PrintDataSourceDefinition {
  return {
    code: 'sales',
    name: '销售单',
    fields: [{ key: 'title', label: '标题' }],
    createSampleData: () => ({}),
    ...patch,
  } as PrintDataSourceDefinition
}

afterEach(() => {
  vi.unstubAllEnvs()
  vi.resetModules()
})

describe('配置前置校验', () => {
  it('尚未配置时取配置直接报错，宽松入口则返回 null', async () => {
    const { getPrintingConfiguration, tryGetPrintingConfiguration } = await loadAdapter()

    expect(tryGetPrintingConfiguration()).toBeNull()
    expect(() => getPrintingConfiguration()).toThrow(/尚未初始化/u)
  })

  it('缺少模板解析函数时抛 TypeError，且配置不落地', async () => {
    const { configurePrinting, tryGetPrintingConfiguration } = await loadAdapter()

    expect(() => configurePrinting({} as PrintingConfiguration)).toThrow(TypeError)
    expect(() => configurePrinting({} as PrintingConfiguration)).toThrow(/必须配置 resolveTemplate/u)
    expect(tryGetPrintingConfiguration()).toBeNull()
  })

  it('传 null 或把 resolveTemplate 写成非函数同样报错', async () => {
    const { configurePrinting } = await loadAdapter()

    expect(() => configurePrinting(null as unknown as PrintingConfiguration)).toThrow(/必须配置 resolveTemplate/u)
    expect(() => configurePrinting({ resolveTemplate: 'nope' } as unknown as PrintingConfiguration))
      .toThrow(/必须配置 resolveTemplate/u)
  })
})

describe('主机与令牌归一', () => {
  it('省略 host/token 且环境变量为空时用内置安全默认值', async () => {
    vi.stubEnv('VITE_HIPRINT_HOST', '')
    vi.stubEnv('VITE_HIPRINT_TOKEN', '')
    const { configurePrinting, getPrintingConfiguration } = await loadAdapter()

    configurePrinting(baseConfiguration())

    expect(getPrintingConfiguration()).toMatchObject({
      host: 'http://localhost:17521',
      token: 'vue-plugin-hiprint',
    })
  })

  it('环境变量存在时优先于内置默认值', async () => {
    vi.stubEnv('VITE_HIPRINT_HOST', 'http://10.0.0.9:17521')
    vi.stubEnv('VITE_HIPRINT_TOKEN', 'env-token')
    const { configurePrinting, getPrintingConfiguration } = await loadAdapter()

    configurePrinting(baseConfiguration())

    expect(getPrintingConfiguration()).toMatchObject({
      host: 'http://10.0.0.9:17521',
      token: 'env-token',
    })
  })

  it('显式传参优先于环境变量，并裁掉首尾空白', async () => {
    vi.stubEnv('VITE_HIPRINT_HOST', 'http://10.0.0.9:17521')
    const { configurePrinting, getPrintingConfiguration } = await loadAdapter()

    configurePrinting(baseConfiguration({ host: '  http://printer.local:17521  ', token: '  my-token  ' }))

    expect(getPrintingConfiguration()).toMatchObject({
      host: 'http://printer.local:17521',
      token: 'my-token',
    })
  })

  it('纯空白的显式传参等同未传，回落到环境变量', async () => {
    vi.stubEnv('VITE_HIPRINT_HOST', 'http://10.0.0.9:17521')
    vi.stubEnv('VITE_HIPRINT_TOKEN', 'env-token')
    const { configurePrinting, getPrintingConfiguration } = await loadAdapter()

    configurePrinting(baseConfiguration({ host: '   ', token: '  ' }))

    expect(getPrintingConfiguration()).toMatchObject({
      host: 'http://10.0.0.9:17521',
      token: 'env-token',
    })
  })

  it('模板解析函数原样保留，不被默认值归一覆盖掉', async () => {
    const resolveTemplate = vi.fn()
    const { configurePrinting, getPrintingConfiguration } = await loadAdapter()

    configurePrinting(baseConfiguration({ resolveTemplate: resolveTemplate as unknown as PrintingConfiguration['resolveTemplate'] }))

    expect(getPrintingConfiguration().resolveTemplate).toBe(resolveTemplate)
  })

  it('重新配置整体替换旧配置，重新登录后不会残留上一轮的地址', async () => {
    vi.stubEnv('VITE_HIPRINT_HOST', '')
    const { configurePrinting, getPrintingConfiguration } = await loadAdapter()
    configurePrinting(baseConfiguration({ host: 'http://old:17521' }))

    configurePrinting(baseConfiguration())

    expect(getPrintingConfiguration().host).toBe('http://localhost:17521')
  })
})

describe('适配器替换', () => {
  it('注入替身后取适配器不触碰真实引擎，拿到的就是注入的那个实例', async () => {
    const { getPrintingAdapter, setPrintingAdapter } = await loadAdapter()
    const fake = { isClientConnected: () => false } as unknown as PrintingAdapter

    setPrintingAdapter(fake)

    await expect(getPrintingAdapter()).resolves.toBe(fake)
  })

  it('替身优先级高于配置：先配置再注入，取到的仍是替身', async () => {
    const { configurePrinting, getPrintingAdapter, setPrintingAdapter } = await loadAdapter()
    const fake = { isClientConnected: () => true } as unknown as PrintingAdapter
    configurePrinting(baseConfiguration())

    setPrintingAdapter(fake)

    await expect(getPrintingAdapter()).resolves.toBe(fake)
  })

  it('后注入的替身覆盖先注入的', async () => {
    const { getPrintingAdapter, setPrintingAdapter } = await loadAdapter()
    const latest = { isClientConnected: () => true } as unknown as PrintingAdapter

    setPrintingAdapter({ isClientConnected: () => false } as unknown as PrintingAdapter)
    setPrintingAdapter(latest)

    await expect(getPrintingAdapter()).resolves.toBe(latest)
  })
})

describe('字段元素协议映射', () => {
  it('未声明类型的字段按 text 输出，且不带 textType 键', async () => {
    const { buildHiprintElementDefinitions } = await loadAdapter()

    const [element] = buildHiprintElementDefinitions(source())

    expect(element).toMatchObject({ tid: 'xihan.sales.title', title: '标题', field: 'title', type: 'text' })
    expect(Object.hasOwn(element ?? {}, 'textType')).toBe(false)
    expect(Object.hasOwn(element ?? {}, 'columns')).toBe(false)
    expect(Object.hasOwn(element ?? {}, 'editable')).toBe(false)
  })

  it('图片字段保持 image 类型，同样不带 textType', async () => {
    const { buildHiprintElementDefinitions } = await loadAdapter()

    const [element] = buildHiprintElementDefinitions(source({
      fields: [{ key: 'logo', label: '图标', kind: 'image' }],
    }))

    expect(element?.type).toBe('image')
    expect(Object.hasOwn(element ?? {}, 'textType')).toBe(false)
  })

  it('明细表列宽缺省补 100，显式列宽原样保留', async () => {
    const { buildHiprintElementDefinitions } = await loadAdapter()

    const [element] = buildHiprintElementDefinitions(source({
      fields: [{
        key: 'details',
        label: '明细',
        kind: 'table',
        columns: [{ field: 'code', title: '编码' }, { field: 'qty', title: '数量', width: 60 }],
      }],
    }))

    expect(element?.columns).toStrictEqual([[
      { title: '编码', field: 'code', width: 100 },
      { title: '数量', field: 'qty', width: 60 },
    ]])
  })

  it('明细表列被包成外层多一层的二维数组，符合 hiprint 的多行表头结构', async () => {
    const { buildHiprintElementDefinitions } = await loadAdapter()

    const [element] = buildHiprintElementDefinitions(source({
      fields: [{ key: 'details', label: '明细', kind: 'table', columns: [{ field: 'code', title: '编码' }] }],
    }))

    const columns = element?.columns as unknown[][]
    expect(Array.isArray(columns)).toBe(true)
    expect(columns).toHaveLength(1)
    expect(columns[0]).toHaveLength(1)
  })

  it('明细表带齐四个可编辑开关，设计器才能改列宽与列标题', async () => {
    const { buildHiprintElementDefinitions } = await loadAdapter()

    const [element] = buildHiprintElementDefinitions(source({
      fields: [{ key: 'details', label: '明细', kind: 'table', columns: [{ field: 'code', title: '编码' }] }],
    }))

    expect(element).toMatchObject({
      editable: true,
      columnDisplayEditable: true,
      columnTitleEditable: true,
      columnResizable: true,
    })
  })

  it('列定义只取标题、字段与列宽三项，注册期附加的占位/控件类型不外泄给引擎', async () => {
    const { buildHiprintElementDefinitions } = await loadAdapter()

    const [element] = buildHiprintElementDefinitions(source({
      fields: [{
        key: 'details',
        label: '明细',
        kind: 'table',
        columns: [{ field: 'code', title: '编码', inputType: 'text', placeholder: '请填写' }],
      }],
    }))

    expect(Object.keys((element?.columns as Record<string, unknown>[][])[0]![0]!).sort())
      .toStrictEqual(['field', 'title', 'width'])
  })

  it('冻结的列定义也能安全映射（注册表返回的正是冻结数组）', async () => {
    const { buildHiprintElementDefinitions } = await loadAdapter()
    const columns = Object.freeze([Object.freeze({ field: 'code', title: '编码' })])

    const [element] = buildHiprintElementDefinitions(source({
      fields: Object.freeze([Object.freeze({ key: 'details', label: '明细', kind: 'table' as const, columns })]),
    }))

    expect(element?.columns).toStrictEqual([[{ title: '编码', field: 'code', width: 100 }]])
  })

  it('tid 随数据源编码变化，不同数据源的同名字段互不覆盖', async () => {
    const { buildHiprintElementDefinitions } = await loadAdapter()

    const first = buildHiprintElementDefinitions(source({ code: 'sales' }))
    const second = buildHiprintElementDefinitions(source({ code: 'purchase' }))

    expect(first[0]?.tid).toBe('xihan.sales.title')
    expect(second[0]?.tid).toBe('xihan.purchase.title')
  })

  it('字段列表为空时得到空数组，不产出占位素材', async () => {
    const { buildHiprintElementDefinitions } = await loadAdapter()

    expect(buildHiprintElementDefinitions(source({ fields: [] }))).toStrictEqual([])
  })
})
