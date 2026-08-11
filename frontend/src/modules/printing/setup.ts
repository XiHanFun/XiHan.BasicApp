/**
 * 应用打印能力启动注册。
 * 职责：把后端模板解析 API 注入公共 printing 包，并注册首版 system.print-demo 代码数据源。
 */
import type { PrintTemplateScope as ApiPrintTemplateScope } from './api/print-template.types'
import type { PrintTemplateScope as PublicPrintTemplateScope, ResolvedPrintTemplate } from '~/printing'
import { configurePrinting, registerPrintDataSource } from '~/printing'
import { printTemplateApi } from './api/print-template'

/**
 * 注册应用打印配置与内置示例数据源。
 * @returns 无返回值；hiprint 本体仍保持惰性加载，首次设计或打印时才产生独立 chunk 请求。
 * @throws 数据源重复注册或配置不完整。
 */
export default function setupPrinting(): void {
  registerPrintDataSource({
    code: 'system.print-demo',
    name: '系统打印示例',
    fields: [
      { key: 'title', label: '标题', kind: 'text' },
      { key: 'documentNo', label: '单据编号', kind: 'text' },
      { key: 'customerName', label: '客户名称', kind: 'text' },
      { key: 'createdTime', label: '制单时间', kind: 'text', inputType: 'datetime' },
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
    createSampleData: () => ({
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
    }),
  })

  configurePrinting({
    host: import.meta.env.VITE_HIPRINT_HOST,
    token: import.meta.env.VITE_HIPRINT_TOKEN,
    resolveTemplate: async (templateCode, scope): Promise<ResolvedPrintTemplate> => {
      const resolved = await printTemplateApi.resolveByCode(templateCode, scope as ApiPrintTemplateScope)
      return {
        ...resolved,
        requestedScope: resolved.requestedScope as PublicPrintTemplateScope,
        resolvedScope: resolved.resolvedScope as PublicPrintTemplateScope,
      }
    },
  })
  return undefined
}
