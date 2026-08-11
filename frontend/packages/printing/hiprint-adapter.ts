/**
 * vue-plugin-hiprint 适配层。
 * 职责：动态加载固定版引擎、安装 print-lock.css、注册代码数据源 provider，并隔离全局 WebSocket/Event API。
 */
import type {
  PrintDataSourceDefinition,
  PrintDevice,
  PrintingAdapter,
  PrintingConfiguration,
} from './types'
import printLockUrl from 'vue-plugin-hiprint/dist/print-lock.css?url'
import { getPrintDataSourceRegistryVersion, getPrintFieldTid, listPrintDataSources } from './data-source-registry'

const DEFAULT_HIPRINT_HOST = 'http://localhost:17521'
const DEFAULT_HIPRINT_TOKEN = 'vue-plugin-hiprint'
const PRINT_LOCK_LINK_ID = 'xihan-hiprint-print-lock'

let configuration: PrintingConfiguration | null = null
let adapterPromise: Promise<PrintingAdapter> | null = null
let adapterOverride: PrintingAdapter | null = null

/**
 * 配置打印包；应用启动时必须注入后端模板解析函数。
 * @param options 运行配置；host/token 省略时读取 Vite 环境变量与安全默认值。
 * @returns 无返回值。
 * @throws resolveTemplate 缺失时抛出 TypeError。
 */
export function configurePrinting(options: PrintingConfiguration): void {
  if (!options || typeof options.resolveTemplate !== 'function')
    throw new TypeError('打印包必须配置 resolveTemplate。')

  configuration = {
    ...options,
    host: options.host?.trim() || import.meta.env.VITE_HIPRINT_HOST?.trim() || DEFAULT_HIPRINT_HOST,
    token: options.token?.trim() || import.meta.env.VITE_HIPRINT_TOKEN?.trim() || DEFAULT_HIPRINT_TOKEN,
  }
  // 配置可能在热更新或重新登录后变化，丢弃旧适配器以便下一次使用重新初始化连接。
  adapterPromise = null
  return undefined
}

/**
 * 返回打印运行配置。
 * @returns 已完成默认值归一的配置。
 * @throws 应用尚未调用 configurePrinting。
 */
export function getPrintingConfiguration(): PrintingConfiguration {
  if (!configuration)
    throw new Error('打印能力尚未初始化，请先调用 configurePrinting。')
  return configuration
}

/**
 * 为测试或宿主环境替换底层适配器。
 * @param adapter 适配器；传 null 恢复真实 hiprint 动态适配器。
 * @returns 无返回值。
 */
export function setPrintingAdapter(adapter: PrintingAdapter | null): void {
  adapterOverride = adapter
  return undefined
}

/**
 * 获取当前打印适配器。
 * @returns 可复用适配器实例。
 * @throws 浏览器环境、CSS 资源或 hiprint 加载失败。
 */
export async function getPrintingAdapter(): Promise<PrintingAdapter> {
  if (adapterOverride)
    return adapterOverride
  adapterPromise ??= loadHiprintAdapter()
  return adapterPromise
}

/** 动态加载第三方引擎并创建边界适配器。 */
async function loadHiprintAdapter(): Promise<PrintingAdapter> {
  if (typeof window === 'undefined' || typeof document === 'undefined')
    throw new Error('hiprint 只能在浏览器环境中使用。')

  await ensurePrintLockStyle()
  const module = await import('vue-plugin-hiprint')
  const config = getPrintingConfiguration()
  const DefaultElementTypeProvider = module.defaultElementTypeProvider

  let appliedRegistryVersion = -1

  /** 数据源注册表有新增时才重建 provider 并重初始化引擎，未变化时跳过（init 会重置连接配置）。 */
  function ensureProviders(): void {
    const version = getPrintDataSourceRegistryVersion()
    if (version === appliedRegistryVersion)
      return
    const providers = [
      new DefaultElementTypeProvider(),
      ...listPrintDataSources().map(source => createDataSourceProvider(module.hiprint, source)),
    ]
    module.hiprint.init({
      host: config.host,
      token: config.token,
      lang: 'cn',
      providers,
    })
    appliedRegistryVersion = version
  }

  ensureProviders()
  const PrintTemplate = module.hiprint.PrintTemplate

  return {
    createTemplate(template, options = {}) {
      ensureProviders()
      return new PrintTemplate({ ...options, template })
    },
    enableFieldDragging(elements) {
      ensureProviders()
      const jquery = window.$
      if (typeof jquery !== 'function')
        throw new Error('hiprint 未正确初始化 jQuery 拖拽运行时。')
      module.hiprint.PrintElementTypeManager.buildByHtml(jquery([...elements]))
    },
    isClientConnected() {
      return Boolean(window.hiwebSocket?.opened)
    },
    listPrinters() {
      return normalizePrinters(window.hiwebSocket?.getPrinterList?.() ?? [])
    },
    refreshPrinters(timeoutMs = 5_000) {
      return waitForPrinterRefresh(timeoutMs)
    },
    removePrintListeners(template) {
      // 上游模板只暴露 on（on 会先 clear），但事件总线挂在 window.hinnn；按模板 id 精确清理，不影响其它并行实例。
      window.hinnn?.event.clear(`printSuccess_${template.id}`)
      window.hinnn?.event.clear(`printError_${template.id}`)
    },
  }
}

/** 创建一个把业务字段映射为 hiprint PrintElementTypeGroup 的动态 provider。 */
function createDataSourceProvider(
  hiprint: {
    PrintElementTypeGroup: new (name: string, elements: Record<string, unknown>[]) => unknown
  },
  source: ReturnType<typeof listPrintDataSources>[number],
): { addElementTypes: (manager: DynamicElementTypeManager) => void } {
  const moduleCode = `xihan.${source.code}`
  return {
    addElementTypes(manager) {
      manager.removePrintElementTypes(moduleCode)
      manager.addPrintElementTypes(moduleCode, [
        new hiprint.PrintElementTypeGroup(
          source.name,
          buildHiprintElementDefinitions(source),
        ),
      ])
    },
  }
}

/**
 * 把已校验的数据源字段转换为 hiprint 0.0.60 的元素协议。
 * @param source 已由注册表完成规范化的数据源。
 * @returns 可传给 PrintElementTypeGroup 的元素定义。
 * @remarks 条码和二维码在上游引擎中属于 text 元素，必须通过 textType 区分；直接使用 barcode/qrcode 作为 type 会导致素材无法实例化。
 */
export function buildHiprintElementDefinitions(
  source: Readonly<PrintDataSourceDefinition<unknown>>,
): Record<string, unknown>[] {
  return source.fields.map((field) => {
    const kind = field.kind ?? 'text'
    const isEncodedText = kind === 'barcode' || kind === 'qrcode'
    return {
      tid: getPrintFieldTid(source.code, field.key),
      title: field.label,
      field: field.key,
      type: isEncodedText ? 'text' : kind,
      ...(isEncodedText ? { textType: kind } : {}),
      ...(kind === 'table'
        ? {
            columns: [[...field.columns!].map(column => ({
              title: column.title,
              field: column.field,
              width: column.width ?? 100,
            }))],
            editable: true,
            columnDisplayEditable: true,
            columnTitleEditable: true,
            columnResizable: true,
          }
        : {}),
    }
  })
}

/** 等待 electron-hiprint 返回新的打印机列表，并在成功、超时或异常时移除全局监听。 */
function waitForPrinterRefresh(timeoutMs: number): Promise<PrintDevice[]> {
  if (!window.hiwebSocket?.opened)
    return Promise.reject(new Error('electron-hiprint 客户端未连接，请检查客户端、地址与 token。'))
  if (!Number.isFinite(timeoutMs) || timeoutMs <= 0)
    return Promise.reject(new RangeError('打印机刷新超时时间必须大于 0。'))

  return new Promise<PrintDevice[]>((resolve, reject) => {
    const eventBus = window.hinnn?.event
    if (!eventBus) {
      reject(new Error('hiprint 事件总线未初始化。'))
      return
    }

    let timer: ReturnType<typeof setTimeout> | null = null
    let onPrinterList: (payload: unknown) => void = () => undefined
    const cleanup = () => {
      if (timer)
        clearTimeout(timer)
      eventBus.off('printerList', onPrinterList)
    }
    onPrinterList = (payload: unknown) => {
      cleanup()
      resolve(normalizePrinters(payload))
    }

    eventBus.on('printerList', onPrinterList)
    timer = setTimeout(() => {
      cleanup()
      reject(new Error('刷新打印机列表超时，请检查 electron-hiprint 客户端连接。'))
    }, timeoutMs)

    try {
      window.hiwebSocket!.refreshPrinterList()
    }
    catch (error) {
      cleanup()
      reject(error instanceof Error ? error : new Error('刷新打印机列表失败。'))
    }
  })
}

/** 把客户端松散载荷归一为只包含有效 name 的打印机快照。 */
function normalizePrinters(payload: unknown): PrintDevice[] {
  if (!Array.isArray(payload))
    return []
  return payload
    .filter((item): item is PrintDevice => Boolean(item && typeof item === 'object' && typeof item.name === 'string' && item.name.trim()))
    .map(item => ({ ...item, name: item.name.trim() }))
}

/** 安装带 media=print 且文件名含 print-lock 的 link，满足上游 print2 的运行时检查。 */
function ensurePrintLockStyle(): Promise<void> {
  const existing = document.getElementById(PRINT_LOCK_LINK_ID) as HTMLLinkElement | null
  if (existing)
    return Promise.resolve()

  return new Promise<void>((resolve, reject) => {
    const link = document.createElement('link')
    link.id = PRINT_LOCK_LINK_ID
    link.rel = 'stylesheet'
    link.type = 'text/css'
    link.media = 'print'
    link.href = printLockUrl
    link.addEventListener('load', () => resolve(), { once: true })
    link.addEventListener('error', () => reject(new Error('print-lock.css 加载失败。')), { once: true })
    document.head.append(link)
  })
}

/** 上游 provider 注册器的最小结构。 */
interface DynamicElementTypeManager {
  addPrintElementTypes: (moduleCode: string, groups: unknown[]) => void
  removePrintElementTypes: (moduleCode: string) => void
}
