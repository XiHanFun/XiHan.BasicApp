import type { TraceItem } from './trace-analysis'
import type { TraceLogType } from '~/contracts'
import { describe, expect, it } from 'vitest'
import { buildTraceFlow, buildTraceTimeBuckets, pickTraceBucketMinutes, traceModuleOf } from './trace-analysis'

function log(partial: Partial<TraceItem>): TraceItem {
  return { id: '1', logType: 'Access', status: 'Success', occurredAt: '2026-09-30T10:00:00', title: 'roles.list', traceId: 't', ...partial } as TraceItem
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
  it('接口路径取资源段（去掉冒号后的动词），完整地址只看路径', () => {
    expect(traceModuleOf(log({ path: '/api/roles/1:grant' }))).toBe('roles')
    expect(traceModuleOf(log({ path: '/api/roles:list?x=1' }))).toBe('roles')
    expect(traceModuleOf(log({ path: 'http://localhost:9710/api/users/2' }))).toBe('users')
    expect(traceModuleOf(log({ path: '/hubs/notification/negotiate' }))).toBe('hubs/notification')
  })

  it('没有路径的访问与操作日志取操作 ID 的资源段，数据变更取实体名，其他没有模块', () => {
    expect(traceModuleOf(log({ title: 'tenants.create' }))).toBe('tenants')
    expect(traceModuleOf(log({ logType: 'Operation', title: 'roles.grant' }))).toBe('roles')
    expect(traceModuleOf(log({ logType: 'Diff', title: 'XiHan.BasicApp.Access.Domain.Entities.SysRole' }))).toBe('SysRole')
    expect(traceModuleOf(log({ logType: 'Login', title: 'Login' }))).toBeNull()
  })
})

describe('buildTraceFlow', () => {
  it('类型 → 模块 → 结果按条数合并；没有模块的从类型直接流到结果', () => {
    const flow = buildTraceFlow([
      log({ title: 'roles.list' }),
      log({ title: 'roles.update', status: 'Failure' }),
      log({ title: 'users.get' }),
      log({ logType: 'Login', title: 'Login' }),
    ], labels)

    expect(flow.nodes.map(node => node.id)).toEqual([
      'type:Access',
      'type:Login',
      'module:roles',
      'module:users',
      'result:Success',
      'result:Failure',
    ])
    expect(flow.links).toEqual(expect.arrayContaining([
      { source: 'type:Access', target: 'module:roles', value: 2 },
      { source: 'module:roles', target: 'result:Success', value: 1 },
      { source: 'module:roles', target: 'result:Failure', value: 1 },
      { source: 'type:Login', target: 'result:Success', value: 1 },
    ]))
    expect(flow.links).toHaveLength(6)
  })

  it('超出上限的模块并成「其他模块」', () => {
    const flow = buildTraceFlow([
      log({ title: 'a.x' }),
      log({ title: 'a.y' }),
      log({ title: 'b.x' }),
      log({ title: 'c.x' }),
    ], labels, 1)

    expect(flow.nodes.filter(node => node.group === '模块').map(node => node.name)).toEqual(['a', '其他模块'])
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
      log({ occurredAt: '2026-09-30T10:00:20', status: 'Success' }),
      log({ occurredAt: '2026-09-30T10:00:50', status: 'Failure' }),
      log({ occurredAt: '2026-09-30T10:03:10', status: 'Warning' }),
    ])

    expect(bucketMinutes).toBe(1)
    expect(buckets.map(bucket => [bucket.start.getMinutes(), bucket.Success, bucket.Warning, bucket.Failure])).toEqual([
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
