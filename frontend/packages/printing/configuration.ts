/**
 * 打印包运行配置。
 * 职责：由应用启动层注入后端模板解析与数据源目录函数，打印服务与目录加载经这里取用。
 */
import type { PrintingConfiguration } from './types'

let configuration: PrintingConfiguration | null = null

/**
 * 配置打印包；应用启动时必须注入后端模板解析函数。
 * @param options 运行配置。
 * @throws resolveTemplate 缺失时抛出 TypeError。
 */
export function configurePrinting(options: PrintingConfiguration): void {
  if (!options || typeof options.resolveTemplate !== 'function')
    throw new TypeError('打印包必须配置 resolveTemplate。')
  configuration = { ...options }
}

/**
 * 返回打印运行配置；尚未配置时返回 null（供不强制要求配置的路径使用）。
 * @returns 配置或 null。
 */
export function tryGetPrintingConfiguration(): PrintingConfiguration | null {
  return configuration
}

/**
 * 返回打印运行配置。
 * @returns 已注入的配置。
 * @throws 尚未调用 configurePrinting。
 */
export function getPrintingConfiguration(): PrintingConfiguration {
  if (!configuration)
    throw new Error('打印能力尚未初始化，请先调用 configurePrinting。')
  return configuration
}
