/**
 * 打印演示种子（后端 XiHan.BasicApp.Printing/Infrastructure/Seeders/Demo）的模板与数据源。
 *
 * 读后端随程序集发布的同一批 JSON：每张模板都要通过打印设计器的完整校验，用所绑数据源的样例数据排版时
 * 不出错误级诊断（条码、二维码取值不合法，图片地址不安全，表达式出错等，预览里就是报错占位），也不出格式失败；
 * 模板引用的字段都要在所绑数据源里登记。
 */
import type { PrintDocument } from '@xihan-print/core'
import type { PrintFieldDefinition } from './types'
import { readdirSync, readFileSync } from 'node:fs'
import { join } from 'node:path'
import { validateDocument } from '@xihan-print/core'
import { paginateDocument } from '@xihan-print/runtime'
import { describe, expect, it } from 'vitest'
import { listUnregisteredPrintFields } from './fields'

interface DemoDataSource {
  code: string
  name: string
  fields: PrintFieldDefinition[]
  sample: unknown
}

const demoRoot = join(__dirname, '..', '..', '..', 'backend', 'src', 'modules', 'XiHan.BasicApp.Printing', 'Infrastructure', 'Seeders', 'Demo')

/** 元素总览绑的内置数据源 system.print-demo：字段与样例同后端 BuiltInPrintDataSources.SystemPrintDemo */
const systemPrintDemo: DemoDataSource = {
  code: 'system.print-demo',
  name: '系统打印示例',
  fields: [
    { key: 'title', label: '标题' },
    { key: 'documentNo', label: '单据编号' },
    { key: 'customerName', label: '客户名称' },
    { key: 'createdTime', label: '制单时间', inputType: 'datetime' },
    { key: 'logo', label: 'Logo', kind: 'image' },
    { key: 'barcode', label: '单据条码', kind: 'barcode' },
    { key: 'qrCode', label: '单据二维码', kind: 'qrcode' },
    {
      key: 'items',
      label: '明细表',
      kind: 'table',
      columns: [
        { field: 'sku', title: '物料编码', width: 90 },
        { field: 'name', title: '物料名称', width: 120 },
        { field: 'quantity', title: '数量', width: 60, inputType: 'number' },
        { field: 'unit', title: '单位', width: 50 },
      ],
    },
  ],
  sample: {
    title: 'XiHan BasicApp 打印示例',
    documentNo: 'DEMO-20260729-0001',
    customerName: '示例客户',
    createdTime: '2026-07-29 10:00:00',
    logo: '/favicon.png',
    barcode: 'DEMO-20260729-0001',
    qrCode: 'https://basicapp.xihanfun.com/print-demo/DEMO-20260729-0001',
    items: [
      { sku: 'SKU-001', name: '示例物料 A', quantity: 2, unit: '件' },
      { sku: 'SKU-002', name: '示例物料 B', quantity: 5, unit: '箱' },
    ],
  },
}

function readJson<T>(...segments: string[]): T {
  return JSON.parse(readFileSync(join(demoRoot, ...segments), 'utf8')) as T
}

const dataSources = new Map<string, DemoDataSource>([
  [systemPrintDemo.code, systemPrintDemo],
  ...readdirSync(join(demoRoot, 'DataSources')).map((file): [string, DemoDataSource] => {
    const source = readJson<DemoDataSource>('DataSources', file)
    return [source.code, source]
  }),
])

/** 模板编码 → 所绑数据源：与后端 PrintingDemoCatalog 的登记一致 */
const templates: [code: string, dataSourceCode: string][] = [
  ['demo.pickup-receipt', 'demo.pickup-receipt'],
  ['demo.delivery-note', 'demo.delivery-note'],
  ['demo.formats', 'demo.formats'],
  ['demo.inspection', 'demo.inspection'],
  ['demo.outage-notice', 'demo.outage-notice'],
  ['demo.element-overview', 'system.print-demo'],
]

/** 不依赖 Canvas 的粗估度量器：每个字符按一个字号宽 */
const measurer = {
  version: 0,
  measure: (text: string, font: { size: number }) => ({ width: [...text].length * font.size, ascent: font.size * 0.8, descent: font.size * 0.2 }),
}

/** 预览里会显示成报错占位或丢值的诊断：错误级之外，再加格式、表达式与数据形状不对的警告 */
const blockingWarnings = new Set(['format.unavailable', 'format.failed', 'expression.unavailable', 'data.notArray', 'data.notScalar'])

describe('打印演示种子', () => {
  it('模板与数据源文件一一登记，没有漏掉或多出的', () => {
    expect(readdirSync(join(demoRoot, 'Templates')).map(file => file.replace(/\.json$/, '')).sort())
      .toEqual(templates.map(([code]) => code).sort())
    expect([...dataSources.keys()].filter(code => code.startsWith('demo.')).sort())
      .toEqual(templates.map(([, dataSource]) => dataSource).filter(code => code.startsWith('demo.')).sort())
  })

  it.each(templates)('%s 通过打印设计器的完整校验', (code) => {
    const document = readJson<PrintDocument>('Templates', `${code}.json`)

    expect(validateDocument(document)).toEqual({ valid: true, issues: [] })
  })

  it.each(templates)('%s 用所绑数据源的样例排版，没有报错占位', (code, dataSourceCode) => {
    const document = readJson<PrintDocument>('Templates', `${code}.json`)
    const source = dataSources.get(dataSourceCode)!

    const result = paginateDocument(document, source.sample, { measurer })

    expect(result.sheets.length).toBeGreaterThan(0)
    expect(result.diagnostics.filter(item => item.severity === 'error' || blockingWarnings.has(item.code))).toEqual([])
  })

  it.each(templates)('%s 引用的字段都在所绑数据源里登记', (code, dataSourceCode) => {
    const document = readJson<PrintDocument>('Templates', `${code}.json`)

    expect(listUnregisteredPrintFields(document, dataSources.get(dataSourceCode)!.fields)).toEqual([])
  })

  it('停电检修通知的样例故意带 <script>，排版时丢掉，不出现在输出里', () => {
    const document = readJson<PrintDocument>('Templates', 'demo.outage-notice.json')

    const result = paginateDocument(document, dataSources.get('demo.outage-notice')!.sample, { measurer })

    expect(result.diagnostics.map(item => item.code)).toEqual(['richText.droppedContent'])
  })
})
