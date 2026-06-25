<script setup lang="ts">
import type { UserInboxItemDto } from '@/api'
import {
  NButton,
  NEmpty,
  NIcon,
  NSelect,
  NSkeleton,
  NSpace,
  NTag,
  useMessage,
} from 'naive-ui'
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { NotificationStatus, NotificationType, workbenchApi } from '@/api'
import { NotificationContent } from '~/components'
import { Icon } from '~/iconify'
import { useNotificationStore } from '~/stores'
import { formatDate } from '~/utils'

defineOptions({ name: 'WorkbenchInboxPage' })

type TagType = 'default' | 'error' | 'info' | 'success' | 'warning'

const router = useRouter()
const message = useMessage()
const notificationStore = useNotificationStore()
const { t } = useI18n()

const loading = ref(false)
const unreadOnly = ref(false)
const typeFilter = ref<NotificationType | undefined>(undefined)
const items = ref<UserInboxItemDto[]>([])

const notificationTypeOptions = computed(() => [
  { label: t('workbench.inbox.type_all'), value: undefined },
  { label: t('workbench.inbox.type_system'), value: NotificationType.System },
  { label: t('workbench.inbox.type_security'), value: NotificationType.Security },
  { label: t('workbench.inbox.type_business'), value: NotificationType.Business },
  { label: t('workbench.inbox.type_todo'), value: NotificationType.Todo },
  { label: t('workbench.inbox.type_emergency'), value: NotificationType.Emergency },
])

const scopeOptions = computed(() => [
  { label: t('workbench.inbox.scope_all'), value: 'all' },
  { label: t('workbench.inbox.scope_pending'), value: 'pending' },
])

function changeScope(value: string | number) {
  unreadOnly.value = value === 'pending'
  loadNotifications()
}

const visibleItems = computed(() => {
  let result = items.value
  if (unreadOnly.value) {
    result = result.filter(needsAttention)
  }
  if (typeFilter.value !== undefined) {
    result = result.filter(item => item.notificationType === typeFilter.value)
  }
  return result
})

const unreadCount = computed(() =>
  items.value.filter(item => item.notificationStatus === NotificationStatus.Unread).length,
)
const confirmCount = computed(() =>
  items.value.filter(item => item.needConfirm && !item.confirmTime).length,
)

function needsAttention(item: UserInboxItemDto) {
  return item.notificationStatus === NotificationStatus.Unread || (item.needConfirm && !item.confirmTime)
}

function notificationTypeLabel(type: NotificationType) {
  switch (type) {
    case NotificationType.Security:
      return t('workbench.inbox.type_security')
    case NotificationType.Business:
      return t('workbench.inbox.type_business')
    case NotificationType.Todo:
      return t('workbench.inbox.type_todo')
    case NotificationType.Emergency:
      return t('workbench.inbox.type_emergency')
    default:
      return t('workbench.inbox.type_system')
  }
}

function notificationTypeTag(type: NotificationType): TagType {
  switch (type) {
    case NotificationType.Emergency:
      return 'error'
    case NotificationType.Security:
      return 'warning'
    case NotificationType.System:
      return 'info'
    case NotificationType.Business:
      return 'success'
    default:
      return 'default'
  }
}

/** 按通知类型给图标着色（主题无关的语义色 + 同色低透明度底） */
const TYPE_COLOR: Record<string, string> = {
  [NotificationType.System]: '#3b82f6',
  [NotificationType.Security]: '#f59e0b',
  [NotificationType.Business]: '#22c55e',
  [NotificationType.Todo]: '#8b5cf6',
  [NotificationType.Emergency]: '#ef4444',
}

function iconStyle(item: UserInboxItemDto) {
  const color = TYPE_COLOR[item.notificationType] ?? '#64748b'
  return { color, background: `color-mix(in srgb, ${color} 14%, transparent)` }
}

function syncHeaderStore(list = items.value) {
  notificationStore.setItems(list.map(item => ({
    basicId: item.basicId,
    confirmTime: item.confirmTime ?? undefined,
    content: item.content ?? undefined,
    contentFormat: item.contentFormat,
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
    const list = await workbenchApi.inbox.list(unreadOnly.value)
    items.value = list
    syncHeaderStore(list)
  }
  catch {
    message.error(t('workbench.inbox.load_failed'))
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
    await workbenchApi.inbox.markRead(item.basicId)
    item.notificationStatus = NotificationStatus.Read
    item.readTime = item.readTime ?? new Date().toISOString()
    notificationStore.markItemRead(item.basicId)
  }
  catch {
    message.error(t('workbench.inbox.mark_read_failed'))
  }
}

async function handleConfirm(item: UserInboxItemDto) {
  try {
    await workbenchApi.inbox.confirm(item.basicId)
    const now = new Date().toISOString()
    item.notificationStatus = NotificationStatus.Read
    item.readTime = item.readTime ?? now
    item.confirmTime = item.confirmTime ?? now
    notificationStore.markItemConfirmed(item.basicId)
  }
  catch {
    message.error(t('workbench.inbox.confirm_failed'))
  }
}

async function handleMarkAllRead() {
  if (unreadCount.value === 0) {
    return
  }

  try {
    await workbenchApi.inbox.markAllRead()
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
    message.error(t('workbench.inbox.mark_all_read_failed'))
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
      <div class="inbox-toolbar-left">
        <NSelect
          v-model:value="typeFilter"
          :options="notificationTypeOptions"
          clearable
          :placeholder="t('workbench.inbox.type_placeholder')"
          size="small"
          style="width: 120px"
        />
        <NSelect
          :value="unreadOnly ? 'pending' : 'all'"
          :options="scopeOptions"
          size="small"
          style="width: 96px"
          @update:value="changeScope"
        />
      </div>

      <div class="inbox-toolbar-right">
        <NTag round size="small">
          {{ t('workbench.inbox.unread_count', { n: unreadCount }) }}
        </NTag>
        <NTag round size="small" :type="confirmCount > 0 ? 'warning' : 'default'">
          {{ t('workbench.inbox.confirm_count', { n: confirmCount }) }}
        </NTag>
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
          {{ t('workbench.inbox.mark_all_read') }}
        </NButton>
        <NButton :aria-label="t('workbench.inbox.refresh')" circle :loading="loading" size="small" @click="loadNotifications">
          <template #icon>
            <NIcon><Icon icon="lucide:refresh-cw" /></NIcon>
          </template>
        </NButton>
      </div>
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

    <NEmpty v-else-if="visibleItems.length === 0" class="inbox-empty" :description="t('workbench.inbox.empty')" />

    <div v-else class="inbox-list">
      <article
        v-for="item in visibleItems"
        :key="item.basicId"
        class="inbox-item"
        :class="{ 'inbox-item--attention': needsAttention(item) }"
      >
        <div class="inbox-item-icon" :style="iconStyle(item)">
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
                {{ t('workbench.inbox.unread') }}
              </NTag>
              <NTag v-if="item.needConfirm && !item.confirmTime" round size="small" type="error">
                {{ t('workbench.inbox.pending_confirm') }}
              </NTag>
            </div>
          </div>

          <NotificationContent
            v-if="item.content"
            class="inbox-item-content"
            :content="item.content"
            :format="item.contentFormat"
          />
          <p v-else class="inbox-item-content">
            {{ t('workbench.inbox.no_content') }}
          </p>

          <div class="inbox-item-foot">
            <span>{{ formatDate(item.sendTime) }}</span>
            <span v-if="item.readTime">{{ t('workbench.inbox.read_at', { time: formatDate(item.readTime) }) }}</span>
            <span v-if="item.confirmTime">{{ t('workbench.inbox.confirmed_at', { time: formatDate(item.confirmTime) }) }}</span>
          </div>
        </div>

        <NSpace class="inbox-item-actions" :size="4">
          <NButton
            v-if="item.notificationStatus === NotificationStatus.Unread"
            :aria-label="t('workbench.inbox.mark_read')"
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
            :aria-label="t('workbench.inbox.confirm')"
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
            :aria-label="t('workbench.inbox.open_link')"
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
  flex-shrink: 0;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  padding: 10px 12px;
  border: 1px solid var(--border-color);
  border-radius: var(--radius);
  background: var(--bg-surface);
}

/* 左侧：搜索筛选条件 */
.inbox-toolbar-left {
  display: flex;
  align-items: center;
  gap: 8px;
}

/* 右侧：计数 + 操作 */
.inbox-toolbar-right {
  display: flex;
  align-items: center;
  gap: 8px;
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
  flex: 1;
  flex-direction: column;
  /* 行式列表：靠分隔线区分条目，不留间隙(同通知中心) */
  gap: 0;
  min-height: 0;
  overflow-y: auto;
}

/* 与通知中心(铃铛面板)一致：清爽行式 —— 无卡片边框/底色，分隔线 + 悬停高亮 */
.inbox-item {
  position: relative;
  display: grid;
  grid-template-columns: auto minmax(0, 1fr) auto;
  gap: 12px;
  align-items: start;
  padding: 14px 16px;
  border-bottom: 1px solid hsl(var(--border) / 50%);
  transition: background 0.15s ease;
}

.inbox-item:last-child {
  border-bottom: none;
}

.inbox-item:hover {
  background: hsl(var(--accent) / 60%);
}

/* 未读/待处理：仅轻微主色底，无卡片/左条 */
.inbox-item--attention {
  background: hsl(var(--primary) / 4%);
}

.inbox-item--attention:hover {
  background: hsl(var(--primary) / 8%);
}

.inbox-item-icon {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 38px;
  height: 38px;
  border-radius: 9px;
  flex-shrink: 0;
}

.inbox-item-main {
  min-width: 0;
}

.inbox-item-head {
  display: flex;
  align-items: center;
  gap: 8px;
}

.inbox-item-title {
  flex: 0 1 auto;
  min-width: 0;
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

  .inbox-toolbar-left,
  .inbox-toolbar-right {
    flex-wrap: wrap;
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
