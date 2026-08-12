/**
 * 后端数据源目录加载。
 * 职责：经注入的 listDataSources 拉取后端注册表并并入本地数据源注册表，
 * 使数据源目录以后端为单一事实源；设计与打印路径在使用前确保目录已加载。
 */
import type { PrintSampleInputType, PrintTableColumn, RemotePrintDataSourceDto } from './types'
import { getPrintDataSource, registerPrintDataSource } from './data-source-registry'
import { tryGetPrintingConfiguration } from './hiprint-adapter'

let loadPromise: Promise<void> | null = null

/**
 * 确保后端数据源目录已加载并注册（幂等；并发调用共享同一次拉取）。
 * @returns 完成信号；未注入 listDataSources 时直接完成，拉取失败时抛出且下次调用重试。
 */
export function ensureRemotePrintDataSourcesLoaded(): Promise<void> {
  loadPromise ??= loadRemoteSources().catch((error) => {
    // 失败不缓存：网络恢复后下一次使用重新拉取
    loadPromise = null
    throw error
  })
  return loadPromise
}

/** 拉取目录并注册尚未注册的数据源（本地已注册的编码保持原定义）。 */
async function loadRemoteSources(): Promise<void> {
  const listDataSources = tryGetPrintingConfiguration()?.listDataSources
  if (!listDataSources)
    return
  const sources = await listDataSources()
  for (const source of sources) {
    if (getPrintDataSource(source.code))
      continue
    registerPrintDataSource(toDefinition(source))
  }
}

/** 把后端目录项转换为本地注册表定义（静态样例 JSON 化为样例工厂）。 */
function toDefinition(source: RemotePrintDataSourceDto) {
  const sample = JSON.parse(source.sampleDataJson) as Record<string, unknown>
  return {
    code: source.code,
    name: source.name,
    fields: source.fields.map(field => ({
      key: field.key,
      label: field.label,
      kind: field.kind,
      columns: field.columns?.map((column): PrintTableColumn => ({
        field: column.field,
        title: column.title,
        width: column.width ?? undefined,
        inputType: (column.inputType ?? undefined) as PrintSampleInputType | undefined,
      })),
      inputType: (field.inputType ?? undefined) as PrintSampleInputType | undefined,
      placeholder: field.placeholder ?? undefined,
    })),
    createSampleData: () => structuredClone(sample),
  }
}
