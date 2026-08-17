/**
 * 公共打印服务。
 * 职责：按编码解析模板、按需校验可选代码数据源、打开浏览器预览，以及通过当前会话 FIFO 队列执行 electron-hiprint 直打。
 */
import type {
  DirectPrintOptions,
  HiprintTemplateInstance,
  PreviewPrintOptions,
  PrintDevice,
  PrintJobResult,
  PrintTemplateScope,
  ResolvedPrintTemplate,
} from './types'
import { requirePrintDataSource } from './data-source-registry'
import { getPrintingAdapter, getPrintingConfiguration } from './hiprint-adapter'
import { getPreferredPrinter, savePreferredPrinter } from './printer-preference'
import { ensureRemotePrintDataSourcesLoaded } from './remote-data-sources'

/**
 * 模拟数据表单打开后，服务端模板版本已经发生变化。
 * 调用方可据此展示本地化提示，而无需依赖错误文本进行脆弱判断。
 */
export class PrintTemplateVersionChangedError extends Error {
  /** 创建一个模板版本冲突错误。 */
  constructor() {
    super('打印模板已被更新，请关闭模拟数据表单后重新打开。')
    this.name = 'PrintTemplateVersionChangedError'
  }
}

const DEFAULT_SCOPE: PrintTemplateScope = 'Auto'
const DEFAULT_DIRECT_TIMEOUT_MS = 30_000
const MIN_COPIES = 1
const MAX_COPIES = 99

// Promise 链只保存任务完成信号，不保存打印数据；成功或失败都吞入下一环，确保后续任务不会被前一个失败永久阻塞。
let directQueueTail: Promise<void> = Promise.resolve()

/**
 * 按模板编码打开浏览器打印预览。
 * @param templateCode 后端打印模板编码。
 * @param data 单对象或对象数组打印数据；仅保存在当前浏览器内存。
 * @param options 作用域和标题选项。
 * @returns 预览窗口调用完成信号；不表示用户最终确认打印。
 * @throws 模板不可用、数据源未注册、JSON 损坏或浏览器阻止打印窗口。
 */
export async function previewPrintByCode<T>(
  templateCode: string,
  data: T | T[],
  options: PreviewPrintOptions = {},
): Promise<void> {
  const resolved = await resolveTemplate(templateCode, options.scope ?? DEFAULT_SCOPE)
  ensureExpectedRowVersion(resolved, options.expectedRowVersion)
  const adapter = await getPrintingAdapter()
  const template = adapter.createTemplate(parseTemplateJson(resolved))

  // hiprint 的 print 调用同步创建预览窗口；其 callback 只能说明窗口已打开，无法知道用户点击了打印还是取消。
  await new Promise<void>((resolve, reject) => {
    let settled = false
    const complete = () => {
      if (settled)
        return
      settled = true
      resolve()
    }
    try {
      template.print(
        data,
        options.title ? { title: options.title } : {},
        { callback: complete },
      )
      // 某些浏览器版本不会触发上游 callback；同步调用无异常即表示预览打开动作已经发起。
      queueMicrotask(complete)
    }
    catch (error) {
      settled = true
      reject(error instanceof Error ? error : new Error('打开打印预览失败。'))
    }
  })
}

/**
 * 确认模拟数据表单对应的模板版本仍是本次即将预览的版本。
 * @param resolved 后端刚解析出的最新模板。
 * @param expectedRowVersion 打开模拟数据表单时读取的字符串行版本。
 * @throws 预期版本为空白或模板已被并行更新。
 */
function ensureExpectedRowVersion(
  resolved: ResolvedPrintTemplate,
  expectedRowVersion: string | undefined,
): void {
  if (expectedRowVersion === undefined)
    return
  const expected = expectedRowVersion.trim()
  if (!expected)
    throw new TypeError('预期打印模板版本不能为空。')
  if (resolved.rowVersion !== expected)
    throw new PrintTemplateVersionChangedError()
}

/**
 * 按模板编码加入当前会话 FIFO 直打队列。
 * @param templateCode 后端打印模板编码。
 * @param data 单对象或对象数组打印数据；任务完成后即可由调用方释放。
 * @param options 打印机、份数、标题、作用域、超时和取消信号。
 * @returns 客户端明确返回成功事件后的作业结果。
 * @throws 客户端离线、token 错误、无打印机、打印机不存在、超时、取消或客户端失败事件。
 */
export function directPrintByCode<T>(
  templateCode: string,
  data: T | T[],
  options: DirectPrintOptions = {},
): Promise<PrintJobResult> {
  const run = directQueueTail.then(
    () => executeDirectPrint(templateCode, data, options),
    () => executeDirectPrint(templateCode, data, options),
  )
  directQueueTail = run.then(() => undefined, () => undefined)
  return run
}

/**
 * 读取 electron-hiprint 当前打印机快照。
 * @returns 打印机列表。
 * @throws 客户端未连接。
 */
export async function listPrinters(): Promise<PrintDevice[]> {
  const adapter = await getPrintingAdapter()
  ensureClientConnected(adapter.isClientConnected())
  return adapter.listPrinters()
}

/**
 * 请求 electron-hiprint 刷新打印机列表。
 * @param timeoutMs 刷新等待超时，默认 5 秒。
 * @returns 客户端最新打印机列表。
 * @throws 客户端离线、事件总线异常或超时。
 */
export async function refreshPrinters(timeoutMs = 5_000): Promise<PrintDevice[]> {
  const adapter = await getPrintingAdapter()
  ensureClientConnected(adapter.isClientConnected())
  return adapter.refreshPrinters(timeoutMs)
}

/**
 * 设置当前用户、租户和模板的本地打印机偏好。
 * @param templateCode 模板编码。
 * @param printerName 打印机名称；null 表示清除偏好。
 * @returns 完成信号。
 * @throws 指定打印机不在当前客户端列表中或客户端离线。
 */
export async function setPreferredPrinter(
  templateCode: string,
  printerName: null | string,
): Promise<void> {
  const normalized = printerName?.trim()
  if (normalized) {
    const printers = await listPrinters()
    if (!printers.some(printer => printer.name === normalized))
      throw new Error(`打印机不存在或已离线：${normalized}`)
  }
  savePreferredPrinter(templateCode, normalized || null)
}

/** 执行队首直打任务并只为该模板实例绑定一次性事件。 */
async function executeDirectPrint<T>(
  templateCode: string,
  data: T | T[],
  options: DirectPrintOptions,
): Promise<PrintJobResult> {
  options.signal?.throwIfAborted()
  const copies = normalizeCopies(options.copies)
  const timeoutMs = normalizeTimeout(options.timeoutMs)
  const resolved = await resolveTemplate(templateCode, options.scope ?? DEFAULT_SCOPE)
  options.signal?.throwIfAborted()

  const adapter = await getPrintingAdapter()
  ensureClientConnected(adapter.isClientConnected())
  let printers = adapter.listPrinters()
  if (printers.length === 0)
    printers = await adapter.refreshPrinters(Math.min(timeoutMs, 5_000))
  if (printers.length === 0)
    throw new Error('electron-hiprint 客户端未返回任何可用打印机。')

  const printerName = choosePrinter(templateCode, printers, options.printerName)
  const template = adapter.createTemplate(parseTemplateJson(resolved))
  return waitForDirectPrint(template, resolved, data, {
    adapter,
    copies,
    printerName,
    signal: options.signal,
    timeoutMs,
    title: options.title?.trim() || resolved.templateName,
  })
}

/** 使用 success/error 事件完成一个任务，并在所有终止路径精确清理监听器和计时器。 */
function waitForDirectPrint<T>(
  template: HiprintTemplateInstance,
  resolved: ResolvedPrintTemplate,
  data: T | T[],
  context: DirectPrintContext,
): Promise<PrintJobResult> {
  // 打印机查询和模板创建期间也可能发生组件卸载；绑定事件前再次检查，避免已取消信号只能等到超时。
  context.signal?.throwIfAborted()
  return new Promise<PrintJobResult>((resolve, reject) => {
    let settled = false
    let timer: ReturnType<typeof setTimeout> | null = null
    let onAbort: () => void = () => undefined

    const cleanup = () => {
      if (timer)
        clearTimeout(timer)
      context.signal?.removeEventListener('abort', onAbort)
      context.adapter.removePrintListeners(template)
    }
    const settle = (callback: () => void) => {
      if (settled)
        return
      settled = true
      cleanup()
      callback()
    }
    onAbort = () => settle(() => reject(context.signal?.reason instanceof Error
      ? context.signal.reason
      : new DOMException('打印任务已取消。', 'AbortError')))
    const onSuccess = (payload: unknown) => settle(() => resolve({
      templateCode: resolved.templateCode,
      resolvedScope: resolved.resolvedScope,
      printerName: context.printerName,
      copies: context.copies,
      payload,
    }))
    const onError = (payload: unknown) => settle(() => reject(toClientError(payload)))

    template.on('printSuccess', onSuccess)
    template.on('printError', onError)
    context.signal?.addEventListener('abort', onAbort, { once: true })
    timer = setTimeout(
      () => settle(() => reject(new Error(`直接打印等待客户端响应超时（${context.timeoutMs}ms）。`))),
      context.timeoutMs,
    )

    try {
      template.print2(data, {
        printer: context.printerName,
        copies: context.copies,
        title: context.title,
      })
    }
    catch (error) {
      settle(() => reject(error instanceof Error ? error : new Error('提交直接打印任务失败。')))
    }
  })
}

/** 按显式指定、本地偏好、客户端默认的固定优先级选择打印机。 */
function choosePrinter(
  templateCode: string,
  printers: PrintDevice[],
  explicitPrinter?: string,
): string {
  const explicit = explicitPrinter?.trim()
  if (explicit) {
    if (!printers.some(printer => printer.name === explicit))
      throw new Error(`指定打印机不存在或已离线：${explicit}`)
    return explicit
  }

  const preferred = getPreferredPrinter(templateCode)
  if (preferred && printers.some(printer => printer.name === preferred))
    return preferred

  const clientDefault = printers.find(printer => printer.isDefault === true || printer.default === true)
  // 空字符串是 electron-hiprint 使用系统默认打印机的官方约定；列表非空已在调用方保证。
  return clientDefault?.name ?? ''
}

/** 解析后端模板；契约增强模式确认数据源已注册，自由模式直接使用模板标准字段绑定。 */
async function resolveTemplate(
  templateCode: string,
  scope: PrintTemplateScope,
): Promise<ResolvedPrintTemplate> {
  const normalizedCode = templateCode?.trim()
  if (!normalizedCode)
    throw new Error('打印模板编码不能为空。')
  const resolved = await getPrintingConfiguration().resolveTemplate(normalizedCode, scope)
  // 自由模板不依赖目录；仅在模板绑定数据源时才拉取，目录接口异常不影响自由模板打印
  if (resolved.dataSourceCode?.trim()) {
    await ensureRemotePrintDataSourcesLoaded()
    requirePrintDataSource(resolved.dataSourceCode)
  }
  return resolved
}

/** 解析数据库模板 JSON，并拒绝非对象或缺少 panels 的损坏缓存/响应。 */
function parseTemplateJson(resolved: ResolvedPrintTemplate): Record<string, unknown> {
  try {
    const template = JSON.parse(resolved.templateJson) as unknown
    if (!template || typeof template !== 'object' || Array.isArray(template))
      throw new Error('模板根节点不是对象。')
    if (!Array.isArray((template as { panels?: unknown }).panels))
      throw new Error('模板缺少 panels 数组。')
    return template as Record<string, unknown>
  }
  catch (error) {
    throw new Error(`打印模板 ${resolved.templateCode} JSON 无效。`, { cause: error })
  }
}

/** 校验客户端连接；token 错误与离线在上游都表现为连接未建立，提示同时覆盖两类排查方向。 */
function ensureClientConnected(connected: boolean): void {
  if (!connected)
    throw new Error('electron-hiprint 客户端未连接，请检查客户端、VITE_HIPRINT_HOST 与 VITE_HIPRINT_TOKEN。')
}

/** 归一 1～99 的打印份数。 */
function normalizeCopies(copies = 1): number {
  if (!Number.isInteger(copies) || copies < MIN_COPIES || copies > MAX_COPIES)
    throw new RangeError(`打印份数必须是 ${MIN_COPIES}～${MAX_COPIES} 的整数。`)
  return copies
}

/** 归一直打超时。 */
function normalizeTimeout(timeoutMs = DEFAULT_DIRECT_TIMEOUT_MS): number {
  if (!Number.isFinite(timeoutMs) || timeoutMs <= 0)
    throw new RangeError('直接打印超时时间必须大于 0。')
  return timeoutMs
}

/** 把客户端失败载荷转为不包含配置 token 的友好异常。 */
function toClientError(payload: unknown): Error {
  if (payload instanceof Error)
    return new Error(`直接打印失败：${payload.message}`)
  if (payload && typeof payload === 'object') {
    const source = payload as { message?: unknown, msg?: unknown }
    const message = typeof source.message === 'string'
      ? source.message
      : typeof source.msg === 'string'
        ? source.msg
        : ''
    if (message)
      return new Error(`直接打印失败：${message}`)
  }
  return new Error('electron-hiprint 客户端报告打印失败。')
}

/** 单个直打任务的不可变执行上下文。 */
interface DirectPrintContext {
  adapter: Awaited<ReturnType<typeof getPrintingAdapter>>
  copies: number
  printerName: string
  signal?: AbortSignal
  timeoutMs: number
  title: string
}
