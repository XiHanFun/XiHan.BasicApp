import type { WorkbenchDashboardSummaryDto } from '@/api'
import { workbenchApi } from '@/api'

/**
 * 工作台汇总数据共享加载：统计卡与最近动态两个小组件同源，
 * 模块级短缓存合并并发请求，避免同屏重复打接口。
 */
const CACHE_TTL_MS = 15_000

let cached: { at: number, promise: Promise<WorkbenchDashboardSummaryDto> } | null = null

export function loadDashboardSummary(): Promise<WorkbenchDashboardSummaryDto> {
  const now = Date.now()
  if (cached && now - cached.at < CACHE_TTL_MS) {
    return cached.promise
  }
  const promise = workbenchApi.dashboard.summary().catch((error) => {
    // 失败不缓存，下次重试
    cached = null
    throw error
  })
  cached = { at: now, promise }
  return promise
}
