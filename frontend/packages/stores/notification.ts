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
  isGlobal?: boolean
  needConfirm?: boolean
  icon?: string
  link?: string
}

/**
 * 通知状态 store —— 仅维护头部铃铛需要的轻量数据，
 * 管理页面的 CRUD 仍走 notificationApi
 */
export const useNotificationStore = defineStore('notification', () => {
  const items = ref<NotificationItem[]>([])
  const loading = ref(false)

  const unreadCount = computed(() =>
    items.value.filter(n => n.notificationStatus === 0).length,
  )

  // 按类型拆分：通知(0/1/3/4) 与 公告(2)
  const messages = computed(() =>
    items.value.filter(n => n.notificationType !== 2),
  )
  const announcements = computed(() =>
    items.value.filter(n => n.notificationType === 2),
  )

  const unreadMessages = computed(() =>
    messages.value.filter(n => n.notificationStatus === 0),
  )
  const unreadAnnouncements = computed(() =>
    announcements.value.filter(n => n.notificationStatus === 0),
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

  function markAllRead() {
    items.value.forEach((n) => {
      if (n.notificationStatus === 0) {
        n.notificationStatus = 1
        n.readTime = new Date().toISOString()
      }
    })
  }

  /** 收到 SignalR 推送时追加到列表头部 */
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
    messages,
    announcements,
    unreadMessages,
    unreadAnnouncements,
    setItems,
    markItemRead,
    markAllRead,
    prependItem,
    $reset,
  }
})
