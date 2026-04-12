<script lang="ts" setup>
import type { SysNotification } from '@/api/modules/notification'
import {
  NButton,
  NCard,
  NEmpty,
  NScrollbar,
  NSelect,
  NSpin,
  NTag,
  useMessage,
} from 'naive-ui'
import { computed, onMounted, ref } from 'vue'
import { userInboxApi } from '@/api'
import { NOTIFICATION_TYPE_OPTIONS } from '~/constants'
import { useNotificationStore, useUserStore } from '~/stores'
import { formatDate } from '~/utils'

defineOptions({ name: 'DashboardInbox' })

const message = useMessage()
const userStore = useUserStore()
const notificationStore = useNotificationStore()

const loading = ref(false)
const items = ref<SysNotification[]>([])
const typeFilter = ref<number | undefined>(undefined)

const TAG_TYPES: Array<'default' | 'error' | 'info' | 'success' | 'warning'> = ['info', 'success', 'warning', 'warning', 'error']
function getTypeTag(type: number) {
  const opt = NOTIFICATION_TYPE_OPTIONS.find(o => o.value === type)
  return { label: opt?.label ?? '通知', type: TAG_TYPES[type] ?? ('default' as const) }
}

const filteredItems = computed(() => {
  if (typeFilter.value == null) return items.value
  return items.value.filter(n => n.notificationType === typeFilter.value)
})

async function loadData() {
  const userId = userStore.userInfo?.basicId
  if (!userId) return
  loading.value = true
  try {
    items.value = await userInboxApi.list(
      String(userId), true, userStore.userInfo?.tenantId,
    )
  }
  catch {
    message.error('加载站内信失败')
  }
  finally {
    loading.value = false
  }
}

async function handleMarkRead(id: string) {
  const userId = userStore.userInfo?.basicId
  if (!userId) return
  try {
    await userInboxApi.markRead(id, String(userId), userStore.userInfo?.tenantId)
    const item = items.value.find(n => n.basicId === id)
    if (item) {
      item.notificationStatus = 1
      item.readTime = new Date().toISOString()
    }
    notificationStore.markItemRead(id)
  }
  catch {
    message.error('标记已读失败')
  }
}

async function handleConfirm(id: string) {
  const userId = userStore.userInfo?.basicId
  if (!userId) return
  try {
    await userInboxApi.confirm(id, String(userId), userStore.userInfo?.tenantId)
    const item = items.value.find(n => n.basicId === id)
    if (item) {
      item.notificationStatus = 1
      item.readTime = item.readTime ?? new Date().toISOString()
      item.confirmTime = new Date().toISOString()
    }
    notificationStore.markItemConfirmed(id)
  }
  catch {
    message.error('确认失败')
  }
}

async function handleMarkAllRead() {
  const userId = userStore.userInfo?.basicId
  if (!userId) return
  try {
    await userInboxApi.markAllRead(String(userId), userStore.userInfo?.tenantId)
    items.value.forEach((n) => {
      if (n.notificationStatus === 0) {
        n.notificationStatus = 1
        n.readTime = new Date().toISOString()
      }
    })
    notificationStore.markAllRead()
    message.success('已全部标记为已读')
  }
  catch {
    message.error('操作失败')
  }
}

function needsAttention(n: SysNotification) {
  if (n.notificationStatus === 0) return true
  return Boolean(n.needConfirm) && !n.confirmTime
}

const unreadCount = computed(() => items.value.filter(needsAttention).length)

onMounted(loadData)
</script>

<template>
  <div class="flex overflow-hidden flex-col gap-3 p-4 h-full">
    <NCard size="small" :bordered="false">
      <div class="flex items-center justify-between">
        <div class="flex items-center gap-3">
          <span class="text-lg font-semibold">站内信</span>
          <NTag v-if="unreadCount > 0" type="error" size="small" round>
            {{ unreadCount }} 条未读
          </NTag>
        </div>
        <div class="flex items-center gap-2">
          <NSelect
            v-model:value="typeFilter"
            :options="[{ label: '全部类型', value: undefined }, ...NOTIFICATION_TYPE_OPTIONS]"
            placeholder="筛选类型"
            clearable
            style="width: 140px"
            size="small"
          />
          <NButton size="small" @click="loadData">
            刷新
          </NButton>
          <NButton
            v-if="unreadCount > 0"
            size="small"
            type="primary"
            @click="handleMarkAllRead"
          >
            全部已读
          </NButton>
        </div>
      </div>
    </NCard>

    <NCard :bordered="false" class="flex-1 overflow-hidden" content-class="!p-0 h-full">
      <NSpin :show="loading" class="h-full">
        <NScrollbar class="h-full">
          <div v-if="filteredItems.length === 0 && !loading" class="flex items-center justify-center py-20">
            <NEmpty description="暂无站内信" />
          </div>
          <div
            v-for="item in filteredItems"
            :key="item.basicId"
            class="inbox-item"
            :class="{ 'inbox-item--unread': needsAttention(item) }"
          >
            <div class="inbox-item-dot" :class="{ 'inbox-item-dot--active': needsAttention(item) }" />
            <div class="inbox-item-body">
              <div class="inbox-item-header">
                <span class="inbox-item-title">{{ item.title }}</span>
                <NTag :type="getTypeTag(item.notificationType).type" size="small" :bordered="false" round>
                  {{ getTypeTag(item.notificationType).label }}
                </NTag>
                <NTag v-if="item.isGlobal === false" size="small" type="info" :bordered="false" round>
                  提及我
                </NTag>
              </div>
              <div v-if="item.content" class="inbox-item-content">
                {{ item.content }}
              </div>
              <div class="inbox-item-footer">
                <span class="inbox-item-time">{{ formatDate(item.sendTime) }}</span>
                <div class="flex items-center gap-2">
                  <NButton
                    v-if="item.needConfirm && !item.confirmTime"
                    size="tiny"
                    type="warning"
                    secondary
                    @click="handleConfirm(item.basicId)"
                  >
                    确认
                  </NButton>
                  <NButton
                    v-if="item.notificationStatus === 0 && !item.needConfirm"
                    size="tiny"
                    type="primary"
                    text
                    @click="handleMarkRead(item.basicId)"
                  >
                    标记已读
                  </NButton>
                  <NTag v-if="item.notificationStatus === 1 && (!item.needConfirm || item.confirmTime)" size="small" :bordered="false">
                    已读
                  </NTag>
                </div>
              </div>
            </div>
          </div>
        </NScrollbar>
      </NSpin>
    </NCard>
  </div>
</template>

<style scoped>
.inbox-item {
  display: flex;
  gap: 12px;
  padding: 14px 20px;
  border-bottom: 1px solid hsl(var(--border) / 50%);
  transition: background 0.15s ease;
}

.inbox-item:last-child {
  border-bottom: none;
}

.inbox-item:hover {
  background: hsl(var(--accent) / 50%);
}

.inbox-item--unread {
  background: hsl(var(--primary) / 4%);
}

.inbox-item-dot {
  flex-shrink: 0;
  width: 8px;
  height: 8px;
  margin-top: 8px;
  border-radius: 50%;
  background: transparent;
}

.inbox-item-dot--active {
  background: hsl(var(--primary));
}

.inbox-item-body {
  flex: 1;
  min-width: 0;
}

.inbox-item-header {
  display: flex;
  align-items: center;
  gap: 8px;
}

.inbox-item-title {
  font-size: 14px;
  font-weight: 500;
  color: hsl(var(--foreground));
}

.inbox-item-content {
  margin-top: 6px;
  font-size: 13px;
  color: hsl(var(--muted-foreground));
  line-height: 1.5;
}

.inbox-item-footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-top: 8px;
}

.inbox-item-time {
  font-size: 12px;
  color: hsl(var(--muted-foreground) / 70%);
}
</style>
