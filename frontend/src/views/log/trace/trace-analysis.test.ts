import type { TraceTimelineItemDto } from '@/api'
import { describe, expect, it } from 'vitest'
import { TraceLogType } from '@/api'
import { buildTraceFlow, buildTraceTimeBuckets, pickTraceBucketMinutes, traceModuleOf } from './trace-analysis'

function log(partial: Partial<TraceTimelineItemDto>): TraceTimelineItemDto {
  return { basicId: '1', logType: TraceLogType.Access, status: 'success', time: '2026-09-30T10:00:00', ...partial } as TraceTimelineItemDto
}

const labels = {
  logType: (type: TraceLogType) => `T-${type}`,
  result: (result: string) => `R-${result}`,
  otherModules: '其他模块',
  groupLogType: '日志类型',
  groupModule: '模块',
  groupResult: '结果',
}

describe('traceModuleOf', () => {
  it('接口路径取服务名，查询服务并入同一资源', () => {
    expect(traceModuleOf(log({ path: '/api/RoleQuery/RolePage' }))).toBe('Role')
    expect(traceModuleOf(log({ path: '/api/Role/BatchUpdateRoleParents?x=1' }))).toBe('Role')
  })

  it('完整地址只看路径；非 /api 路径取前两段', () => {
    expect(traceModuleOf(log({ path: 'http://localhost:9708/api/UserQuery/Get' }))).toBe('User')
    expect(traceModuleOf(log({ path: '/hubs/notification/negotiate' }))).toBe('hubs/notification')
  })

  it('数据变更取实体名，没有路径的其他日志没有模块', () => {
    expect(traceModuleOf(log({ logType: TraceLogType.Diff, title: 'Update SysUser' }))).toBe('SysUser')
    expect(traceModuleOf(log({ logType: TraceLogType.Login, title: 'Success' }))).toBeNull()
  })
})

describe('buildTraceFlow', () => {
  it('类型 → 模块 → 结果按条数合并；没有模块的从类型直接流到结果', () => {
    const flow = buildTraceFlow([
      log({ path: '/api/RoleQuery/RolePage' }),
      log({ path: '/api/Role/Update', status: 'error' }),
      log({ path: '/api/UserQuery/Get' }),
      log({ logType: TraceLogType.Login, status: 'success' }),
    ], labels)

    expect(flow.nodes.map(node => node.id)).toEqual([
      'type:Access',
      'type:Login',
      'module:Role',
      'module:User',
      'result:success',
      'result:error',
    ])
    expect(flow.links).toEqual(expect.arrayContaining([
      { source: 'type:Access', target: 'module:Role', value: 2 },
      { source: 'module:Role', target: 'result:success', value: 1 },
      { source: 'module:Role', target: 'result:error', value: 1 },
      { source: 'type:Login', target: 'result:success', value: 1 },
    ]))
    expect(flow.links).toHaveLength(6)
  })

  it('超出上限的模块并成「其他模块」', () => {
    const flow = buildTraceFlow([
      log({ path: '/api/A/x' }),
      log({ path: '/api/A/y' }),
      log({ path: '/api/B/x' }),
      log({ path: '/api/C/x' }),
    ], labels, 1)

    expect(flow.nodes.filter(node => node.group === '模块').map(node => node.name)).toEqual(['A', '其他模块'])
    expect(flow.links).toEqual(expect.arrayContaining([{ source: 'type:Access', target: 'module:*', value: 2 }]))
  })
})

describe('time buckets', () => {
  it('按跨度选最细且不超过上限的桶宽', () => {
    expect(pickTraceBucketMinutes(30 * 60_000)).toBe(1)
    expect(pickTraceBucketMinutes(3 * 60 * 60_000)).toBe(5)
    expect(pickTraceBucketMinutes(24 * 60 * 60_000)).toBe(30)
  })

  it('桶从对齐的起点连续铺开，空档计 0，按结果分类计数', () => {
    const { bucketMinutes, buckets } = buildTraceTimeBuckets([
      log({ time: '2026-09-30T10:00:20', status: 'success' }),
      log({ time: '2026-09-30T10:00:50', status: 'error' }),
      log({ time: '2026-09-30T10:03:10', status: 'warning' }),
    ])

    expect(bucketMinutes).toBe(1)
    expect(buckets.map(bucket => [bucket.start.getMinutes(), bucket.success, bucket.warning, bucket.error])).toEqual([
      [0, 1, 0, 1],
      [1, 0, 0, 0],
      [2, 0, 0, 0],
      [3, 0, 1, 0],
    ])
  })

  it('没有可用时间时返回空', () => {
    expect(buildTraceTimeBuckets([]).buckets).toEqual([])
  })
})
