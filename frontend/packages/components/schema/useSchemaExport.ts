import type { ListFieldSchema } from './types'
import { ref } from 'vue'
import { islandStart } from '~/composables/useDynamicIsland'
import { formatFieldText } from './renderer'

/** CSV 单元格转义：含逗号/引号/换行时包裹双引号并转义内部引号 */
function csvCell(value: string): string {
  return /[",\r\n]/.test(value) ? `"${value.replace(/"/g, '""')}"` : value
}

/**
 * 将行集合按导出字段序列化为 CSV 文本（带 UTF-8 BOM，便于 Excel 正确识别中文）。
 */
export function toCsv<TRow extends object>(fields: ListFieldSchema<TRow>[], rows: TRow[]): string {
  const header = fields.map(f => csvCell(f.title)).join(',')
  const body = rows
    .map(row => fields.map(f => csvCell(formatFieldText(f, row))).join(','))
    .join('\r\n')
  return `\uFEFF${header}${body ? `\r\n${body}` : ''}`
}

/** 触发浏览器下载文本文件 */
export function downloadText(filename: string, content: string, mime = 'text/csv;charset=utf-8'): void {
  const blob = new Blob([content], { type: mime })
  const url = URL.createObjectURL(blob)
  const anchor = document.createElement('a')
  anchor.href = url
  anchor.download = filename
  document.body.appendChild(anchor)
  anchor.click()
  anchor.remove()
  URL.revokeObjectURL(url)
}

/**
 * Schema 同步导出（当前筛选结果 → CSV）。
 * 字段、文件名、取数由 SchemaPage 提供（取数通常翻页拉全集，受安全上限约束）。
 */
export interface UseSchemaExportOptions<TRow extends object> {
  /** 导出字段（已按权限过滤） */
  fields: () => ListFieldSchema<TRow>[]
  /** 文件名（不含扩展名） */
  fileName: () => string
  /** 取导出行（通常为全部筛选结果） */
  fetchRows: () => Promise<TRow[]>
}

export function useSchemaExport<TRow extends object>(options: UseSchemaExportOptions<TRow>) {
  const exporting = ref(false)

  async function exportCsv(): Promise<void> {
    if (exporting.value) {
      return
    }
    exporting.value = true
    // 灵动岛过程提示：拉取为不确定态；完成给出「再次导出」操作，失败给出「重试」
    const task = islandStart('export', '正在导出数据…', { icon: 'lucide:download' })
    try {
      const rows = await options.fetchRows()
      if (rows.length === 0) {
        task.info('无可导出数据')
        return
      }
      const fileName = `${options.fileName()}.csv`
      downloadText(fileName, toCsv(options.fields(), rows))
      task.success(`已导出 ${rows.length} 条`, {
        detail: fileName,
        actions: [
          { key: 'again', label: '再次导出', icon: 'lucide:rotate-cw', handler: () => void exportCsv() },
        ],
      })
    }
    catch {
      task.error('导出失败', {
        actions: [
          { key: 'retry', label: '重试', icon: 'lucide:rotate-cw', tone: 'primary', handler: () => void exportCsv() },
        ],
      })
    }
    finally {
      exporting.value = false
    }
  }

  return { exporting, exportCsv }
}
