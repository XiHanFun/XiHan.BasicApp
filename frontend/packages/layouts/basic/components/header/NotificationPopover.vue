<script setup lang="ts">
import type { NotificationItem } from '~/stores'
import { NBadge, NButton, NEmpty, NScrollbar, NSpin, NTabPane, NTabs, NTag, NTooltip } from 'naive-ui'
import { computed, nextTick, onBeforeUnmount, onMounted, reactive, ref, watch } from 'vue'
import { NOTIFICATION_TYPE_OPTIONS } from '~/constants'
import { Icon } from '~/iconify'

defineOptions({ name: 'NotificationPopover' })

const props = defineProps<{
  messages: NotificationItem[]
  announcements: NotificationItem[]
  unreadMessages: NotificationItem[]
  unreadAnnouncements: NotificationItem[]
  unreadCount: number
  loading: boolean
}>()

const emit = defineEmits<{
  markRead: [id: string]
  confirm: [id: string]
  markAllRead: []
  viewAll: []
  refresh: []
}>()

const showPopover = ref(false)
const activeTab = ref('message')
const triggerRef = ref<HTMLButtonElement>()
const dropdownPos = reactive({ top: '0px', right: '0px' })

function updateDropdownPosition() {
  if (!triggerRef.value) return
  const rect = triggerRef.value.getBoundingClientRect()
  dropdownPos.top = `${rect.bottom + 8}px`
  dropdownPos.right = `${Math.max(document.documentElement.clientWidth - rect.right - 40, 8)}px`
}

watch(showPopover, (val) => {
  if (val) {
    nextTick(updateDropdownPosition)
    window.addEventListener('resize', updateDropdownPosition)
    window.addEventListener('scroll', updateDropdownPosition, true)
  }
  else {
    window.removeEventListener('resize', updateDropdownPosition)
    window.removeEventListener('scroll', updateDropdownPosition, true)
  }
})

onMounted(() => {
  // 组件卸载时兜底清理
})
onBeforeUnmount(() => {
  window.removeEventListener('resize', updateDropdownPosition)
  window.removeEventListener('scroll', updateDropdownPosition, true)
})

// 从 business.ts 构建类型映射，单一数据源
const TAG_TYPE_LIST: Array<'default' | 'error' | 'info' | 'success' | 'warning'> = ['info', 'success', 'warning', 'warning', 'error']
function getTypeInfo(type: number) {
  const opt = NOTIFICATION_TYPE_OPTIONS.find(o => o.value === type)
  return {
    label: opt?.label ?? '通知',
    type: TAG_TYPE_LIST[type] ?? ('default' as const),
  }
}

const messageTabLabel = computed(() => {
  const count = props.unreadMessages.length
  return count > 0 ? `通知 (${count})` : '通知'
})

const announcementTabLabel = computed(() => {
  const count = props.unreadAnnouncements.length
  return count > 0 ? `公告 (${count})` : '公告'
})

function formatTime(time: string) {
  if (!time) return ''
  const date = new Date(time)
  if (Number.isNaN(date.getTime())) return ''
  const now = new Date()
  const diff = now.getTime() - date.getTime()
  const minutes = Math.floor(diff / 60000)
  if (minutes < 1) return '刚刚'
  if (minutes < 60) return `${minutes} 分钟前`
  const hours = Math.floor(minutes / 60)
  if (hours < 24) return `${hours} 小时前`
  const days = Math.floor(hours / 24)
  if (days < 7) return `${days} 天前`
  return `${date.getMonth() + 1}/${date.getDate()}`
}

function handleItemClick(item: NotificationItem) {
  if (item.notificationStatus === 0) {
    emit('markRead', item.basicId)
  }
}

function handleClickOutside() {
  showPopover.value = false
}
</script>

<template>
  <div class="notification-popover-wrapper" @click.stop>
    <!-- 铃铛触发按钮 -->
    <NTooltip>
      <template #trigger>
        <button
          ref="triggerRef"
          type="button"
          class="xihan-icon-btn mr-1"
          @click="showPopover = !showPopover"
        >
          <NBadge
            :value="props.unreadCount"
            :max="99"
            :show="props.unreadCount > 0"
            :offset="[-2, 2]"
          >
            <Icon icon="lucide:bell" width="16" height="16" />
          </NBadge>
        </button>
      </template>
      通知
    </NTooltip>

    <!-- 遮罩 + 下拉弹窗 Teleport 到 body，脱离 header 层叠上下文 -->
    <Teleport to="body">
      <div v-if="showPopover" class="notification-overlay" @click="handleClickOutside" />
      <Transition name="notification-slide">
        <div
          v-if="showPopover"
          class="notification-dropdown"
          :style="{ top: dropdownPos.top, right: dropdownPos.right }"
          @click.stop
        >
        <div class="notification-dropdown-header">
          <span class="notification-dropdown-title">通知中心</span>
          <div class="notification-dropdown-actions">
            <NTooltip>
              <template #trigger>
                <button type="button" class="notification-header-btn" @click="emit('refresh')">
                  <Icon icon="lucide:refresh-cw" width="14" height="14" />
                </button>
              </template>
              刷新
            </NTooltip>
            <NButton
              v-if="unreadCount > 0"
              text
              size="small"
              type="primary"
              @click="emit('markAllRead')"
            >
              全部已读
            </NButton>
          </div>
        </div>

        <NSpin :show="props.loading">
          <NTabs v-model:value="activeTab" type="line" size="small" class="notification-tabs">
            <NTabPane name="message" :tab="messageTabLabel">
              <NScrollbar style="max-height: 360px">
                <div v-if="messages.length === 0" class="notification-empty">
                  <NEmpty description="暂无通知" size="small" />
                </div>
                <div
                  v-for="item in messages"
                  :key="item.basicId"
                  class="notification-item"
                  :class="{ 'notification-item--unread': item.notificationStatus === 0 }"
                  @click="handleItemClick(item)"
                >
                  <div class="notification-item-dot" :class="{ 'notification-item-dot--active': item.notificationStatus === 0 }" />
                  <div class="notification-item-body">
                    <div class="notification-item-header">
                      <span class="notification-item-title">{{ item.title }}</span>
                      <NTag :type="getTypeInfo(item.notificationType).type" size="small" :bordered="false" round>
                        {{ getTypeInfo(item.notificationType).label }}
                      </NTag>
                    </div>
                    <div v-if="item.content" class="notification-item-content">
                      {{ item.content }}
                    </div>
                    <div class="notification-item-footer">
                      <span class="notification-item-time">{{ formatTime(item.sendTime) }}</span>
                      <NButton
                        v-if="item.needConfirm && item.notificationStatus === 0"
                        size="tiny"
                        type="warning"
                        secondary
                        @click.stop="emit('confirm', item.basicId)"
                      >
                        确认
                      </NButton>
                    </div>
                  </div>
                </div>
              </NScrollbar>
            </NTabPane>

            <NTabPane name="announcement" :tab="announcementTabLabel">
              <NScrollbar style="max-height: 360px">
                <div v-if="announcements.length === 0" class="notification-empty">
                  <NEmpty description="暂无公告" size="small" />
                </div>
                <div
                  v-for="item in announcements"
                  :key="item.basicId"
                  class="notification-item"
                  :class="{ 'notification-item--unread': item.notificationStatus === 0 }"
                  @click="handleItemClick(item)"
                >
                  <div class="notification-item-dot" :class="{ 'notification-item-dot--active': item.notificationStatus === 0 }" />
                  <div class="notification-item-body">
                    <div class="notification-item-header">
                      <span class="notification-item-title">{{ item.title }}</span>
                    </div>
                    <div v-if="item.content" class="notification-item-content">
                      {{ item.content }}
                    </div>
                    <div class="notification-item-footer">
                      <span class="notification-item-time">{{ formatTime(item.sendTime) }}</span>
                    </div>
                  </div>
                </div>
              </NScrollbar>
            </NTabPane>
          </NTabs>
        </NSpin>

        <div class="notification-dropdown-footer">
          <NButton text type="primary" size="small" @click="emit('viewAll'); showPopover = false">
            查看全部
          </NButton>
        </div>
      </div>
      </Transition>
    </Teleport>
  </div>
</template>

<style scoped>
.notification-popover-wrapper {
  position: relative;
  display: inline-flex;
  align-items: center;
}

.xihan-icon-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 32px;
  height: 32px;
  padding: 0;
  border: none;
  border-radius: 6px;
  background: transparent;
  color: hsl(var(--foreground) / 65%);
  cursor: pointer;
  outline: none;
  flex-shrink: 0;
  transition:
    background 0.15s ease,
    color 0.15s ease;
}

.xihan-icon-btn:hover {
  background: hsl(var(--accent));
  color: hsl(var(--foreground));
}

.notification-overlay {
  position: fixed;
  inset: 0;
  z-index: 1999;
}

.notification-dropdown {
  position: fixed;
  z-index: 2000;
  width: 380px;
  background: hsl(var(--card));
  border: 1px solid hsl(var(--border));
  border-radius: 12px;
  box-shadow:
    0 8px 30px hsl(var(--foreground) / 8%),
    0 2px 8px hsl(var(--foreground) / 4%);
  overflow: hidden;
}

.notification-dropdown-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 14px 16px 8px;
}

.notification-dropdown-title {
  font-size: 15px;
  font-weight: 600;
  color: hsl(var(--foreground));
}

.notification-dropdown-actions {
  display: flex;
  align-items: center;
  gap: 8px;
}

.notification-header-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 26px;
  height: 26px;
  padding: 0;
  border: none;
  border-radius: 6px;
  background: transparent;
  color: hsl(var(--muted-foreground));
  cursor: pointer;
  transition: all 0.15s ease;
}

.notification-header-btn:hover {
  background: hsl(var(--accent));
  color: hsl(var(--foreground));
}

.notification-tabs {
  padding: 0 4px;
}

.notification-tabs :deep(.n-tabs-nav) {
  padding: 0 12px;
}

.notification-empty {
  padding: 32px 0;
}

.notification-item {
  display: flex;
  gap: 10px;
  padding: 10px 16px;
  cursor: pointer;
  transition: background 0.15s ease;
  border-bottom: 1px solid hsl(var(--border) / 50%);
}

.notification-item:last-child {
  border-bottom: none;
}

.notification-item:hover {
  background: hsl(var(--accent) / 60%);
}

.notification-item--unread {
  background: hsl(var(--primary) / 4%);
}

.notification-item-dot {
  flex-shrink: 0;
  width: 8px;
  height: 8px;
  margin-top: 6px;
  border-radius: 50%;
  background: transparent;
}

.notification-item-dot--active {
  background: hsl(var(--primary));
}

.notification-item-body {
  flex: 1;
  min-width: 0;
}

.notification-item-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
}

.notification-item-title {
  font-size: 13px;
  font-weight: 500;
  color: hsl(var(--foreground));
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  flex: 1;
  min-width: 0;
}

.notification-item-content {
  margin-top: 4px;
  font-size: 12px;
  color: hsl(var(--muted-foreground));
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.notification-item-footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-top: 4px;
  gap: 8px;
}

.notification-item-time {
  font-size: 11px;
  color: hsl(var(--muted-foreground) / 70%);
}

.notification-dropdown-footer {
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 10px;
  border-top: 1px solid hsl(var(--border));
}

/* 进出场动画 */
.notification-slide-enter-active {
  transition: all 0.2s ease-out;
}

.notification-slide-leave-active {
  transition: all 0.15s ease-in;
}

.notification-slide-enter-from {
  opacity: 0;
  transform: translateY(-6px) scale(0.98);
}

.notification-slide-leave-to {
  opacity: 0;
  transform: translateY(-4px) scale(0.99);
}
</style>
