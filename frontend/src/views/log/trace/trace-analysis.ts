import type { TraceTimelineItemDto } from '@/api'
import { TraceLogType } from '@/api'

/** 结果分类：以后端已折算的 item.status 为准（与卡片着色同一口径） */
export const TRACE_RESULTS = ['success', 'info', 'warning', 'error'] as const
export type TraceResult = typeof TRACE_RESULTS[number]

export function traceResultOf(item: TraceTimelineItemDto): TraceResult {
  switch (item.status) {
    case 'success':
    case 'warning':
    case 'error':
      return item.status
    default:
      return 'info'
  }
}

/** 路径只取 pathname：操作日志记的是完整请求地址，查询串与锚点不参与归类 */
function pathnameOf(raw?: string | null): string {
  const value = raw?.trim() ?? ''
  if (!/^https?:\/\//i.test(value)) {
    return value.split(/[?#]/)[0] ?? ''
  }
  return URL.canParse(value) ? new URL(value).pathname : ''
}

/**
 * 日志落在哪个模块：接口路径取服务名（查询服务与命令服务同属一个资源，Query 后缀并入），
 * 非 /api 的路径取前两段，数据变更取实体名；登录、权限变更等没有路径的返回 null。
 */
export function traceModuleOf(item: TraceTimelineItemDto): string | null {
  const segments = pathnameOf(item.path).split('/').filter(Boolean)
  if (segments.length > 0) {
    if (segments[0]!.toLowerCase() === 'api' && segments[1]) {
      return segments[1].replace(/Query$/, '') || segments[1]
    }
    return segments.slice(0, 2).join('/')
  }
  if (item.logType === TraceLogType.Diff && item.title) {
    const words = item.title.trim().split(/\s+/)
    return words.length > 1 ? words[words.length - 1]! : null
  }
  return null
}

export interface TraceFlowNode {
  id: string
  name: string
  group: string
}

export interface TraceFlowLink {
  source: string
  target: string
  value: number
}

export interface TraceFlowLabels {
  logType: (type: TraceLogType) => string
  result: (result: TraceResult) => string
  otherModules: string
  groupLogType: string
  groupModule: string
  groupResult: string
}

const OTHER_MODULE_ID = 'module:*'

/**
 * 流向：日志类型 → 模块 → 结果，流量为条数。没有模块的日志从类型直接流到结果。
 * 模块只留条数最多的前 maxModules 个，其余并成「其他模块」，免得一列几十个节点挤成一片。
 * 节点次序即展示次序：类型、模块按条数由多到少，结果按 成功 → 信息 → 警告 → 失败。
 */
export function buildTraceFlow(items: readonly TraceTimelineItemDto[], labels: TraceFlowLabels, maxModules = 10) {
  const moduleCounts = new Map<string, number>()
  for (const item of items) {
    const module = traceModuleOf(item)
    if (module) {
      moduleCounts.set(module, (moduleCounts.get(module) ?? 0) + 1)
    }
  }
  const keptModules = new Set(
    [...moduleCounts.entries()]
      .sort((a, b) => b[1] - a[1] || a[0].localeCompare(b[0]))
      .slice(0, maxModules)
      .map(([module]) => module),
  )

  const typeCounts = new Map<TraceLogType, number>()
  const resultSeen = new Set<TraceResult>()
  const linkCounts = new Map<string, TraceFlowLink>()
  function addLink(source: string, target: string) {
    const key = `${source}\u0000${target}`
    const link = linkCounts.get(key)
    if (link) {
      link.value++
    }
    else {
      linkCounts.set(key, { source, target, value: 1 })
    }
  }

  let otherModuleCount = 0
  for (const item of items) {
    const typeId = `type:${item.logType}`
    const result = traceResultOf(item)
    const resultId = `result:${result}`
    typeCounts.set(item.logType, (typeCounts.get(item.logType) ?? 0) + 1)
    resultSeen.add(result)

    const module = traceModuleOf(item)
    if (!module) {
      addLink(typeId, resultId)
      continue
    }

    let moduleId = `module:${module}`
    if (!keptModules.has(module)) {
      moduleId = OTHER_MODULE_ID
      otherModuleCount++
    }
    addLink(typeId, moduleId)
    addLink(moduleId, resultId)
  }

  const nodes: TraceFlowNode[] = [
    ...[...typeCounts.entries()]
      .sort((a, b) => b[1] - a[1])
      .map(([type]) => ({ id: `type:${type}`, name: labels.logType(type), group: labels.groupLogType })),
    ...[...keptModules].map(module => ({ id: `module:${module}`, name: module, group: labels.groupModule })),
    ...(otherModuleCount > 0 ? [{ id: OTHER_MODULE_ID, name: labels.otherModules, group: labels.groupModule }] : []),
    ...TRACE_RESULTS
      .filter(result => resultSeen.has(result))
      .map(result => ({ id: `result:${result}`, name: labels.result(result), group: labels.groupResult })),
  ]

  return { nodes, links: [...linkCounts.values()] }
}

/** 时间桶的候选宽度（分钟）：都是整分、整时或整天，刻度落在读者习惯的位置 */
const BUCKET_MINUTES = [1, 2, 5, 10, 15, 30, 60, 120, 180, 360, 720, 1440] as const

/** 选最细、又不超过 maxBuckets 个桶的宽度 */
export function pickTraceBucketMinutes(spanMs: number, maxBuckets = 60): number {
  for (const minutes of BUCKET_MINUTES) {
    if (Math.floor(Math.max(spanMs, 0) / (minutes * 60_000)) + 1 <= maxBuckets) {
      return minutes
    }
  }
  return BUCKET_MINUTES.at(-1)!
}

/** 按本地时间对齐到桶的起点：5 分钟桶落在 :00 :05 …，小时桶落在整点，天桶落在零点 */
function floorToBucket(time: number, minutes: number): Date {
  const date = new Date(time)
  date.setSeconds(0, 0)
  if (minutes < 60) {
    date.setMinutes(Math.floor(date.getMinutes() / minutes) * minutes)
  }
  else if (minutes < 1440) {
    const hours = minutes / 60
    date.setHours(Math.floor(date.getHours() / hours) * hours, 0)
  }
  else {
    date.setHours(0, 0)
  }
  return date
}

export type TraceTimeBucket = { start: Date, end: Date } & Record<TraceResult, number>

/**
 * 时间分布：从最早一条到最晚一条连续铺桶，没有日志的时段也占位（计 0），空档一眼可见。
 * 返回桶宽（分钟）与各桶按结果分类的条数。
 */
export function buildTraceTimeBuckets(items: readonly TraceTimelineItemDto[], maxBuckets = 60) {
  const times = items.map(item => new Date(item.time).getTime())
  const valid = times.filter(Number.isFinite)
  if (valid.length === 0) {
    return { bucketMinutes: 1, buckets: [] as TraceTimeBucket[] }
  }

  const min = Math.min(...valid)
  const max = Math.max(...valid)
  const bucketMinutes = pickTraceBucketMinutes(max - min, maxBuckets)
  const bucketMs = bucketMinutes * 60_000
  const origin = floorToBucket(min, bucketMinutes).getTime()
  const count = Math.floor((max - origin) / bucketMs) + 1

  const buckets: TraceTimeBucket[] = Array.from({ length: count }, (_, index) => ({
    start: new Date(origin + index * bucketMs),
    end: new Date(origin + (index + 1) * bucketMs),
    success: 0,
    info: 0,
    warning: 0,
    error: 0,
  }))
  items.forEach((item, index) => {
    const time = times[index]!
    if (Number.isFinite(time)) {
      buckets[Math.floor((time - origin) / bucketMs)]![traceResultOf(item)]++
    }
  })

  return { bucketMinutes, buckets }
}
