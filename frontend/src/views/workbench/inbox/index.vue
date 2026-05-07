<script setup lang="ts">
import type { UserInboxItemDto } from '@/api'
import {
  NButton,
  NEmpty,
  NIcon,
  NSkeleton,
  NSpace,
  NSwitch,
  NTag,
  useMessage,
} from 'naive-ui'
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { NotificationStatus, NotificationType, userInboxApi } from '@/api'
import { Icon } from '~/iconify'
import { useNotificationStore } from '~/stores'
import { formatDate } from '~/utils'

defineOptions({ name: 'WorkbenchInboxPage' })

type TagType = 'default' | 'error' | 'info' | 'success' | 'warning'

const router = useRouter()
const message = useMessage()
const notificationStore = useNotificationStore()

const loading = ref(false)
const unreadOnly = ref(false)
const items = ref<UserInboxItemDto[]>([])

const visibleItems = computed(() => {
  if (!unreadOnly.value) {
    return items.value
  }

  return items.value.filter(needsAttention)
})

const unreadCount = computed(() =>
  items.value.filter(item => item.notificationStatus === NotificationStatus.Unread).length,
)
const confirmCount = computed(() =>
  items.value.filter(item => item.needConfirm && !item.confirmTime).length,
)
const attentionCount = computed(() => visibleItems.value.filter(needsAttention).length)

function needsAttention(item: UserInboxItemDto) {
  return item.notificationStatus === NotificationStatus.Unread || (item.needConfirm && !item.confirmTime)
}

function notificationTypeLabel(type: NotificationType) {
  switch (type) {
    case NotificationType.User:
      return '用户'
    case NotificationType.Announcement:
      return '公告'
    case NotificationType.Warning:
      return '警告'
    case NotificationType.Error:
      return '错误'
    default:
      return '系统'
  }
}

function notificationTypeTag(type: NotificationType): TagType {
  switch (type) {
    case NotificationType.Error:
      return 'error'
    case NotificationType.Warning:
      return 'warning'
    case NotificationType.Announcement:
      return 'info'
    case NotificationType.User:
      return 'success'
    default:
      return 'default'
  }
}

function syncHeaderStore(list = items.value) {
  notificationStore.setItems(list.map(item => ({
    basicId: item.basicId,
    confirmTime: item.confirmTime ?? undefined,
    content: item.content ?? undefined,
    icon: item.icon ?? undefined,
    isGlobal: item.isGlobal,
    link: item.link ?? undefined,
    needConfirm: item.needConfirm,
    notificationStatus: item.notificationStatus,
    notificationType: item.notificationType,
    readTime: item.readTime ?? undefined,
    sendTime: item.sendTime,
    title: item.title,
  })))
}

async function loadNotifications() {
  loading.value = true
  try {
    const list = await userInboxApi.list(unreadOnly.value)
    items.value = list
    syncHeaderStore(list)
  }
  catch {
    message.error('加载站内信失败')
  }
  finally {
    loading.value = false
  }
}

async function handleMarkRead(item: UserInboxItemDto) {
  if (item.notificationStatus !== NotificationStatus.Unread) {
    return
  }

  try {
    await userInboxApi.markRead(item.basicId)
    item.notificationStatus = NotificationStatus.Read
    item.readTime = item.readTime ?? new Date().toISOString()
    notificationStore.markItemRead(item.basicId)
  }
  catch {
    message.error('标记已读失败')
  }
}

async function handleConfirm(item: UserInboxItemDto) {
  try {
    await userInboxApi.confirm(item.basicId)
    const now = new Date().toISOString()
    item.notificationStatus = NotificationStatus.Read
    item.readTime = item.readTime ?? now
    item.confirmTime = item.confirmTime ?? now
    notificationStore.markItemConfirmed(item.basicId)
  }
  catch {
    message.error('确认通知失败')
  }
}

async function handleMarkAllRead() {
  if (unreadCount.value === 0) {
    return
  }

  try {
    await userInboxApi.markAllRead()
    const now = new Date().toISOString()
    items.value.forEach((item) => {
      if (item.notificationStatus === NotificationStatus.Unread) {
        item.notificationStatus = NotificationStatus.Read
        item.readTime = item.readTime ?? now
      }
    })
    notificationStore.markAllRead()
  }
  catch {
    message.error('全部标记已读失败')
  }
}

function handleOpenLink(item: UserInboxItemDto) {
  if (!item.link) {
    return
  }

  if (item.link.startsWith('/')) {
    router.push(item.link)
    return
  }

  window.open(item.link, '_blank', 'noopener,noreferrer')
}

onMounted(loadNotifications)
</script>

<template>
  <div class="inbox-page">
    <div class="inbox-toolbar">
      <div class="inbox-title">
        <span class="inbox-title-icon">
          <Icon icon="lucide:inbox" width="18" />
        </span>
        <span>站内信</span>
        <NTag :type="attentionCount > 0 ? 'warning' : 'success'" round size="small">
          {{ attentionCount }} 待处理
        </NTag>
      </div>

      <NSpace align="center" :size="8">
        <NTag round size="small">
          未读 {{ unreadCount }}
        </NTag>
        <NTag round size="small" :type="confirmCount > 0 ? 'warning' : 'default'">
          待确认 {{ confirmCount }}
        </NTag>
        <NSwitch
          v-model:value="unreadOnly"
          size="small"
          @update:value="loadNotifications"
        >
          <template #checked>
            待处理
          </template>
          <template #unchecked>
            全部
          </template>
        </NSwitch>
        <NButton
          :disabled="unreadCount === 0"
          :loading="loading"
          size="small"
          secondary
          type="primary"
          @click="handleMarkAllRead"
        >
          <template #icon>
            <NIcon><Icon icon="lucide:check-check" /></NIcon>
          </template>
          全部已读
        </NButton>
        <NButton aria-label="刷新" circle :loading="loading" size="small" @click="loadNotifications">
          <template #icon>
            <NIcon><Icon icon="lucide:refresh-cw" /></NIcon>
          </template>
        </NButton>
      </NSpace>
    </div>

    <div v-if="loading && items.length === 0" class="inbox-skeleton">
      <div v-for="index in 5" :key="index" class="inbox-skeleton-row">
        <NSkeleton circle :height="38" :width="38" />
        <div class="inbox-skeleton-body">
          <NSkeleton text style="width: 35%" />
          <NSkeleton text :repeat="2" />
        </div>
      </div>
    </div>

    <NEmpty v-else-if="visibleItems.length === 0" class="inbox-empty" description="暂无通知" />

    <div v-else class="inbox-list">
      <article
        v-for="item in visibleItems"
        :key="item.basicId"
        class="inbox-item"
        :class="{ 'inbox-item--attention': needsAttention(item) }"
      >
        <div class="inbox-item-icon">
          <Icon :icon="item.icon || 'lucide:bell'" width="18" />
        </div>

        <div class="inbox-item-main">
          <div class="inbox-item-head">
            <div class="inbox-item-title">
              {{ item.title }}
            </div>
            <div class="inbox-item-tags">
              <NTag :type="notificationTypeTag(item.notificationType)" round size="small">
                {{ notificationTypeLabel(item.notificationType) }}
              </NTag>
              <NTag v-if="item.notificationStatus === NotificationStatus.Unread" round size="small" type="warning">
                未读
              </NTag>
              <NTag v-if="item.needConfirm && !item.confirmTime" round size="small" type="error">
                待确认
              </NTag>
            </div>
          </div>

          <p class="inbox-item-content">
            {{ item.content || '无正文' }}
          </p>

          <div class="inbox-item-foot">
            <span>{{ formatDate(item.sendTime) }}</span>
            <span v-if="item.readTime">已读 {{ formatDate(item.readTime) }}</span>
            <span v-if="item.confirmTime">已确认 {{ formatDate(item.confirmTime) }}</span>
          </div>
        </div>

        <NSpace class="inbox-item-actions" :size="4">
          <NButton
            v-if="item.notificationStatus === NotificationStatus.Unread"
            aria-label="标记已读"
            circle
            quaternary
            size="small"
            type="primary"
            @click="handleMarkRead(item)"
          >
            <template #icon>
              <NIcon><Icon icon="lucide:check" /></NIcon>
            </template>
          </NButton>
          <NButton
            v-if="item.needConfirm && !item.confirmTime"
            aria-label="确认"
            circle
            quaternary
            size="small"
            type="warning"
            @click="handleConfirm(item)"
          >
            <template #icon>
              <NIcon><Icon icon="lucide:badge-check" /></NIcon>
            </template>
          </NButton>
          <NButton
            v-if="item.link"
            aria-label="打开链接"
            circle
            quaternary
            size="small"
            @click="handleOpenLink(item)"
          >
            <template #icon>
              <NIcon><Icon icon="lucide:external-link" /></NIcon>
            </template>
          </NButton>
        </NSpace>
      </article>
    </div>
  </div>
</template>

<style scoped>
.inbox-page {
  display: flex;
  flex-direction: column;
  gap: 12px;
  padding: 12px;
  height: 100%;
  min-height: 0;
}

.inbox-toolbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  padding: 10px 12px;
  border: 1px solid var(--border-color);
  border-radius: var(--radius);
  background: var(--bg-surface);
}

.inbox-title {
  display: flex;
  align-items: center;
  gap: 8px;
  min-width: 0;
  font-size: 15px;
  font-weight: 600;
  color: var(--text-primary);
}

.inbox-title-icon {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 30px;
  height: 30px;
  border-radius: 8px;
  background: hsl(var(--primary) / 10%);
  color: hsl(var(--primary));
}

.inbox-skeleton {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.inbox-skeleton-row {
  display: flex;
  gap: 10px;
  padding: 14px;
  border: 1px solid var(--border-color);
  border-radius: var(--radius);
  background: var(--bg-surface);
}

.inbox-skeleton-body {
  flex: 1;
  min-width: 0;
}

.inbox-empty {
  flex: 1;
  justify-content: center;
}

.inbox-list {
  display: flex;
  flex-direction: column;
  gap: 8px;
  min-height: 0;
}

.inbox-item {
  display: grid;
  grid-template-columns: auto minmax(0, 1fr) auto;
  gap: 12px;
  align-items: start;
  padding: 14px;
  border: 1px solid var(--border-color);
  border-radius: var(--radius);
  background: var(--bg-surface);
  transition:
    border-color 0.16s ease,
    background 0.16s ease;
}

.inbox-item--attention {
  border-color: hsl(var(--primary) / 34%);
  background: hsl(var(--accent));
}

.inbox-item-icon {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 38px;
  height: 38px;
  border-radius: 8px;
  background: #eef6ff;
  color: #2563eb;
}

.inbox-item-main {
  min-width: 0;
}

.inbox-item-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 10px;
}

.inbox-item-title {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  font-size: 14px;
  font-weight: 600;
  color: var(--text-primary);
}

.inbox-item-tags {
  display: flex;
  align-items: center;
  gap: 5px;
  flex-shrink: 0;
}

.inbox-item-content {
  margin: 6px 0 0;
  color: var(--text-secondary);
  line-height: 1.55;
  white-space: pre-wrap;
  word-break: break-word;
}

.inbox-item-foot {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
  margin-top: 8px;
  font-size: 12px;
  color: var(--text-tertiary);
}

.inbox-item-actions {
  flex-wrap: nowrap;
}

@media (max-width: 720px) {
  .inbox-toolbar {
    align-items: flex-start;
    flex-direction: column;
  }

  .inbox-item {
    grid-template-columns: auto minmax(0, 1fr);
  }

  .inbox-item-actions {
    grid-column: 2;
  }

  .inbox-item-head {
    align-items: flex-start;
    flex-direction: column;
  }

  .inbox-item-title {
    white-space: normal;
  }
}
</style>
