/**
 * 公共打印包无设备单元测试。
 * 职责：通过可替换适配器验证数据源约束、预览数据、打印机优先级、FIFO、失败、超时和监听器清理。
 */
import type {
  HiprintTemplateInstance,
  PrintDevice,
  PrintElementAlignAction,
  PrintElementSpacingDirection,
  PrintingAdapter,
  RemotePrintDataSourceDto,
  ResolvedPrintTemplate,
} from './types'
import assert from 'node:assert/strict'
import { createPinia, setActivePinia } from 'pinia'
import { afterEach, it } from 'vitest'
import { useUserStore } from '~/stores'
import { buildHiprintElementDefinitions } from './hiprint-adapter'
import {
  configurePrinting,
  createBlankPrintSampleRecord,
  createDefaultPrintSampleData,
  createPrintDesigner,
  createPrintSamplePayload,
  directPrintByCode,
  ensureRemotePrintDataSourcesLoaded,
  extractPrintSampleFormSchema,
  getPrintDataSource,
  getPrintSampleValue,
  inferPrintSampleInputType,
  normalizePrintSampleData,
  previewPrintByCode,
  PrintTemplateVersionChangedError,
  registerPrintDataSource,
  setPreferredPrinter,
  setPrintingAdapter,
  setPrintSampleValue,
} from './index'

const TemplateJson = '{"panels":[{"printElements":[]}]}'

afterEach(() => {
  setPrintingAdapter(null)
})

it('重复数据源编码立即报错', () => {
  const code = uniqueCode('duplicate')
  registerSource(code)

  assert.throws(() => registerSource(code), /已注册/u)
})

it('字段目录按 hiprint 0.0.60 协议映射全部支持类型', () => {
  const definitions = buildHiprintElementDefinitions({
    code: uniqueCode('field-kinds'),
    name: '字段类型',
    fields: [
      { key: 'title', label: '文本', kind: 'text' },
      { key: 'logo', label: '图片', kind: 'image' },
      { key: 'barcode', label: '条码', kind: 'barcode' },
      { key: 'qrcode', label: '二维码', kind: 'qrcode' },
      {
        key: 'details',
        label: '明细',
        kind: 'table',
        columns: [{ field: 'materialCode', title: '物料编码', width: 120 }],
      },
    ],
    createSampleData: () => ({}),
  })

  assert.deepEqual(
    definitions.map(item => ({ type: item.type, textType: item.textType })),
    [
      { type: 'text', textType: undefined },
      { type: 'image', textType: undefined },
      { type: 'text', textType: 'barcode' },
      { type: 'text', textType: 'qrcode' },
      { type: 'table', textType: undefined },
    ],
  )
  assert.deepEqual(definitions[4]?.columns, [[{
    field: 'materialCode',
    title: '物料编码',
    width: 120,
  }]])
})

it('多面板模板按视觉顺序提取三种绑定、去重并忽略静态元素', () => {
  const code = uniqueCode('sample-schema')
  registerPrintDataSource({
    code,
    name: '模拟字段数据源',
    fields: [
      { key: 'title', label: '标题', kind: 'text', inputType: 'textarea', placeholder: '请输入标题' },
      { key: 'logo', label: 'Logo', kind: 'image' },
      { key: 'barcode', label: '条码', kind: 'barcode' },
      { key: 'qrcode', label: '二维码', kind: 'qrcode' },
      {
        key: 'details',
        label: '明细',
        kind: 'table',
        columns: [
          { field: 'materialCode', title: '物料编码' },
          { field: 'quantity', title: '数量', inputType: 'number' },
        ],
      },
      { key: 'enabled', label: '启用', inputType: 'boolean' },
    ],
    createSampleData: () => ({}),
  })
  const template = {
    panels: [
      {
        printElements: [
          { options: { left: 0, text: '静态标题', top: 0 }, printElementType: { type: 'text' } },
          { options: { field: 'title', left: 20, top: 40 } },
          { options: { left: 30, top: 10 }, printElementType: { field: 'logo' } },
          { options: { field: 'barcode', left: 80, top: 20 } },
          { options: { left: 10, top: 20 }, tid: `xihan.${code}.qrcode` },
          { options: { field: 'manual.reference', left: 10, top: 30 } },
          { options: { field: 'details', left: 10, top: 50 } },
          { options: { field: 'title', left: 10, top: 60 } },
        ],
      },
      { printElements: [{ options: { field: 'enabled', left: 0, top: 0 } }] },
    ],
  }

  const schema = extractPrintSampleFormSchema(template, code)

  assert.deepEqual(schema.fields.map(field => field.key), [
    'logo',
    'qrcode',
    'barcode',
    'manual.reference',
    'title',
    'details',
    'enabled',
  ])
  assert.deepEqual(schema.fields.map(field => field.kind), [
    'image',
    'qrcode',
    'barcode',
    'text',
    'text',
    'table',
    'text',
  ])
  assert.equal(schema.fields.find(field => field.key === 'title')?.inputType, 'textarea')
  assert.equal(schema.fields.find(field => field.key === 'title')?.placeholder, '请输入标题')
  assert.equal(schema.fields.find(field => field.key === 'details')?.columns?.[1]?.inputType, 'number')
  assert.equal(schema.fields.find(field => field.key === 'manual.reference')?.registered, false)
  assert.equal(schema.warnings.length, 1)
})

it('自由模板按元素元数据推断图片、条码、表格和空白默认数据', async () => {
  const template = {
    panels: [{
      printElements: [
        {
          options: { field: 'customer.name', left: 10, top: 10 },
          printElementType: { title: '客户名称', type: 'text' },
        },
        {
          options: { field: 'logo', left: 10, top: 20 },
          printElementType: { title: '企业 Logo', type: 'image' },
        },
        {
          options: { field: 'documentNo', left: 10, top: 30 },
          printElementType: { textType: 'barcode', title: '单据条码', type: 'text' },
        },
        {
          options: { field: 'items', left: 10, top: 40 },
          printElementType: {
            columns: [[
              { field: 'sku', title: '物料编码' },
              { field: 'quantity', inputType: 'number', title: '数量' },
            ]],
            title: '明细',
            type: 'table',
          },
        },
      ],
    }],
  }

  const schema = extractPrintSampleFormSchema(template, null)
  const sample = await createDefaultPrintSampleData(template, null)

  assert.deepEqual(schema.fields.map(field => ({
    key: field.key,
    kind: field.kind,
    label: field.label,
  })), [
    { key: 'customer.name', kind: 'text', label: '客户名称' },
    { key: 'logo', kind: 'image', label: '企业 Logo' },
    { key: 'documentNo', kind: 'barcode', label: '单据条码' },
    { key: 'items', kind: 'table', label: '明细' },
  ])
  assert.equal(schema.fields[3]?.columns?.[1]?.inputType, 'number')
  assert.equal(schema.warnings.length, 0)
  assert.deepEqual(sample, {
    customer: { name: '' },
    documentNo: '',
    items: [],
    logo: '',
  })
})

it('模拟数据安全处理嵌套路径、控件类型、对象数组与内部标识隔离', () => {
  const nested: Record<string, unknown> = {}
  setPrintSampleValue(nested, 'customer.address.city', '杭州')
  assert.equal(getPrintSampleValue(nested, 'customer.address.city'), '杭州')
  assert.throws(() => setPrintSampleValue(nested, '__proto__.polluted', true), /不安全片段/u)
  assert.equal(({} as { polluted?: unknown }).polluted, undefined)
  assert.equal(inferPrintSampleInputType(undefined, 3), 'number')
  assert.equal(inferPrintSampleInputType(undefined, false), 'boolean')
  assert.equal(inferPrintSampleInputType('date', '2026-07-30'), 'date')

  const blank = createBlankPrintSampleRecord([
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
  const normalized = normalizePrintSampleData(source)
  assert.equal(normalized.isCollection, true)
  assert.notEqual(normalized.records[0], source[0])
  assert.notEqual(normalized.records[0]?.details, source[0]?.details)
  assert.deepEqual(normalizePrintSampleData([]), { isCollection: true, records: [{}] })
  assert.throws(() => normalizePrintSampleData('invalid'), /对象或对象数组/u)
  assert.throws(() => normalizePrintSampleData([{}, 'invalid']), /每一项都必须是对象/u)

  const editableRecords = [
    { id: crypto.randomUUID(), data: normalized.records[0]! },
    { id: crypto.randomUUID(), data: { enabled: false, quantity: 3 } },
  ]
  const payload = createPrintSamplePayload(editableRecords.map(record => record.data), true)
  assert.deepEqual(payload, [
    { enabled: true, quantity: 2, details: [{ quantity: 1 }] },
    { enabled: false, quantity: 3 },
  ])
  assert.equal(Object.hasOwn((payload as Record<string, unknown>[])[0]!, 'id'), false)
})

it('设计器旋转和 JSON 整体更新会发布最终模板，更新失败时恢复原设计', async () => {
  const code = uniqueCode('designer-json')
  registerSource(code)
  const initialTemplate = {
    panels: [{ height: 297, index: 0, printElements: [], width: 210 }],
  }
  let currentTemplate: unknown = structuredClone(initialTemplate)
  let rotationCount = 0
  const changes: unknown[] = []
  const template: HiprintTemplateInstance = {
    id: 'designer-template',
    clientIsOpened: () => false,
    clear: () => undefined,
    design: () => undefined,
    getJson: () => structuredClone(currentTemplate),
    getPaneltotal: () => 1,
    getPrinterList: () => [],
    on: () => undefined,
    print: () => undefined,
    print2: () => undefined,
    redo: () => undefined,
    rotatePaper: () => {
      rotationCount += 1
    },
    selectPanel: () => undefined,
    setPaper: () => undefined,
    undo: () => undefined,
    update: (nextTemplate) => {
      if ((nextTemplate as { shouldFail?: boolean }).shouldFail)
        throw new Error('模拟 JSON 更新失败')
      currentTemplate = structuredClone(nextTemplate)
    },
    zoom: () => undefined,
  }
  const adapter: PrintingAdapter = {
    createTemplate: () => template,
    enableFieldDragging: () => undefined,
    isClientConnected: () => false,
    listPrinters: () => [],
    refreshPrinters: async () => [],
    removePrintListeners: () => undefined,
  }
  setPrintingAdapter(adapter)

  const designer = await createPrintDesigner({
    canvas: '#designer-test-canvas',
    dataSourceCode: code,
    onDataChanged: json => changes.push(structuredClone(json)),
    settingContainer: '#designer-test-settings',
    template: initialTemplate,
  })
  designer.rotatePaper()
  const updatedTemplate = {
    panels: [{ height: 210, index: 0, printElements: [], width: 297 }],
  }
  designer.updateTemplate(updatedTemplate)

  assert.equal(rotationCount, 1)
  assert.deepEqual(currentTemplate, updatedTemplate)
  assert.deepEqual(changes, [initialTemplate, updatedTemplate])

  assert.throws(
    () => designer.updateTemplate({ panels: [{}], shouldFail: true }),
    /模拟 JSON 更新失败/u,
  )
  assert.deepEqual(currentTemplate, updatedTemplate)
})

it('设计器元素对齐与固定间距校验选择数量并发布最终模板', async () => {
  const code = uniqueCode('designer-alignment')
  registerSource(code)
  let selectedElementCount = 1
  const alignActions: PrintElementAlignAction[] = []
  const spacingActions: Array<{ direction: PrintElementSpacingDirection, spacing: number }> = []
  const changes: unknown[] = []
  const currentTemplate = { panels: [{ printElements: [] }] }
  const template: HiprintTemplateInstance = {
    id: 'designer-alignment-template',
    clientIsOpened: () => false,
    clear: () => undefined,
    design: () => undefined,
    getJson: () => structuredClone(currentTemplate),
    getPaneltotal: () => 1,
    getPrinterList: () => [],
    getSelectEls: () => Array.from({ length: selectedElementCount }, () => ({})),
    on: () => undefined,
    print: () => undefined,
    print2: () => undefined,
    redo: () => undefined,
    selectAllElements: () => {
      selectedElementCount = 3
    },
    selectPanel: () => undefined,
    setElsAlign: action => alignActions.push(action),
    setElsSpace: (spacing, horizontal) => spacingActions.push({
      direction: horizontal ? 'horizontal' : 'vertical',
      spacing,
    }),
    setPaper: () => undefined,
    undo: () => undefined,
    zoom: () => undefined,
  }
  const adapter: PrintingAdapter = {
    createTemplate: () => template,
    enableFieldDragging: () => undefined,
    isClientConnected: () => false,
    listPrinters: () => [],
    refreshPrinters: async () => [],
    removePrintListeners: () => undefined,
  }
  setPrintingAdapter(adapter)

  const designer = await createPrintDesigner({
    canvas: '#designer-alignment-canvas',
    dataSourceCode: code,
    onDataChanged: json => changes.push(structuredClone(json)),
    settingContainer: '#designer-alignment-settings',
    template: currentTemplate,
  })

  assert.throws(() => designer.alignElements('left'), /至少需要选择 2 个元素/u)
  designer.selectAllElements()
  assert.equal(designer.getSelectedElementCount(), 3)
  selectedElementCount = 2
  designer.alignElements('left')
  designer.setElementSpacing(10, 'horizontal')
  designer.setElementSpacing(10, 'vertical')
  assert.throws(() => designer.alignElements('distributeHor'), /至少需要选择 3 个元素/u)
  assert.throws(() => designer.setElementSpacing(-1, 'horizontal'), /0～1000/u)

  selectedElementCount = 3
  designer.alignElements('distributeHor')

  assert.deepEqual(alignActions, ['left', 'distributeHor'])
  assert.deepEqual(spacingActions, [
    { direction: 'horizontal', spacing: 10 },
    { direction: 'vertical', spacing: 10 },
  ])
  assert.equal(changes.length, 4)
})

it('浏览器预览原样接收对象和数组数据', async () => {
  const code = uniqueCode('preview')
  registerSource(code)
  const fixture = createAdapterFixture()
  configureFor(code)
  setPrintingAdapter(fixture.adapter)
  const objectData = { documentNo: 'A-1' }
  const arrayData = [{ documentNo: 'A-2' }, { documentNo: 'A-3' }]

  await previewPrintByCode(code, objectData)
  await previewPrintByCode(code, arrayData)

  assert.deepEqual(fixture.previewData, [objectData, arrayData])
})

it('自由模板无需注册数据源即可使用公共预览 API', async () => {
  const code = uniqueCode('free-preview')
  const fixture = createAdapterFixture()
  configureFor(code, '1', null)
  setPrintingAdapter(fixture.adapter)

  await previewPrintByCode(code, { customField: '自由数据' })

  assert.deepEqual(fixture.previewData, [{ customField: '自由数据' }])
})

it('浏览器预览在预期模板版本一致时继续打开', async () => {
  const code = uniqueCode('preview-version-match')
  registerSource(code)
  const fixture = createAdapterFixture()
  configureFor(code, 'version-7')
  setPrintingAdapter(fixture.adapter)

  await previewPrintByCode(code, { documentNo: 'V-1' }, { expectedRowVersion: 'version-7' })

  assert.equal(fixture.createdTemplateCount, 1)
})

it('浏览器预览在模板版本冲突时阻止旧表单数据且不创建模板实例', async () => {
  const code = uniqueCode('preview-version-conflict')
  registerSource(code)
  const fixture = createAdapterFixture()
  configureFor(code, 'version-8')
  setPrintingAdapter(fixture.adapter)

  await assert.rejects(
    previewPrintByCode(code, { documentNo: 'V-2' }, { expectedRowVersion: 'version-7' }),
    error => error instanceof PrintTemplateVersionChangedError,
  )
  assert.equal(fixture.createdTemplateCount, 0)
})

it('浏览器预览未传预期版本时保持原有调用兼容性', async () => {
  const code = uniqueCode('preview-version-optional')
  registerSource(code)
  const fixture = createAdapterFixture()
  configureFor(code, 'version-9')
  setPrintingAdapter(fixture.adapter)

  await previewPrintByCode(code, { documentNo: 'V-3' })

  assert.equal(fixture.createdTemplateCount, 1)
})

it('直打按显式、本地偏好、客户端默认的顺序选择打印机', async () => {
  installBrowserPreferenceContext()
  const code = uniqueCode('priority')
  registerSource(code)
  const fixture = createAdapterFixture({
    printers: [
      { name: 'Default Printer', isDefault: true },
      { name: 'Preferred Printer' },
      { name: 'Explicit Printer' },
    ],
  })
  configureFor(code)
  setPrintingAdapter(fixture.adapter)

  const explicit = await directPrintByCode(code, { documentNo: 'P-1' }, { printerName: 'Explicit Printer' })
  await setPreferredPrinter(code, 'Preferred Printer')
  const preferred = await directPrintByCode(code, { documentNo: 'P-2' })
  await setPreferredPrinter(code, null)
  const clientDefault = await directPrintByCode(code, { documentNo: 'P-3' })

  assert.equal(explicit.printerName, 'Explicit Printer')
  assert.equal(preferred.printerName, 'Preferred Printer')
  assert.equal(clientDefault.printerName, 'Default Printer')
})

it('当前会话直打队列严格 FIFO 且逐任务清理监听器', async () => {
  const code = uniqueCode('fifo')
  registerSource(code)
  const fixture = createAdapterFixture({
    onPrint2: (template, _data, _options, sequence) => {
      setTimeout(
        () => fixture.emit(template.id, 'printSuccess', { sequence }),
        sequence === 1 ? 20 : 0,
      )
    },
  })
  configureFor(code)
  setPrintingAdapter(fixture.adapter)

  const first = directPrintByCode(code, { documentNo: 'F-1' })
  const second = directPrintByCode(code, { documentNo: 'F-2' })
  await new Promise(resolve => setTimeout(resolve, 5))
  assert.deepEqual(fixture.directStartOrder, [1])

  const results = await Promise.all([first, second])
  assert.deepEqual(fixture.directStartOrder, [1, 2])
  assert.deepEqual(results.map(result => result.payload), [{ sequence: 1 }, { sequence: 2 }])
  assert.equal(fixture.cleanupCount, 2)
})

it('客户端失败事件明确拒绝且不回退浏览器预览', async () => {
  const code = uniqueCode('failure')
  registerSource(code)
  const fixture = createAdapterFixture({
    onPrint2: template => queueMicrotask(
      () => fixture.emit(template.id, 'printError', { message: '驱动拒绝任务' }),
    ),
  })
  configureFor(code)
  setPrintingAdapter(fixture.adapter)

  await assert.rejects(
    directPrintByCode(code, { documentNo: 'E-1' }),
    /驱动拒绝任务/u,
  )
  assert.equal(fixture.previewData.length, 0)
  assert.equal(fixture.cleanupCount, 1)
})

it('直打超时后拒绝并清理任务监听器', async () => {
  const code = uniqueCode('timeout')
  registerSource(code)
  const fixture = createAdapterFixture({ onPrint2: () => undefined })
  configureFor(code)
  setPrintingAdapter(fixture.adapter)

  await assert.rejects(
    directPrintByCode(code, { documentNo: 'T-1' }, { timeoutMs: 10 }),
    /超时/u,
  )
  assert.equal(fixture.cleanupCount, 1)
})

it('组件卸载取消直打后立即拒绝并清理任务监听器', async () => {
  const code = uniqueCode('abort')
  registerSource(code)
  const fixture = createAdapterFixture({ onPrint2: () => undefined })
  const controller = new AbortController()
  configureFor(code)
  setPrintingAdapter(fixture.adapter)

  const task = directPrintByCode(
    code,
    { documentNo: 'A-1' },
    { signal: controller.signal, timeoutMs: 5_000 },
  )
  setTimeout(
    () => controller.abort(new DOMException('组件已卸载。', 'AbortError')),
    5,
  )

  await assert.rejects(task, error => (error as Error).name === 'AbortError')
  assert.equal(fixture.cleanupCount, 1)
})

it('客户端离线时直接报错且不创建打印模板实例', async () => {
  const code = uniqueCode('offline')
  registerSource(code)
  const fixture = createAdapterFixture({ connected: false })
  configureFor(code)
  setPrintingAdapter(fixture.adapter)

  await assert.rejects(
    directPrintByCode(code, { documentNo: 'O-1' }),
    /客户端未连接/u,
  )
  assert.equal(fixture.createdTemplateCount, 0)
})

it('未注入 listDataSources 时目录直接完成且不缓存空结果', async () => {
  configureCatalog(undefined)
  await ensureRemotePrintDataSourcesLoaded()

  // 注入取数函数后必须真正拉取；若空结果被缓存，这里将拿不到注入函数的拒绝
  configureCatalog(async () => {
    throw new Error('catalog-after-configure')
  })
  await assert.rejects(ensureRemotePrintDataSourcesLoaded(), /catalog-after-configure/u)
})

it('目录拉取失败不缓存，下一次调用重新拉取', async () => {
  let calls = 0
  configureCatalog(async () => {
    calls++
    throw new Error('catalog-unavailable')
  })

  await assert.rejects(ensureRemotePrintDataSourcesLoaded(), /catalog-unavailable/u)
  await assert.rejects(ensureRemotePrintDataSourcesLoaded(), /catalog-unavailable/u)
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
    await Promise.all([ensureRemotePrintDataSourcesLoaded(), ensureRemotePrintDataSourcesLoaded()])
  }
  finally {
    console.error = originalConsoleError
  }

  assert.equal(calls, 1)
  // 本地已注册的编码保持原定义
  assert.equal(getPrintDataSource(localCode)?.name, localCode)
  // 坏样例只跳过自身，不拖垮其余数据源
  assert.equal(getPrintDataSource(brokenCode), undefined)
  assert.equal(isolationLogCount, 1)

  const remote = getPrintDataSource(remoteCode)
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

/** 注册单字段测试数据源。 */
function registerSource(code: string): void {
  registerPrintDataSource({
    code,
    name: code,
    fields: [{ key: 'documentNo', label: '单据编号', kind: 'text' }],
    createSampleData: () => ({ documentNo: 'SAMPLE-1' }),
  })
}

/** 注入返回指定模板与数据源的稳定解析函数。 */
function configureFor(code: string, rowVersion = '1', dataSourceCode: null | string = code): void {
  configurePrinting({
    host: 'http://localhost:17521',
    token: 'test-token',
    resolveTemplate: async (_templateCode, scope): Promise<ResolvedPrintTemplate> => ({
      basicId: '1',
      dataSourceCode,
      engineVersion: '0.0.60',
      requestedScope: scope,
      resolvedScope: scope === 'Auto' ? 'Tenant' : scope,
      rowVersion,
      templateCode: code,
      templateJson: TemplateJson,
      templateName: code,
    }),
  })
}

/** 注入指定目录取数函数（可为空）的运行配置。 */
function configureCatalog(listDataSources: (() => Promise<RemotePrintDataSourceDto[]>) | undefined): void {
  configurePrinting({
    host: 'http://localhost:17521',
    token: 'test-token',
    resolveTemplate: async () => {
      throw new Error('目录测试不解析模板。')
    },
    listDataSources,
  })
}

/** 安装隔离的 Pinia 与内存 localStorage，支持本地打印机偏好测试。 */
function installBrowserPreferenceContext(): void {
  setActivePinia(createPinia())
  const userStore = useUserStore()
  userStore.userInfo = { basicId: 'tester', tenantId: 'tenant-7' } as typeof userStore.userInfo
  const values = new Map<string, string>()
  Object.defineProperty(globalThis, 'localStorage', {
    configurable: true,
    value: {
      getItem: (key: string) => values.get(key) ?? null,
      removeItem: (key: string) => values.delete(key),
      setItem: (key: string, value: string) => values.set(key, value),
    },
  })
}

/** 生成不会与其它测试或应用启动数据源冲突的编码。 */
function uniqueCode(prefix: string): string {
  return `test.${prefix}.${crypto.randomUUID()}`
}

/** 创建可触发一次性打印事件的内存适配器。 */
function createAdapterFixture(options: AdapterFixtureOptions = {}): AdapterFixture {
  const listeners = new Map<string, Map<string, (payload: unknown) => void>>()
  const previewData: unknown[] = []
  const directStartOrder: number[] = []
  const printers = options.printers ?? [{ name: 'Default Printer', isDefault: true }]
  let cleanupCount = 0
  let createdTemplateCount = 0
  const emit = (templateId: string, event: 'printError' | 'printSuccess', payload: unknown) => {
    listeners.get(templateId)?.get(event)?.(payload)
  }

  const adapter: PrintingAdapter = {
    createTemplate() {
      createdTemplateCount += 1
      const id = `template-${createdTemplateCount}`
      const callbacks = new Map<string, (payload: unknown) => void>()
      listeners.set(id, callbacks)
      return {
        id,
        clientIsOpened: () => options.connected !== false,
        clear: () => undefined,
        design: () => undefined,
        getJson: () => JSON.parse(TemplateJson) as unknown,
        getPaneltotal: () => 1,
        getPrinterList: () => printers,
        on: (eventName, callback) => callbacks.set(eventName, callback),
        print: (data, _printOptions, styleOptions) => {
          previewData.push(data)
          const callback = styleOptions?.callback
          if (typeof callback === 'function')
            callback()
        },
        print2: (data, printOptions) => {
          const sequence = directStartOrder.length + 1
          directStartOrder.push(sequence)
          if (options.onPrint2) {
            options.onPrint2(
              templateById(id, listeners),
              data,
              printOptions ?? {},
              sequence,
            )
          }
          else {
            queueMicrotask(() => emit(id, 'printSuccess', { sequence }))
          }
        },
        redo: () => undefined,
        selectPanel: () => undefined,
        setPaper: () => undefined,
        undo: () => undefined,
        zoom: () => undefined,
      }
    },
    enableFieldDragging: () => undefined,
    isClientConnected: () => options.connected !== false,
    listPrinters: () => printers,
    refreshPrinters: async () => printers,
    removePrintListeners: (template) => {
      cleanupCount += 1
      listeners.delete(template.id)
    },
  }

  const fixture: AdapterFixture = {
    adapter,
    directStartOrder,
    emit,
    get cleanupCount() {
      return cleanupCount
    },
    get createdTemplateCount() {
      return createdTemplateCount
    },
    previewData,
  }
  return fixture
}

/** 按 id 返回仅供测试回调发出事件的模板引用。 */
function templateById(
  id: string,
  listeners: Map<string, Map<string, (payload: unknown) => void>>,
): HiprintTemplateInstance {
  return {
    id,
    clientIsOpened: () => true,
    clear: () => undefined,
    design: () => undefined,
    getJson: () => ({}),
    getPaneltotal: () => 1,
    getPrinterList: () => [],
    on: (event, callback) => listeners.get(id)?.set(event, callback),
    print: () => undefined,
    print2: () => undefined,
    redo: () => undefined,
    selectPanel: () => undefined,
    setPaper: () => undefined,
    undo: () => undefined,
    zoom: () => undefined,
  }
}

/** 内存适配器创建选项。 */
interface AdapterFixtureOptions {
  connected?: boolean
  onPrint2?: (
    template: HiprintTemplateInstance,
    data: unknown,
    options: Record<string, unknown>,
    sequence: number,
  ) => void
  printers?: PrintDevice[]
}

/** 内存适配器测试观测面。 */
interface AdapterFixture {
  adapter: PrintingAdapter
  readonly cleanupCount: number
  readonly createdTemplateCount: number
  directStartOrder: number[]
  emit: (templateId: string, event: 'printError' | 'printSuccess', payload: unknown) => void
  previewData: unknown[]
}
