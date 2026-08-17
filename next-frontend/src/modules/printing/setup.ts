/**
 * 应用打印能力启动注册。
 * 职责：把后端模板解析与数据源目录 API 注入公共 printing 包；
 * 数据源目录以后端注册表为单一事实源（含 system.print-demo），惰性拉取。
 */
import type { PrintTemplateScope as ApiPrintTemplateScope } from './api/print-template.types'
import type { PrintTemplateScope as PublicPrintTemplateScope, ResolvedPrintTemplate } from '~/printing'
import { configurePrinting } from '~/printing'
import { printDataSourceApi } from './api/print-data-source'
import { printTemplateApi } from './api/print-template'

/**
 * 注册应用打印配置。
 * @returns 无返回值；hiprint 本体仍保持惰性加载，首次设计或打印时才产生独立 chunk 请求。
 */
export default function setupPrinting(): void {
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
    listDataSources: () => printDataSourceApi.list(),
  })
}
