/**
 * 公共打印包无设备单元测试。
 * 职责：通过替身运行时验证数据源约束、字段树、模板格式、浏览器打印、打印机优先级、FIFO、客户端失败与取消，以及数据源目录加载。
 */
import type { PrintDocument, TextContent } from '@xihan-print/core'
import type { PrintDevice, RemotePrintDataSourceDto, ResolvedPrintTemplate } from './types'
import assert from 'node:assert/strict'
import { createDocument, createElementRegistry, createPage } from '@xihan-print/core'
import { afterEach, beforeEach, it, vi } from 'vitest'
// 预热依赖图：打印包经 printer-preference 引 ~/stores 桶文件，会牵出 pinia、~/locales、~/utils 等整张图，
// 首次转换在满并行下超过 10 秒的钩子超时。静态导入让这笔开销落在文件收集阶段，不计入任何钩子或用例；
// beforeEach 里的 vi.resetModules() 只清模块实例、保留转换结果，重新导入只剩求值开销。
// 这份静态实例只用于预热、从不被使用：printing / pinia / stores 仍一律取自 resetModules 之后的同一次导入。
import './index'

/**
 * 被测模块的当前实例。
 *
 * 打印包有多处模块级可变单例、且不对外暴露重置入口：
 * remote-data-sources 的 loadPromise（目录拉取缓存）、print-service 的 directQueueTail（直打 FIFO 队列尾）、
 * configuration 的运行配置。每个用例前 resetModules + 动态导入，让每个用例拿到全新的模块实例，乱序执行也成立。
 */
let printing: typeof import('./index')
let piniaModule: typeof import('pinia')
let storesModule: typeof import('~/stores')
let runtime: FakeRuntime

const TemplateJson = '{"schemaVersion":1,"pages":[{"id":"page-1"}]}'

beforeEach(async () => {
  vi.resetModules()
  runtime = createFakeRuntime()
  vi.doMock('./runtime', () => ({ loadPrintRuntime: async () => runtime.api }))
  ;[printing, piniaModule, storesModule] = await Promise.all([
    import('./index'),
    import('pinia'),
    import('~/stores'),
  ])
  // 每个用例都装一个空 pinia：打印机偏好按用户/租户分域，直打路径会读用户 store。
  piniaModule.setActivePinia(piniaModule.createPinia())
})

afterEach(() => {
  vi.doUnmock('./runtime')
  restoreBrowserPreferenceContext()
})

it('重复数据源编码立即报错', () => {
  const code = uniqueCode('duplicate')
  registerSource(code)
  assert.throws(() => registerSource(code), /已注册/u)
})

it('数据源字段换成设计器字段树：点号拆成对象层级，明细表是 list，控件类型给出数据类型，图片、条码、二维码带上 content', () => {
  const fields = printing.toPrintDataFields([
    { key: 'title', label: '标题' },
    { key: 'customer.name', label: '客户名称' },
    { key: 'customer.level', label: '客户等级', inputType: 'number' },
    { key: 'logo', label: '标志', kind: 'image' },
    { key: 'documentNo', label: '单号条码', kind: 'barcode' },
    { key: 'link', label: '查询码', kind: 'qrcode' },
    { key: 'createdTime', label: '创建时间', inputType: 'datetime' },
    { key: 'items', label: '明细', kind: 'table', columns: [{ field: 'sku', title: '编码' }, { field: 'spec.color', title: '颜色' }, { field: 'qty', title: '数量', inputType: 'number' }] },
  ])
  assert.deepEqual(fields, [
    { name: 'title', label: '标题', kind: 'value' },
    {
      name: 'customer',
      kind: 'object',
      children: [
        { name: 'name', label: '客户名称', kind: 'value' },
        { name: 'level', label: '客户等级', kind: 'value', type: 'number' },
      ],
    },
    { name: 'logo', label: '标志', kind: 'value', content: 'image' },
    { name: 'documentNo', label: '单号条码', kind: 'value', content: 'barcode' },
    { name: 'link', label: '查询码', kind: 'value', content: 'qrcode' },
    { name: 'createdTime', label: '创建时间', kind: 'value', type: 'datetime' },
    {
      name: 'items',
      label: '明细',
      kind: 'list',
      children: [
        { name: 'sku', label: '编码', kind: 'value' },
        { name: 'spec', kind: 'object', children: [{ name: 'color', label: '颜色', kind: 'value' }] },
        { name: 'qty', label: '数量', kind: 'value', type: 'number' },
      ],
    },
  ])
})

it('模板 JSON 只接受当前格式：旧格式、无效 JSON、版本不对或没有页面模板时明确报格式错误', () => {
  assert.equal(printing.parsePrintTemplateJson(TemplateJson).schemaVersion, 1)
  const rejected = [
    '{"panels":[{"printElements":[]}]}',
    '{',
    '[]',
    '{"schemaVersion":2,"pages":[{}]}',
    '{"schemaVersion":"1","pages":[{}]}',
    '{"schemaVersion":1,"pages":[]}',
    '{"schemaVersion":1}',
  ]
  for (const json of rejected) {
    assert.throws(
      () => printing.parsePrintTemplateJson(json, 'ORDER'),
      (error: unknown) => error instanceof printing.PrintTemplateFormatError && error.templateCode === 'ORDER' && error.message.includes('ORDER'),
      json,
    )
  }
})

it('模拟数据表单只列模板引用的字段：登记过的用数据源的名称与控件，其余按引用推断并提示；未注册的数据源报错', () => {
  const code = uniqueCode('sample-schema')
  registerSampleSchemaSource(code)
  const document = documentWithTexts(
    { kind: 'field', field: 'documentNo' },
    { kind: 'template', template: '{{items[0].sku}} × {{items[0].qty}}' },
    { kind: 'field', field: 'customer.name' },
  )

  const schema = printing.extractPrintSampleFormSchema(document, code)
  assert.deepEqual(schema.fields.map(field => [field.key, field.label, field.kind, field.registered]), [
    ['documentNo', '单据编号', 'text', true],
    ['items', '明细', 'table', true],
    ['customer.name', 'customer.name', 'text', false],
  ])
  assert.equal(schema.warnings.length, 1)

  const free = printing.extractPrintSampleFormSchema(document, null)
  assert.deepEqual(free.fields.map(field => [field.key, field.kind, field.registered, field.columns?.map(column => column.field)]), [
    ['documentNo', 'text', false, undefined],
    ['items', 'table', false, ['sku', 'qty']],
    ['customer.name', 'text', false, undefined],
  ])
  assert.deepEqual(free.warnings, [])

  assert.throws(() => printing.extractPrintSampleFormSchema(document, uniqueCode('missing')), /未注册/u)
})

it('没登记的字段按引用它的元素选输入方式：图片、条码、二维码各归各的，只在模板串里出现的按文本', () => {
  const registry = createElementRegistry()
  const page = createPage({ id: 'page-1' })
  page.elements = [
    registry.createElement('image', { id: 'image-1', x: 0, y: 0, overrides: { source: { kind: 'field', field: 'photo' } } }),
    registry.createElement('barCode', { id: 'barcode-1', x: 0, y: 20, overrides: { content: { kind: 'field', field: 'sku' } } }),
    registry.createElement('matrixCode', { id: 'qrcode-1', x: 0, y: 40, overrides: { content: { kind: 'field', field: 'link' } } }),
    registry.createElement('text', { id: 'text-1', x: 0, y: 60, overrides: { content: { kind: 'template', template: '备注 {{remark}}' } } }),
  ]
  const schema = printing.extractPrintSampleFormSchema(createDocument({ pages: [page] }), null)
  assert.deepEqual(schema.fields.map(field => [field.key, field.kind]), [
    ['photo', 'image'],
    ['sku', 'barcode'],
    ['link', 'qrcode'],
    ['remark', 'text'],
  ])
})

it('列出模板引用了、数据源却没登记的字段：明细表的列写成「表.列」，没登记的对象整个算一个', () => {
  const code = uniqueCode('unregistered')
  registerSampleSchemaSource(code)
  const document = documentWithTexts(
    { kind: 'field', field: 'documentNo' },
    { kind: 'template', template: '{{items[0].sku}} × {{items[0].qty}}' },
    { kind: 'expression', expression: 'customer.name + customer.phone' },
  )
  const source = printing.requirePrintDataSource(code)
  assert.deepEqual(printing.listUnregisteredPrintFields(document, source.fields), ['items.qty', 'customer'])
  assert.deepEqual(printing.listUnregisteredPrintFields(documentWithTexts({ kind: 'field', field: 'documentNo' }), source.fields), [])
})

it('默认模拟数据：有数据源时用它的样例，自由模板是一个空白对象', async () => {
  const code = uniqueCode('default-sample')
  registerSource(code)
  assert.deepEqual(await printing.createDefaultPrintSampleData(code), { documentNo: 'SAMPLE-1' })
  assert.deepEqual(await printing.createDefaultPrintSampleData(null), {})
})

it('模拟数据安全处理嵌套路径、控件类型、对象数组与内部标识隔离', () => {
  const nested: Record<string, unknown> = {}
  printing.setPrintSampleValue(nested, 'customer.address.city', '杭州')
  assert.equal(printing.getPrintSampleValue(nested, 'customer.address.city'), '杭州')
  assert.throws(() => printing.setPrintSampleValue(nested, '__proto__.polluted', true), /不安全片段/u)
  assert.equal(({} as { polluted?: unknown }).polluted, undefined)
  assert.equal(printing.inferPrintSampleInputType(undefined, 3), 'number')
  assert.equal(printing.inferPrintSampleInputType(undefined, false), 'boolean')
  assert.equal(printing.inferPrintSampleInputType('date', '2026-07-30'), 'date')

  const blank = printing.createBlankPrintSampleRecord([
    { key: 'customer.name', kind: 'text', label: '客户', registered: true },
    { key: 'quantity', kind: 'text', label: '数量', inputType: 'number', registered: true },
    { key: 'enabled', kind: 'text', label: '启用', inputType: 'boolean', registered: true },
    { key: 'details', kind: 'table', label: '明细', registered: true },
  ])
  assert.deepEqual(blank, {
    customer: { name: '' },
    details: [],
    enabled: false,
    quantity: null,
  })

  const source = [{ enabled: true, quantity: 2, details: [{ quantity: 1 }] }]
  const normalized = printing.normalizePrintSampleData(source)
  assert.equal(normalized.isCollection, true)
  assert.notEqual(normalized.records[0], source[0])
  assert.notEqual(normalized.records[0]?.details, source[0]?.details)
  assert.deepEqual(printing.normalizePrintSampleData([]), { isCollection: true, records: [{}] })
  assert.throws(() => printing.normalizePrintSampleData('invalid'), /对象或对象数组/u)
  assert.throws(() => printing.normalizePrintSampleData([{}, 'invalid']), /每一项都必须是对象/u)

  const payload = printing.createPrintSamplePayload([normalized.records[0]!, { enabled: false, quantity: 3 }], true)
  assert.deepEqual(payload, [
    { enabled: true, quantity: 2, details: [{ quantity: 1 }] },
    { enabled: false, quantity: 3 },
  ])
})

it('浏览器打印把单对象包成一条记录、数组原样交给运行时，标题缺省为模板名称', async () => {
  const code = uniqueCode('browser')
  registerSource(code)
  configureFor(code)

  await printing.previewPrintByCode(code, { documentNo: 'V-1' })
  await printing.previewPrintByCode(code, [{ documentNo: 'V-2' }, { documentNo: 'V-3' }], { title: '对账单' })

  assert.deepEqual(runtime.browserPrints, [
    { records: [{ documentNo: 'V-1' }], title: code },
    { records: [{ documentNo: 'V-2' }, { documentNo: 'V-3' }], title: '对账单' },
  ])
  await assert.rejects(printing.previewPrintByCode(code, []), /不能为空数组/u)
})

it('自由模板无需注册数据源即可浏览器打印', async () => {
  const code = uniqueCode('free')
  configureFor(code, '1', null)
  await printing.previewPrintByCode(code, { anything: 1 })
  assert.equal(runtime.browserPrints.length, 1)
})

it('浏览器打印在模板版本一致时继续，冲突时拒绝且不交给运行时', async () => {
  const code = uniqueCode('version')
  registerSource(code)
  configureFor(code, '42')

  await printing.previewPrintByCode(code, { documentNo: 'V-1' }, { expectedRowVersion: ' 42 ' })
  await assert.rejects(
    printing.previewPrintByCode(code, { documentNo: 'V-2' }, { expectedRowVersion: '41' }),
    (error: unknown) => error instanceof printing.PrintTemplateVersionChangedError,
  )
  assert.equal(runtime.browserPrints.length, 1)
})

it('后端存的是旧格式模板时打印明确报格式错误，不交给运行时', async () => {
  const code = uniqueCode('legacy')
  registerSource(code)
  configureFor(code, '1', code, '{"panels":[{"printElements":[]}]}')

  await assert.rejects(printing.previewPrintByCode(code, {}), (error: unknown) => error instanceof printing.PrintTemplateFormatError)
  await assert.rejects(printing.directPrintByCode(code, {}), (error: unknown) => error instanceof printing.PrintTemplateFormatError)
  assert.equal(runtime.browserPrints.length, 0)
  assert.equal(runtime.clientPrints.length, 0)
})

it('直打按显式、本地偏好、客户端默认的顺序选择打印机', async () => {
  installBrowserPreferenceContext()
  const code = uniqueCode('priority')
  registerSource(code)
  configureFor(code)
  runtime.printers = [
    { name: 'Default Printer', isDefault: true },
    { name: 'Preferred Printer', isDefault: false },
    { name: 'Explicit Printer', isDefault: false },
  ]

  const explicit = await printing.directPrintByCode(code, { documentNo: 'P-1' }, { printerName: 'Explicit Printer', copies: 2 })
  await printing.setPreferredPrinter(code, 'Preferred Printer')
  const preferred = await printing.directPrintByCode(code, { documentNo: 'P-2' })
  await printing.setPreferredPrinter(code, null)
  const clientDefault = await printing.directPrintByCode(code, { documentNo: 'P-3' })

  assert.equal(explicit.printerName, 'Explicit Printer')
  assert.equal(explicit.copies, 2)
  assert.equal(preferred.printerName, 'Preferred Printer')
  assert.equal(clientDefault.printerName, 'Default Printer')
  assert.deepEqual(runtime.clientPrints.map(print => [print.options.printer, print.options.copies]), [
    ['Explicit Printer', 2],
    ['Preferred Printer', 1],
    ['Default Printer', 1],
  ])
  await assert.rejects(printing.directPrintByCode(code, {}, { printerName: 'Missing' }), /不存在或已离线/u)
  await assert.rejects(printing.directPrintByCode(code, {}, { copies: 100 }), /1～99/u)
})

it('当前会话直打队列严格 FIFO：前一个任务交给打印队列之前，后一个不送出', async () => {
  const code = uniqueCode('fifo')
  registerSource(code)
  configureFor(code)
  runtime.manualJobs = true

  const first = printing.directPrintByCode(code, { documentNo: 'F-1' })
  const second = printing.directPrintByCode(code, { documentNo: 'F-2' })
  await vi.waitFor(() => assert.equal(runtime.clientPrints.length, 1))
  await new Promise(resolve => setTimeout(resolve, 5))
  assert.equal(runtime.clientPrints.length, 1)

  runtime.clientPrints[0]!.complete()
  await first
  await vi.waitFor(() => assert.equal(runtime.clientPrints.length, 2))
  runtime.clientPrints[1]!.complete()
  await second
  assert.deepEqual(runtime.clientPrints.map(print => print.records), [[{ documentNo: 'F-1' }], [{ documentNo: 'F-2' }]])
})

it('连不上客户端时按错误码明确报错，不退回浏览器打印', async () => {
  const code = uniqueCode('unavailable')
  registerSource(code)
  configureFor(code)
  runtime.printersError = clientError('CLIENT_UNAVAILABLE')

  await assert.rejects(printing.directPrintByCode(code, { documentNo: 'U-1' }), /没有连上本地打印客户端/u)
  await assert.rejects(printing.listPrinters(), /没有连上本地打印客户端/u)
  assert.equal(runtime.browserPrints.length, 0)
  assert.equal(runtime.clientPrints.length, 0)
})

it('客户端报告任务失败时按错误码拒绝；客户端没有打印机时明确报错', async () => {
  const code = uniqueCode('failure')
  registerSource(code)
  configureFor(code)
  runtime.jobError = clientError('PRINTER_OFFLINE')
  await assert.rejects(printing.directPrintByCode(code, { documentNo: 'E-1' }), /脱机/u)

  runtime.jobError = clientError('SOMETHING_NEW')
  await assert.rejects(printing.directPrintByCode(code, { documentNo: 'E-2' }), /SOMETHING_NEW/u)

  runtime.printers = []
  await assert.rejects(printing.directPrintByCode(code, { documentNo: 'E-3' }), /没有找到任何打印机/u)
})

it('组件卸载取消直打后取消客户端任务并以取消原因拒绝', async () => {
  const code = uniqueCode('abort')
  registerSource(code)
  configureFor(code)
  runtime.manualJobs = true
  const controller = new AbortController()

  const pending = printing.directPrintByCode(code, { documentNo: 'A-1' }, { signal: controller.signal })
  await vi.waitFor(() => assert.equal(runtime.clientPrints.length, 1))
  controller.abort(new DOMException('打印模板编辑器已卸载。', 'AbortError'))

  await assert.rejects(pending, (error: unknown) => (error as Error).name === 'AbortError')
  assert.equal(runtime.clientPrints[0]!.cancelled, true)
})

it('未注入 listDataSources 时目录直接完成且不缓存空结果', async () => {
  configureCatalog(undefined)
  await printing.ensureRemotePrintDataSourcesLoaded()

  // 注入取数函数后必须真正拉取；若空结果被缓存，这里将拿不到注入函数的拒绝
  configureCatalog(async () => {
    throw new Error('catalog-after-configure')
  })
  await assert.rejects(printing.ensureRemotePrintDataSourcesLoaded(), /catalog-after-configure/u)
})

it('目录拉取失败不缓存，下一次调用重新拉取', async () => {
  let calls = 0
  configureCatalog(async () => {
    calls++
    throw new Error('catalog-unavailable')
  })

  await assert.rejects(printing.ensureRemotePrintDataSourcesLoaded(), /catalog-unavailable/u)
  await assert.rejects(printing.ensureRemotePrintDataSourcesLoaded(), /catalog-unavailable/u)
  assert.equal(calls, 2)
})

it('目录拉取注册后端数据源：跳过已注册编码、隔离坏样例、并发共享一次拉取', async () => {
  const localCode = uniqueCode('remote-local')
  registerSource(localCode)
  const remoteCode = uniqueCode('remote-good')
  const brokenCode = uniqueCode('remote-broken')
  let calls = 0
  configureCatalog(async () => {
    calls++
    return [
      { code: localCode, name: '后端同名数据源', fields: [{ key: 'other', label: '其它', kind: 'text' }], sampleDataJson: '{}' },
      {
        code: remoteCode,
        name: '后端数据源',
        fields: [
          { key: 'title', label: '标题', kind: 'text', inputType: 'textarea', placeholder: '请输入' },
          {
            key: 'items',
            label: '明细',
            kind: 'table',
            columns: [{ field: 'sku', title: '编码', width: null, inputType: 'number', placeholder: '数量' }],
          },
        ],
        sampleDataJson: '{"title":"示例","items":[{"sku":1}]}',
      },
      { code: brokenCode, name: '坏样例', fields: [{ key: 'f', label: '字段', kind: 'text' }], sampleDataJson: '{broken' },
    ]
  })

  const originalConsoleError = console.error
  let isolationLogCount = 0
  console.error = () => {
    isolationLogCount++
  }
  try {
    await Promise.all([printing.ensureRemotePrintDataSourcesLoaded(), printing.ensureRemotePrintDataSourcesLoaded()])
  }
  finally {
    console.error = originalConsoleError
  }

  assert.equal(calls, 1)
  // 本地已注册的编码保持原定义
  assert.equal(printing.getPrintDataSource(localCode)?.name, localCode)
  // 坏样例只跳过自身，不拖垮其余数据源
  assert.equal(printing.getPrintDataSource(brokenCode), undefined)
  assert.equal(isolationLogCount, 1)

  const remote = printing.getPrintDataSource(remoteCode)
  assert.equal(remote?.name, '后端数据源')
  assert.deepEqual(remote?.fields.map(field => field.key), ['title', 'items'])
  assert.equal(remote?.fields[0]?.inputType, 'textarea')
  assert.equal(remote?.fields[1]?.columns?.[0]?.width, undefined)
  assert.equal(remote?.fields[1]?.columns?.[0]?.placeholder, '数量')

  // 样例工厂每次返回独立副本
  const first = remote!.createSampleData() as Record<string, unknown>
  const second = remote!.createSampleData() as Record<string, unknown>
  first.title = '已修改'
  assert.equal(second.title, '示例')
})

/** 替身运行时：记下浏览器打印与客户端任务，打印机列表、错误与任务完成时机由用例控制。 */
interface FakeRuntime {
  api: {
    printInBrowser: (document: unknown, records: readonly unknown[], options: { title?: string }) => Promise<number>
    printWithClient: (document: unknown, records: readonly unknown[], options: { printer?: string, copies?: number }) => unknown
    client: () => { printers: () => Promise<PrintDevice[]> }
  }
  browserPrints: { records: unknown[], title?: string }[]
  clientPrints: { records: unknown[], options: { printer?: string, copies?: number }, cancelled: boolean, complete: () => void }[]
  printers: PrintDevice[]
  printersError: Error | null
  jobError: Error | null
  manualJobs: boolean
}

function createFakeRuntime(): FakeRuntime {
  const fake: FakeRuntime = {
    browserPrints: [],
    clientPrints: [],
    printers: [{ name: 'Default Printer', isDefault: true }],
    printersError: null,
    jobError: null,
    manualJobs: false,
    api: {
      printInBrowser: async (_document, records, options) => {
        fake.browserPrints.push({ records: [...records], title: options.title })
        return 1
      },
      printWithClient: (_document, records, options) => {
        let resolve!: () => void
        let reject!: (error: unknown) => void
        const done = new Promise<void>((ok, fail) => {
          resolve = ok
          reject = fail
        })
        const entry = { records: [...records], options, cancelled: false, complete: () => resolve() }
        fake.clientPrints.push(entry)
        if (fake.jobError)
          reject(fake.jobError)
        else if (!fake.manualJobs)
          resolve()
        return {
          done,
          cancel: async () => {
            entry.cancelled = true
            reject(clientError('JOB_CANCELLED'))
            return true
          },
        }
      },
      client: () => ({
        printers: async () => {
          if (fake.printersError)
            throw fake.printersError
          return fake.printers
        },
      }),
    },
  }
  return fake
}

/** 与客户端 SDK 同名的错误：打印服务按 name 与 code 认它。 */
function clientError(code: string): Error {
  const error = new Error(code) as Error & { code: string }
  error.name = 'PrintClientError'
  error.code = code
  return error
}

/** 注册单字段测试数据源。 */
function registerSource(code: string): void {
  printing.registerPrintDataSource({
    code,
    name: code,
    fields: [{ key: 'documentNo', label: '单据编号', kind: 'text' }],
    createSampleData: () => ({ documentNo: 'SAMPLE-1' }),
  })
}

/** 注入返回指定模板与数据源的稳定解析函数。 */
function configureFor(code: string, rowVersion = '1', dataSourceCode: null | string = code, templateJson = TemplateJson): void {
  printing.configurePrinting({
    resolveTemplate: async (_templateCode, scope): Promise<ResolvedPrintTemplate> => ({
      basicId: '1',
      dataSourceCode,
      requestedScope: scope,
      resolvedScope: scope === 'Auto' ? 'Tenant' : scope,
      rowVersion,
      templateCode: code,
      templateJson,
      templateName: code,
    }),
  })
}

/** 注入指定目录取数函数（可为空）的运行配置。 */
function configureCatalog(listDataSources: (() => Promise<RemotePrintDataSourceDto[]>) | undefined): void {
  printing.configurePrinting({
    resolveTemplate: async () => {
      throw new Error('目录测试不解析模板。')
    },
    listDataSources,
  })
}

/** 安装本用例前的原始 localStorage 描述符，用于用例结束后原样还原。 */
let originalLocalStorageDescriptor: PropertyDescriptor | undefined

/**
 * 安装隔离的 Pinia 与内存 localStorage，支持本地打印机偏好测试。
 * 替身实现完整的 Storage 接口；覆盖前记下原描述符，由 afterEach 还原，避免污染同文件的后续用例。
 */
function installBrowserPreferenceContext(): void {
  piniaModule.setActivePinia(piniaModule.createPinia())
  const userStore = storesModule.useUserStore()
  userStore.userInfo = { basicId: 'tester', tenantId: 'tenant-7' } as typeof userStore.userInfo
  const values = new Map<string, string>()

  originalLocalStorageDescriptor ??= Object.getOwnPropertyDescriptor(globalThis, 'localStorage')
  Object.defineProperty(globalThis, 'localStorage', {
    configurable: true,
    value: {
      get length() {
        return values.size
      },
      clear: () => values.clear(),
      getItem: (key: string) => values.get(key) ?? null,
      key: (index: number) => [...values.keys()][index] ?? null,
      removeItem: (key: string) => values.delete(key),
      setItem: (key: string, value: string) => values.set(key, value),
    } satisfies Storage,
  })
}

/** 还原被 installBrowserPreferenceContext 覆盖的全局 localStorage。 */
function restoreBrowserPreferenceContext(): void {
  if (!originalLocalStorageDescriptor)
    return

  Object.defineProperty(globalThis, 'localStorage', originalLocalStorageDescriptor)
  originalLocalStorageDescriptor = undefined
}

/** 登记一个带单号、明细与备注的数据源，供模拟数据表单与字段比对用例共用。 */
function registerSampleSchemaSource(code: string): void {
  printing.registerPrintDataSource({
    code,
    name: code,
    fields: [
      { key: 'documentNo', label: '单据编号' },
      { key: 'items', label: '明细', kind: 'table', columns: [{ field: 'sku', title: '编码' }] },
      { key: 'remark', label: '备注' },
    ],
    createSampleData: () => ({ documentNo: 'S-1', items: [] }),
  })
}

/** 一页模板，每个文本元素按给定的内容取值。 */
function documentWithTexts(...contents: TextContent[]): PrintDocument {
  const registry = createElementRegistry()
  const page = createPage({ id: 'page-1' })
  page.elements = contents.map((content, index) => registry.createElement('text', { id: `text-${index}`, x: 0, y: index * 10, overrides: { content } }))
  return createDocument({ pages: [page] })
}

/** 生成不会与其它测试或应用启动数据源冲突的编码。 */
function uniqueCode(prefix: string): string {
  return `test.${prefix}.${crypto.randomUUID()}`
}
