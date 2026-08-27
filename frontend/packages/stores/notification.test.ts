/**
 * 通知 Store（notification）单元测试。
 * 职责边界：只覆盖头部铃铃需要的轻量状态——「需要关注」口径（未读 或 需确认未确认）、
 * 站内信/提及我两条列表的派生、标记已读/已确认/全部已读的时间戳写入、插入与 $reset。
 * 管理页 CRUD 走 notificationApi，不在本文件范围内。
 */
import type { NotificationItem } from './notification'
import { createPinia, setActivePinia } from 'pinia'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { NotificationStatus, NotificationType } from '~/types/enums'
import { useNotificationStore } from './notification'

function makeItem(overrides?: Partial<NotificationItem>): NotificationItem {
  return {
    basicId: 'n-1',
    title: '标题',
    notificationType: NotificationType.System,
    notificationStatus: NotificationStatus.Read,
    sendTime: '2024-01-01T00:00:00.000Z',
    ...overrides,
  }
}

beforeEach(() => {
  setActivePinia(createPinia())
})

afterEach(() => {
  vi.useRealTimers()
})

describe('初始状态', () => {
  it('初始无通知且不在加载中，徽章计数为 0', () => {
    const store = useNotificationStore()

    expect(store.items).toEqual([])
    expect(store.loading).toBe(false)
    expect(store.unreadCount).toBe(0)
  })
})

describe('「需要关注」口径 = 未读 或 需确认但未确认', () => {
  it('未读通知计入徽章', () => {
    const store = useNotificationStore()

    store.setItems([makeItem({ notificationStatus: NotificationStatus.Unread })])

    expect(store.unreadCount).toBe(1)
  })

  it('已读但 needConfirm 且无 confirmTime 仍计入徽章', () => {
    const store = useNotificationStore()

    store.setItems([makeItem({ needConfirm: true })])

    expect(store.unreadCount).toBe(1)
  })

  it('已读且已确认不计入徽章', () => {
    const store = useNotificationStore()

    store.setItems([makeItem({ needConfirm: true, confirmTime: '2024-01-02T00:00:00.000Z' })])

    expect(store.unreadCount).toBe(0)
  })

  it('已读、不需要确认的通知不计入徽章', () => {
    const store = useNotificationStore()

    store.setItems([makeItem()])

    expect(store.unreadCount).toBe(0)
  })

  it('已删除状态不等于未读，只有 needConfirm 才会让它继续计数', () => {
    const store = useNotificationStore()

    store.setItems([
      makeItem({ basicId: 'a', notificationStatus: NotificationStatus.Deleted }),
      makeItem({ basicId: 'b', notificationStatus: NotificationStatus.Deleted, needConfirm: true }),
    ])

    expect(store.unreadCount).toBe(1)
  })

  it('needConfirm 为 false 时即使无 confirmTime 也不计数', () => {
    const store = useNotificationStore()

    store.setItems([makeItem({ needConfirm: false })])

    expect(store.unreadCount).toBe(0)
  })

  it('多条混合时徽章为需要关注条目的总数', () => {
    const store = useNotificationStore()

    store.setItems([
      makeItem({ basicId: 'a', notificationStatus: NotificationStatus.Unread }),
      makeItem({ basicId: 'b', needConfirm: true }),
      makeItem({ basicId: 'c' }),
    ])

    expect(store.unreadCount).toBe(2)
  })
})

describe('站内信与「提及我」的切分', () => {
  it('allItems 就是全部通知（含全局）', () => {
    const store = useNotificationStore()
    const list = [makeItem({ basicId: 'a', isGlobal: true }), makeItem({ basicId: 'b', isGlobal: false })]

    store.setItems(list)

    expect(store.allItems.map(n => n.basicId)).toEqual(['a', 'b'])
  })

  it('mentionedItems 只收 isGlobal 显式为 false 的通知', () => {
    const store = useNotificationStore()

    store.setItems([
      makeItem({ basicId: 'global', isGlobal: true }),
      makeItem({ basicId: 'mine', isGlobal: false }),
    ])

    expect(store.mentionedItems.map(n => n.basicId)).toEqual(['mine'])
  })

  it('isGlobal 缺省（undefined）的通知不算「提及我」', () => {
    const store = useNotificationStore()

    store.setItems([makeItem({ basicId: 'unknown' })])

    expect(store.mentionedItems).toEqual([])
  })

  it('unreadAll / unreadMentioned 各自按需要关注口径二次过滤', () => {
    const store = useNotificationStore()

    store.setItems([
      makeItem({ basicId: 'g-unread', isGlobal: true, notificationStatus: NotificationStatus.Unread }),
      makeItem({ basicId: 'm-unread', isGlobal: false, notificationStatus: NotificationStatus.Unread }),
      makeItem({ basicId: 'm-read', isGlobal: false }),
    ])

    expect(store.unreadAll.map(n => n.basicId)).toEqual(['g-unread', 'm-unread'])
    expect(store.unreadMentioned.map(n => n.basicId)).toEqual(['m-unread'])
  })
})

describe('标记已读 / 已确认', () => {
  it('markItemRead 置为已读并写入 readTime', () => {
    vi.useFakeTimers()
    vi.setSystemTime(new Date('2024-05-01T08:00:00.000Z'))
    const store = useNotificationStore()
    store.setItems([makeItem({ notificationStatus: NotificationStatus.Unread })])

    store.markItemRead('n-1')

    expect(store.items[0]?.notificationStatus).toBe(NotificationStatus.Read)
    expect(store.items[0]?.readTime).toBe('2024-05-01T08:00:00.000Z')
    expect(store.unreadCount).toBe(0)
  })

  it('markItemRead 传不存在的 id 时静默无副作用', () => {
    const store = useNotificationStore()
    store.setItems([makeItem({ notificationStatus: NotificationStatus.Unread })])

    expect(() => store.markItemRead('不存在')).not.toThrow()
    expect(store.unreadCount).toBe(1)
  })

  it('markItemConfirmed 同时补齐已读状态与 confirmTime，徽章归零', () => {
    vi.useFakeTimers()
    vi.setSystemTime(new Date('2024-05-02T09:30:00.000Z'))
    const store = useNotificationStore()
    store.setItems([makeItem({ notificationStatus: NotificationStatus.Unread, needConfirm: true })])

    store.markItemConfirmed('n-1')

    expect(store.items[0]?.notificationStatus).toBe(NotificationStatus.Read)
    expect(store.items[0]?.readTime).toBe('2024-05-02T09:30:00.000Z')
    expect(store.items[0]?.confirmTime).toBe('2024-05-02T09:30:00.000Z')
    expect(store.unreadCount).toBe(0)
  })

  it('markItemConfirmed 保留已有 readTime，不覆盖首次阅读时间', () => {
    vi.useFakeTimers()
    vi.setSystemTime(new Date('2024-05-03T00:00:00.000Z'))
    const store = useNotificationStore()
    store.setItems([makeItem({ needConfirm: true, readTime: '2020-01-01T00:00:00.000Z' })])

    store.markItemConfirmed('n-1')

    expect(store.items[0]?.readTime).toBe('2020-01-01T00:00:00.000Z')
    expect(store.items[0]?.confirmTime).toBe('2024-05-03T00:00:00.000Z')
  })

  it('markAllRead 只改未读项，已读项的 readTime 不被刷新', () => {
    vi.useFakeTimers()
    vi.setSystemTime(new Date('2024-06-01T00:00:00.000Z'))
    const store = useNotificationStore()
    store.setItems([
      makeItem({ basicId: 'a', notificationStatus: NotificationStatus.Unread }),
      makeItem({ basicId: 'b', readTime: '2020-01-01T00:00:00.000Z' }),
    ])

    store.markAllRead()

    expect(store.items[0]?.readTime).toBe('2024-06-01T00:00:00.000Z')
    expect(store.items[1]?.readTime).toBe('2020-01-01T00:00:00.000Z')
  })

  it('markAllRead 不会顺手确认「需确认」的通知，徽章仍保留', () => {
    const store = useNotificationStore()
    store.setItems([makeItem({ notificationStatus: NotificationStatus.Unread, needConfirm: true })])

    store.markAllRead()

    expect(store.items[0]?.confirmTime).toBeUndefined()
    expect(store.unreadCount).toBe(1)
  })
})

describe('插入与重置', () => {
  it('prependItem 把新通知插到列表最前', () => {
    const store = useNotificationStore()
    store.setItems([makeItem({ basicId: 'old' })])

    store.prependItem(makeItem({ basicId: 'new' }))

    expect(store.items.map(n => n.basicId)).toEqual(['new', 'old'])
  })

  it('prependItem 不做去重，同 id 会重复出现（去重责任在调用方）', () => {
    const store = useNotificationStore()
    store.setItems([makeItem({ basicId: 'dup' })])

    store.prependItem(makeItem({ basicId: 'dup' }))

    expect(store.items).toHaveLength(2)
  })

  it('setItems 整体替换列表，旧条目立即消失', () => {
    const store = useNotificationStore()
    store.setItems([makeItem({ basicId: 'a' })])

    store.setItems([makeItem({ basicId: 'b' })])

    expect(store.items.map(n => n.basicId)).toEqual(['b'])
  })

  it('$reset 清空列表并复位 loading', () => {
    const store = useNotificationStore()
    store.setItems([makeItem({ notificationStatus: NotificationStatus.Unread })])
    store.loading = true

    store.$reset()

    expect(store.items).toEqual([])
    expect(store.loading).toBe(false)
    expect(store.unreadCount).toBe(0)
  })
})
