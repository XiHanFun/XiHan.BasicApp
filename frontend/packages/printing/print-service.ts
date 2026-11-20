/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

/**
 * 公共打印服务
 * 职责：按编码解析模板、按需校验可选代码数据源、调起浏览器打印，以及通过当前会话 FIFO 队列交给本地打印客户端直打
 * 直打连不上客户端时明确报错，不退回浏览器打印
 */
import type { PrintDocument } from '@xihan-print/core'
import type { PrintClientError } from '@xihan-print/runtime/client'
import type {
  DirectPrintOptions,
  PreviewPrintOptions,
  PrintDevice,
  PrintJobResult,
  PrintTemplateScope,
  ResolvedPrintTemplate,
} from './types'
import { $t } from '~/locales'
import { getPrintingConfiguration } from './configuration'
import { requirePrintDataSource } from './data-source-registry'
import { parsePrintTemplateJson } from './document'
import { getPreferredPrinter, savePreferredPrinter } from './printer-preference'
import { ensureRemotePrintDataSourcesLoaded } from './remote-data-sources'
import { loadPrintRuntime } from './runtime'

/**
 * 模拟数据表单打开后，服务端模板版本已经发生变化
 * 调用方可据此展示本地化提示，而无需依赖错误文本进行脆弱判断
 */
export class PrintTemplateVersionChangedError extends Error {
  /** 创建一个模板版本冲突错误 */
  constructor() {
    super($t('print_service.template_version_changed'))
    this.name = 'PrintTemplateVersionChangedError'
  }
}

const DEFAULT_SCOPE: PrintTemplateScope = 'Auto'
const DEFAULT_DIRECT_TIMEOUT_MS = 30_000
const MIN_COPIES = 1
const MAX_COPIES = 99

/** 客户端错误码 → 提示的文案键；没列出的按「直接打印失败」加错误码提示 */
const CLIENT_ERROR_MESSAGES: Readonly<Record<string, string>> = {
  CLIENT_UNAVAILABLE: 'print_service.client_unavailable',
  PROTOCOL_UNSUPPORTED: 'print_service.protocol_unsupported',
  LOCAL_NETWORK_DENIED: 'print_service.local_network_denied',
  CONSENT_DENIED: 'print_service.consent_denied',
  CONSENT_TIMEOUT: 'print_service.consent_timeout',
  NO_PRINTERS: 'print_service.no_printers',
  PRINTER_NOT_FOUND: 'print_service.printer_not_found',
  PRINTER_OFFLINE: 'print_service.printer_offline',
  PAPER_UNSUPPORTED: 'print_service.paper_unsupported',
  TIMEOUT: 'print_service.timeout',
  CONNECTION_LOST: 'print_service.connection_lost',
}

// Promise 链只保存任务完成信号，不保存打印数据；成功或失败都吞入下一环，确保后续任务不会被前一个失败永久阻塞
let directQueueTail: Promise<void> = Promise.resolve()

/**
 * 按模板编码调起浏览器打印（输出页在看不见的 iframe 里排好，再打开打印对话框）
 * @param templateCode 后端打印模板编码
 * @param data 单对象或对象数组打印数据；数组时各自分页后一次打印，仅保存在当前浏览器内存
 * @param options 作用域、标题与预期行版本
 * @returns 打印对话框已调起的完成信号；不表示用户最终确认打印
 * @throws 模板不可用、不是当前格式、数据源未注册或排版失败
 */
export async function previewPrintByCode<T>(
  templateCode: string,
  data: T | T[],
  options: PreviewPrintOptions = {},
): Promise<void> {
  const resolved = await resolveTemplate(templateCode, options.scope ?? DEFAULT_SCOPE)
  ensureExpectedRowVersion(resolved, options.expectedRowVersion)
  const document = parsePrintTemplateJson(resolved.templateJson, resolved.templateCode)
  const runtime = await loadPrintRuntime()
  await runtime.printInBrowser(document, toRecords(data), { title: options.title?.trim() || resolved.templateName })
}

/**
 * 用一份模板（如编辑器里还没保存的设计）调起浏览器打印
 * @param document 打印模板
 * @param data 单对象或对象数组打印数据；数组时各自分页后一次打印
 * @param options 打印选项
 * @param options.title 打印对话框与另存 PDF 时的缺省文件名
 * @returns 打印对话框已调起的完成信号
 * @throws 模板有错误级问题或排版失败
 */
export async function previewPrintDocument<T>(
  document: PrintDocument,
  data: T | T[],
  options: { title?: string } = {},
): Promise<void> {
  const runtime = await loadPrintRuntime()
  await runtime.printInBrowser(document, toRecords(data), options)
}

/**
 * 确认模拟数据表单对应的模板版本仍是本次即将打印的版本
 * @param resolved 后端刚解析出的最新模板
 * @param expectedRowVersion 打开模拟数据表单时读取的字符串行版本
 * @throws 预期版本为空白或模板已被并行更新
 */
function ensureExpectedRowVersion(
  resolved: ResolvedPrintTemplate,
  expectedRowVersion: string | undefined,
): void {
  if (expectedRowVersion === undefined)
    return
  const expected = expectedRowVersion.trim()
  if (!expected)
    throw new TypeError($t('print_service.version_required'))
  if (resolved.rowVersion !== expected)
    throw new PrintTemplateVersionChangedError()
}

/**
 * 按模板编码加入当前会话 FIFO 直打队列，交给本地打印客户端静默打印
 * @param templateCode 后端打印模板编码
 * @param data 单对象或对象数组打印数据；任务完成后即可由调用方释放
 * @param options 打印机、份数、作用域、超时和取消信号
 * @returns 客户端把任务交给系统打印队列后的结果
 * @throws 客户端不可用、未授权、无打印机、打印机不存在、超时、取消或客户端报告失败
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
 * 读取本地打印客户端的打印机列表
 * @returns 打印机列表
 * @throws 客户端不可用或未授权
 */
export async function listPrinters(): Promise<PrintDevice[]> {
  const runtime = await loadPrintRuntime()
  try {
    const printers = await runtime.client().printers()
    return printers.map(printer => ({ name: printer.name, isDefault: printer.isDefault, status: printer.status }))
  }
  catch (error) {
    throw toClientError(error)
  }
}

/**
 * 重新向本地打印客户端查询打印机列表
 * @returns 客户端最新打印机列表
 * @throws 客户端不可用或未授权
 */
export function refreshPrinters(): Promise<PrintDevice[]> {
  return listPrinters()
}

/**
 * 设置当前用户、租户和模板的本地打印机偏好
 * @param templateCode 模板编码
 * @param printerName 打印机名称；null 表示清除偏好
 * @returns 完成信号
 * @throws 指定打印机不在当前客户端列表中或客户端不可用
 */
export async function setPreferredPrinter(
  templateCode: string,
  printerName: null | string,
): Promise<void> {
  const normalized = printerName?.trim()
  if (normalized) {
    const printers = await listPrinters()
    if (!printers.some(printer => printer.name === normalized))
      throw new Error($t('print_service.printer_unavailable', { name: normalized }))
  }
  savePreferredPrinter(templateCode, normalized || null)
}

/** 执行队首直打任务：选打印机、交给客户端、等它交给系统打印队列 */
async function executeDirectPrint<T>(
  templateCode: string,
  data: T | T[],
  options: DirectPrintOptions,
): Promise<PrintJobResult> {
  options.signal?.throwIfAborted()
  const copies = normalizeCopies(options.copies)
  const timeoutMs = normalizeTimeout(options.timeoutMs)
  const resolved = await resolveTemplate(templateCode, options.scope ?? DEFAULT_SCOPE)
  const document = parsePrintTemplateJson(resolved.templateJson, resolved.templateCode)
  options.signal?.throwIfAborted()

  const printers = await listPrinters()
  if (printers.length === 0)
    throw new Error($t('print_service.no_printers'))
  const printerName = choosePrinter(templateCode, printers, options.printerName)
  options.signal?.throwIfAborted()

  const runtime = await loadPrintRuntime()
  let job
  try {
    job = runtime.printWithClient(document, toRecords(data), {
      ...(printerName
        ? { printer: printerName }
        : {}),
      copies,
      timeoutMs,
    })
  }
  catch (error) {
    throw toClientError(error)
  }

  const onAbort = () => {
    void job.cancel()
  }
  options.signal?.addEventListener('abort', onAbort, { once: true })
  try {
    await job.done
  }
  catch (error) {
    if (options.signal?.aborted) {
      throw options.signal.reason instanceof Error
        ? options.signal.reason
        : new DOMException($t('print_service.cancelled'), 'AbortError')
    }
    throw toClientError(error)
  }
  finally {
    options.signal?.removeEventListener('abort', onAbort)
  }
  return {
    templateCode: resolved.templateCode,
    resolvedScope: resolved.resolvedScope,
    printerName,
    copies,
  }
}

/** 按显式指定 → 本地偏好 → 客户端默认选打印机；空串即客户端默认 */
function choosePrinter(
  templateCode: string,
  printers: PrintDevice[],
  explicitPrinter?: string,
): string {
  const explicit = explicitPrinter?.trim()
  if (explicit) {
    if (!printers.some(printer => printer.name === explicit))
      throw new Error($t('print_service.explicit_printer_unavailable', { name: explicit }))
    return explicit
  }

  const preferred = getPreferredPrinter(templateCode)
  if (preferred && printers.some(printer => printer.name === preferred))
    return preferred

  return printers.find(printer => printer.isDefault)?.name ?? ''
}

/** 解析后端模板；绑了数据源的先确认已注册，自由模板按其字段收数据 */
async function resolveTemplate(
  templateCode: string,
  scope: PrintTemplateScope,
): Promise<ResolvedPrintTemplate> {
  const normalizedCode = templateCode?.trim()
  if (!normalizedCode)
    throw new Error($t('print_service.code_required'))
  const resolved = await getPrintingConfiguration().resolveTemplate(normalizedCode, scope)
  // 自由模板不依赖目录；仅在模板绑定数据源时才拉取，目录接口异常不影响自由模板打印
  if (resolved.dataSourceCode?.trim()) {
    await ensureRemotePrintDataSourcesLoaded()
    requirePrintDataSource(resolved.dataSourceCode)
  }
  return resolved
}

/** 单对象包成一条记录，数组原样；空数组拒绝 */
function toRecords<T>(data: T | T[]): unknown[] {
  const records = Array.isArray(data)
    ? data
    : [data]
  if (records.length === 0)
    throw new RangeError($t('print_service.data_empty'))
  return records
}

/** 归一 1～99 的打印份数 */
function normalizeCopies(copies = 1): number {
  if (!Number.isInteger(copies) || copies < MIN_COPIES || copies > MAX_COPIES)
    throw new RangeError($t('print_service.copies_range', { min: MIN_COPIES, max: MAX_COPIES }))
  return copies
}

/** 归一直打超时 */
function normalizeTimeout(timeoutMs = DEFAULT_DIRECT_TIMEOUT_MS): number {
  if (!Number.isFinite(timeoutMs) || timeoutMs <= 0)
    throw new RangeError($t('print_service.timeout_positive'))
  return timeoutMs
}

/** 客户端错误换成按错误码的提示；别的错误原样抛出 */
function toClientError(error: unknown): Error {
  if (error instanceof Error && error.name === 'PrintClientError') {
    const code = (error as PrintClientError).code
    const key = CLIENT_ERROR_MESSAGES[code]
    return new Error(key
      ? $t(key)
      : $t('print_service.failed_with_code', { code }), { cause: error })
  }
  return error instanceof Error
    ? error
    : new Error($t('print_service.failed'))
}
