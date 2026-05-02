import { defineStore } from 'pinia'
import { computed, ref } from 'vue'

export interface NotificationItem {
  basicId: string
  title: string
  content?: string
  notificationType: number
  notificationStatus: number
  sendTime: string
  readTime?: string
  confirmTime?: string
  isGlobal?: boolean
  needConfirm?: boolean
  icon?: string
  link?: string
}

/**
 * 判断某条通知是否"需要关注"：
 * 未读 或 需要确认但尚未确认
 */
function needsAttention(n: NotificationItem): boolean {
  if (n.notificationStatus === 0)
    return true
  return Boolean(n.needConfirm) && !n.confirmTime
}

/**
 * 通知状态 store —— 仅维护头部铃铛需要的轻量数据，
 * 管理页面的 CRUD 仍走 notificationApi
 */
export const useNotificationStore = defineStore('notification', () => {
  const items = ref<NotificationItem[]>([])
  const loading = ref(false)

  // 需要关注的数量（未读 + 需确认未确认），用于铃铛徽章
  const unreadCount = computed(() =>
    items.value.filter(needsAttention).length,
  )

  // 站内信：全部通知；提及我：非全局通知（指定接收人的）
  const allItems = computed(() => items.value)
  const mentionedItems = computed(() =>
    items.value.filter(n => n.isGlobal === false),
  )

  const unreadAll = computed(() =>
    allItems.value.filter(needsAttention),
  )
  const unreadMentioned = computed(() =>
    mentionedItems.value.filter(needsAttention),
  )

  function setItems(list: NotificationItem[]) {
    items.value = list
  }

  function markItemRead(id: string) {
    const item = items.value.find(n => n.basicId === id)
    if (item) {
      item.notificationStatus = 1
      item.readTime = new Date().toISOString()
    }
  }

  function markItemConfirmed(id: string) {
    const item = items.value.find(n => n.basicId === id)
    if (item) {
      item.notificationStatus = 1
      item.readTime = item.readTime ?? new Date().toISOString()
      item.confirmTime = new Date().toISOString()
    }
  }

  function markAllRead() {
    items.value.forEach((n) => {
      if (n.notificationStatus === 0) {
        n.notificationStatus = 1
        n.readTime = new Date().toISOString()
      }
    })
  }

  function prependItem(item: NotificationItem) {
    items.value.unshift(item)
  }

  function $reset() {
    items.value = []
    loading.value = false
  }

  return {
    items,
    loading,
    unreadCount,
    allItems,
    mentionedItems,
    unreadAll,
    unreadMentioned,
    setItems,
    markItemRead,
    markItemConfirmed,
    markAllRead,
    prependItem,
    $reset,
  }
})
