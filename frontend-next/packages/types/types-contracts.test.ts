/**
 * packages/types 的运行时契约面。
 *
 * 职责边界：本包绝大部分是纯类型，运行时只剩两组枚举（enums.ts 的通知/租户枚举、
 * contracts.ts 的查询算子与排序方向）。这里锁定它们与后端序列化口径的一致性、取值唯一性，
 * 以及「包入口不导出任何运行时值」这条分层约定——入口一旦泄漏运行时值，
 * 纯类型导入就会在打包产物里留下真实模块。
 */
import { describe, expect, it } from 'vitest'
import { QueryOperator, SortDirection } from './contracts'
import {
  NotificationContentFormat,
  NotificationPriority,
  NotificationStatus,
  NotificationType,
  TenantMemberType,
} from './enums'
import * as typesEntry from './index'

/** 取字符串枚举的成员名列表（字符串枚举无反向映射） */
function memberNames(target: Record<string, string>): string[] {
  return Object.keys(target)
}

/** 取数字枚举的成员名列表（跳过反向映射键） */
function numericMemberNames(target: Record<string, number | string>): string[] {
  return Object.keys(target).filter(key => Number.isNaN(Number(key)))
}

describe('字符串枚举与后端序列化口径', () => {
  const stringEnums = {
    NotificationContentFormat,
    NotificationPriority,
    NotificationStatus,
    NotificationType,
    TenantMemberType,
  }

  it('每个成员的取值都等于成员名，与 JsonStringEnumConverter 输出一致', () => {
    const mismatched: string[] = []
    for (const [enumName, target] of Object.entries(stringEnums)) {
      for (const [name, value] of Object.entries(target)) {
        if (name !== value) {
          mismatched.push(`${enumName}.${name} => ${value}`)
        }
      }
    }

    expect(mismatched).toStrictEqual([])
  })

  it('每个枚举内部取值互不重复', () => {
    for (const target of Object.values(stringEnums)) {
      const values = Object.values(target)
      expect(new Set(values).size).toBe(values.length)
    }
  })

  it('通知类型固定为五种，紧急独立于系统与业务', () => {
    expect(memberNames(NotificationType)).toStrictEqual(['System', 'Security', 'Business', 'Todo', 'Emergency'])
  })

  it('通知优先级由低到高共四档', () => {
    expect(memberNames(NotificationPriority)).toStrictEqual(['Low', 'Normal', 'High', 'Urgent'])
  })

  it('通知正文格式支持纯文本、Markdown 与 HTML 三种', () => {
    expect(memberNames(NotificationContentFormat)).toStrictEqual(['Text', 'Markdown', 'Html'])
  })

  it('通知状态包含已删除，读未读之外还有一个终态', () => {
    expect(memberNames(NotificationStatus)).toStrictEqual(['Unread', 'Read', 'Deleted'])
  })

  it('租户成员类型含平台管理员，供控制中心做标签配色判断', () => {
    expect(memberNames(TenantMemberType)).toStrictEqual([
      'Owner',
      'Admin',
      'Member',
      'External',
      'Guest',
      'Consultant',
      'PlatformAdmin',
    ])
  })
})

describe('查询算子编号契约', () => {
  it('算子取值互不重复', () => {
    const values = numericMemberNames(QueryOperator).map(name => QueryOperator[name as keyof typeof QueryOperator])

    expect(new Set(values).size).toBe(values.length)
  })

  it('按语义分段编号：比较 1000 段、字符串匹配 2000 段、集合 3000 段、区间 4000 段、空值 5000 段', () => {
    const segmentOf = (value: number) => Math.floor(value / 1000) * 1000
    const grouped: Record<number, string[]> = {}
    for (const name of numericMemberNames(QueryOperator)) {
      const value = QueryOperator[name as keyof typeof QueryOperator]
      const segment = segmentOf(value)
      grouped[segment] = [...(grouped[segment] ?? []), name]
    }

    expect(grouped[1000]).toStrictEqual([
      'Equal',
      'NotEqual',
      'GreaterThan',
      'GreaterThanOrEqual',
      'LessThan',
      'LessThanOrEqual',
    ])
    expect(grouped[2000]).toStrictEqual(['Contains', 'StartsWith', 'EndsWith'])
    expect(grouped[3000]).toStrictEqual(['In', 'NotIn'])
    expect(grouped[4000]).toStrictEqual(['Between'])
    expect(grouped[5000]).toStrictEqual(['IsNull', 'IsNotNull'])
  })

  it('数字枚举带反向映射，可由编号还原算子名（后端回传编号时用得上）', () => {
    expect(QueryOperator[1000]).toBe('Equal')
    expect(QueryOperator[2000]).toBe('Contains')
    expect(QueryOperator[5001]).toBe('IsNotNull')
  })

  it('排序方向只有升序与降序两个取值，且紧邻编号', () => {
    expect(numericMemberNames(SortDirection)).toStrictEqual(['Ascending', 'Descending'])
    expect(SortDirection.Descending - SortDirection.Ascending).toBe(1)
  })

  it('排序方向与查询算子不共用编号语义，各自独立成表', () => {
    expect(SortDirection.Ascending).toBe(1000)
    expect(QueryOperator.Equal).toBe(1000)
    expect(SortDirection).not.toBe(QueryOperator)
  })
})

describe('包入口的分层约定', () => {
  it('入口在运行时零导出：契约层只出类型，枚举等运行时值必须按文件精确导入', () => {
    const runtimeExports = Object.keys(typesEntry).filter(key => key !== 'default')

    expect(runtimeExports).toStrictEqual([])
  })

  it('入口不转出 contracts / enums，避免 `~/types` 拿到形状已分叉的旧类型', () => {
    expect(Object.hasOwn(typesEntry, 'QueryOperator')).toBe(false)
    expect(Object.hasOwn(typesEntry, 'NotificationType')).toBe(false)
  })
})
