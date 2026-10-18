/**
 * 打印运行时（@xihan-print/runtime 与本地打印客户端 SDK）的按需加载。
 * 职责：首次打印时才加载排版与客户端代码，不进首屏；一份数据直接交给运行时，多份数据各自分页后接成一份一次打印。
 * 整个页面共用一个客户端连接，创建时不连，第一次用到时才连。
 */
import type { PaginationResult, PrintDocument } from '@xihan-print/core'
import type { ClientPrintOptions, PrintClient, PrintClientJob } from '@xihan-print/runtime/client'

/** 打印运行时：浏览器打印与本地客户端打印。 */
export interface PrintRuntime {
  /** 在看不见的 iframe 里排出输出页并调起浏览器打印对话框；返回送去打印的页数。 */
  printInBrowser: (document: PrintDocument, records: readonly unknown[], options: { title?: string }) => Promise<number>
  /** 交给本地打印客户端静默打印。 */
  printWithClient: (document: PrintDocument, records: readonly unknown[], options: ClientPrintOptions) => PrintClientJob
  /** 页面共用的客户端连接。 */
  client: () => PrintClient
}

let runtimePromise: Promise<PrintRuntime> | null = null

/**
 * 加载打印运行时；并发调用共享同一次加载，失败不缓存。
 * @returns 打印运行时。
 */
export function loadPrintRuntime(): Promise<PrintRuntime> {
  runtimePromise ??= createRuntime().catch((error) => {
    runtimePromise = null
    throw error
  })
  return runtimePromise
}

async function createRuntime(): Promise<PrintRuntime> {
  const [runtime, clientModule, core, viz, expression, locales] = await Promise.all([
    import('@xihan-print/runtime'),
    import('@xihan-print/runtime/client'),
    import('@xihan-print/core'),
    import('@xihan-ui/viz'),
    import('@xihan-ui/expression'),
    import('~/locales'),
  ])

  let client: PrintClient | null = null
  const locale = () => locales.i18n.global.locale.value

  /** 多份数据各自分页再接成一份：注入项与运行时 printDocument 的缺省一致（Canvas 度量器、受限表达式、formatValue）。 */
  function paginateRecords(document: PrintDocument, records: readonly unknown[]): PaginationResult {
    const measurer = viz.createCanvasMeasurer(globalThis.document)
    if (!measurer)
      throw new Error('浏览器拿不到 Canvas 2D 上下文，无法排版打印模板。')
    const options = {
      measurer,
      expressionEngine: expression.createExpressionEngine(),
      formatter: expression.formatValue,
      locale: locale(),
    }
    return runtime.mergePaginationResults(records.map(record => core.paginate(document, record, options)))
  }

  return {
    async printInBrowser(document, records, options) {
      if (records.length === 1) {
        const outcome = await runtime.printDocument(document, records[0], { ...options, locale: locale() })
        return outcome.sheets
      }
      const result = paginateRecords(document, records)
      await runtime.printSheets(result, options)
      return result.sheets.length
    },
    printWithClient(document, records, options) {
      const target = (client ??= clientModule.createPrintClient())
      return records.length === 1
        ? target.printDocument(document, records[0], { ...options, locale: locale() })
        : target.printSheets(paginateRecords(document, records), options)
    },
    client: () => (client ??= clientModule.createPrintClient()),
  }
}
