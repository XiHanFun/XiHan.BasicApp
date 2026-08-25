<script setup lang="ts">
import type { Tone } from '@xihan-ui/kernel'
import type { NotificationItem } from '~/stores'
import {
  XhBadge,
  XhButton,
  XhEmptyStateDescription,
  XhEmptyStateIcon,
  XhEmptyStateRoot,
  XhEmptyStateTitle,
  XhSpinner,
  XhTabsContent,
  XhTabsList,
  XhTabsRoot,
  XhTabsTrigger,
  XhTagLabel,
  XhTagRoot,
  XhTooltipArrow,
  XhTooltipContent,
  XhTooltipPositioner,
  XhTooltipRoot,
  XhTooltipTrigger,
} from '@xihan-ui/vue'
import { computed, nextTick, onBeforeUnmount, onMounted, reactive, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { NotificationContent } from '~/components'
import { NOTIFICATION_TYPE_OPTIONS } from '~/constants'
import { useEnumOptions } from '~/hooks'
import { Icon } from '~/iconify'
import { NotificationStatus, NotificationType } from '~/types/enums'

defineOptions({ name: 'NotificationPopover' })

const props = defineProps<{
  allItems: NotificationItem[]
  mentionedItems: NotificationItem[]
  unreadAll: NotificationItem[]
  unreadMentioned: NotificationItem[]
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

const { t } = useI18n()

// 通知类型标签：以后端枚举元数据为本地化单一事实源，切语言响应式重取，空回退静态常量
const notificationTypeOptions = useEnumOptions('NotificationType', NOTIFICATION_TYPE_OPTIONS)

const showPopover = ref(false)
const activeTab = ref('inbox')
const triggerRef = ref<HTMLButtonElement>()
const dropdownPos = reactive({ top: '0px', right: '0px' })

function updateDropdownPosition() {
  if (!triggerRef.value)
    return
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

function getTypeTag(type: NotificationType): Tone {
  switch (type) {
    case NotificationType.System:
      return 'info'
    case NotificationType.Security:
      return 'warning'
    case NotificationType.Business:
      return 'success'
    case NotificationType.Todo:
      return 'neutral'
    case NotificationType.Emergency:
      return 'danger'
    default:
      return 'neutral'
  }
}

function getTypeInfo(type: NotificationType) {
  const opt = notificationTypeOptions.value.find(o => o.value === type)
  return {
    label: opt?.label ?? t('header.notification.type_default'),
    type: getTypeTag(type),
  }
}

const inboxTabLabel = computed(() => {
  const count = props.unreadAll.length
  return count > 0
    ? t('header.notification.tab.inbox_count', { n: count })
    : t('header.notification.tab.inbox')
})

const mentionTabLabel = computed(() => {
  const count = props.unreadMentioned.length
  return count > 0
    ? t('header.notification.tab.mention_count', { n: count })
    : t('header.notification.tab.mention')
})

function formatTime(time: string) {
  if (!time)
    return ''
  const date = new Date(time)
  if (Number.isNaN(date.getTime()))
    return ''
  const now = new Date()
  const diff = now.getTime() - date.getTime()
  const minutes = Math.floor(diff / 60000)
  if (minutes < 1)
    return t('header.notification.time.just_now')
  if (minutes < 60)
    return t('header.notification.time.minutes_ago', { n: minutes })
  const hours = Math.floor(minutes / 60)
  if (hours < 24)
    return t('header.notification.time.hours_ago', { n: hours })
  const days = Math.floor(hours / 24)
  if (days < 7)
    return t('header.notification.time.days_ago', { n: days })
  return `${date.getMonth() + 1}/${date.getDate()}`
}

/** 判断是否需要关注：未读 或 需确认但未确认 */
function itemNeedsAttention(item: NotificationItem) {
  if (item.notificationStatus === NotificationStatus.Unread)
    return true
  return Boolean(item.needConfirm) && !item.confirmTime
}

function handleItemClick(item: NotificationItem) {
  if (item.notificationStatus === NotificationStatus.Unread) {
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
    <XhTooltipRoot>
      <XhTooltipTrigger
        ref="triggerRef"
        class="xihan-icon-btn notification-btn mr-1"
        @click="showPopover = !showPopover"
      >
        <!-- 数字、99+、「零则收起」与贴角定位都归组件库算 -->
        <XhBadge
          size="sm"
          tone="danger"
          :count="props.unreadCount"
          :label="t('header.notification.unread_label', { n: props.unreadCount })"
        >
          <Icon icon="lucide:bell" width="16" height="16" />
        </XhBadge>
      </XhTooltipTrigger>
      <XhTooltipPositioner>
        <XhTooltipContent>
          {{ t('header.notification.bell') }}
          <XhTooltipArrow />
        </XhTooltipContent>
      </XhTooltipPositioner>
    </XhTooltipRoot>

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
            <span class="notification-dropdown-title">{{ t('header.notification.title') }}</span>
            <div class="notification-dropdown-actions">
              <XhTooltipRoot>
                <XhTooltipTrigger class="notification-header-btn" @click="emit('refresh')">
                  <Icon icon="lucide:refresh-cw" width="14" height="14" />
                </XhTooltipTrigger>
                <XhTooltipPositioner>
                  <XhTooltipContent>
                    {{ t('header.notification.refresh') }}
                    <XhTooltipArrow />
                  </XhTooltipContent>
                </XhTooltipPositioner>
              </XhTooltipRoot>
              <XhButton
                v-if="unreadCount > 0"
                variant="ghost"
                size="sm"
                tone="brand"
                @click="emit('markAllRead')"
              >
                {{ t('header.notification.mark_all_read') }}
              </XhButton>
            </div>
          </div>

          <div class="notification-stage">
            <div v-if="props.loading" class="notification-loading">
              <XhSpinner :label="t('header.notification.title')" />
            </div>
            <XhTabsRoot v-model:value="activeTab" variant="line" size="sm" class="notification-tabs">
              <XhTabsList>
                <XhTabsTrigger value="inbox">
                  {{ inboxTabLabel }}
                </XhTabsTrigger>
                <XhTabsTrigger value="mention">
                  {{ mentionTabLabel }}
                </XhTabsTrigger>
              </XhTabsList>
              <XhTabsContent value="inbox">
                <div class="notification-scroll">
                  <div v-if="allItems.length === 0" class="notification-empty">
                    <XhEmptyStateRoot size="sm">
                      <XhEmptyStateIcon>
                        <Icon icon="lucide:inbox" width="24" />
                      </XhEmptyStateIcon>
                      <XhEmptyStateTitle>{{ t('common.empty') }}</XhEmptyStateTitle>
                      <XhEmptyStateDescription>{{ t('header.notification.empty.inbox') }}</XhEmptyStateDescription>
                    </XhEmptyStateRoot>
                  </div>
                  <div
                    v-for="item in allItems"
                    :key="item.basicId"
                    class="notification-item"
                    :class="{ 'notification-item--unread': itemNeedsAttention(item) }"
                    @click="handleItemClick(item)"
                  >
                    <div class="notification-item-dot" :class="{ 'notification-item-dot--active': itemNeedsAttention(item) }" />
                    <div class="notification-item-body">
                      <div class="notification-item-header">
                        <span class="notification-item-title">{{ item.title }}</span>
                        <XhTagRoot :tone="getTypeInfo(item.notificationType).type" size="sm" variant="subtle">
                          <XhTagLabel>
                            {{ getTypeInfo(item.notificationType).label }}
                          </XhTagLabel>
                        </XhTagRoot>
                      </div>
                      <div v-if="item.content" class="notification-item-content">
                        <NotificationContent :content="item.content" :format="item.contentFormat" />
                      </div>
                      <div class="notification-item-footer">
                        <span class="notification-item-time">{{ formatTime(item.sendTime) }}</span>
                        <XhButton
                          v-if="item.needConfirm && !item.confirmTime"
                          size="sm"
                          tone="warning"
                          variant="subtle"
                          @click.stop="emit('confirm', item.basicId)"
                        >
                          {{ t('header.notification.confirm') }}
                        </XhButton>
                      </div>
                    </div>
                  </div>
                </div>
              </XhTabsContent>

              <XhTabsContent value="mention">
                <div class="notification-scroll">
                  <div v-if="mentionedItems.length === 0" class="notification-empty">
                    <XhEmptyStateRoot size="sm">
                      <XhEmptyStateIcon>
                        <Icon icon="lucide:inbox" width="24" />
                      </XhEmptyStateIcon>
                      <XhEmptyStateTitle>{{ t('common.empty') }}</XhEmptyStateTitle>
                      <XhEmptyStateDescription>{{ t('header.notification.empty.mention') }}</XhEmptyStateDescription>
                    </XhEmptyStateRoot>
                  </div>
                  <div
                    v-for="item in mentionedItems"
                    :key="item.basicId"
                    class="notification-item"
                    :class="{ 'notification-item--unread': itemNeedsAttention(item) }"
                    @click="handleItemClick(item)"
                  >
                    <div class="notification-item-dot" :class="{ 'notification-item-dot--active': itemNeedsAttention(item) }" />
                    <div class="notification-item-body">
                      <div class="notification-item-header">
                        <span class="notification-item-title">{{ item.title }}</span>
                        <XhTagRoot :tone="getTypeInfo(item.notificationType).type" size="sm" variant="subtle">
                          <XhTagLabel>
                            {{ getTypeInfo(item.notificationType).label }}
                          </XhTagLabel>
                        </XhTagRoot>
                      </div>
                      <div v-if="item.content" class="notification-item-content">
                        <NotificationContent :content="item.content" :format="item.contentFormat" />
                      </div>
                      <div class="notification-item-footer">
                        <span class="notification-item-time">{{ formatTime(item.sendTime) }}</span>
                        <XhButton
                          v-if="item.needConfirm && !item.confirmTime"
                          size="sm"
                          tone="warning"
                          variant="subtle"
                          @click.stop="emit('confirm', item.basicId)"
                        >
                          {{ t('header.notification.confirm') }}
                        </XhButton>
                      </div>
                    </div>
                  </div>
                </div>
              </XhTabsContent>
            </XhTabsRoot>
          </div>

          <div class="notification-dropdown-footer">
            <XhButton variant="ghost" tone="brand" size="sm" @click="emit('viewAll'); showPopover = false">
              {{ t('header.notification.view_all') }}
            </XhButton>
          </div>
        </div>
      </Transition>
    </Teleport>
  </div>
</template>

<style scoped>
/* 加载态：在内容之上叠一层居中旋转标记 */
.notification-stage {
  position: relative;
}

.notification-loading {
  position: absolute;
  inset: 0;
  z-index: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  background: var(--xh-bg-surface);
  opacity: 0.7;
}

/* 列表定高内部滚动 */
.notification-scroll {
  max-block-size: 360px;
  overflow-y: auto;
}

.notification-popover-wrapper {
  position: relative;
  display: inline-flex;
  align-items: center;
}

.notification-overlay {
  position: fixed;
  inset: 0;
  z-index: 1999;
}

.notification-dropdown {
  position: fixed;
  z-index: 2000;
  width: min(420px, calc(100vw - 24px));
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
  padding: 12px 16px 6px;
}

.notification-dropdown-title {
  font-size: 14px;
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

.notification-tabs :deep([data-scope='tabs'][data-part='list']) {
  padding: 0 12px;
}

.notification-empty {
  padding: 32px 0;
}

.notification-item {
  display: flex;
  gap: 10px;
  align-items: flex-start;
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
  align-items: flex-start;
  justify-content: space-between;
  gap: 8px;
}

.notification-item-title {
  flex: 1;
  min-width: 0;
  font-size: 13px;
  font-weight: 500;
  color: hsl(var(--foreground));
  line-height: 1.45;
  white-space: normal;
  word-break: break-word;
}

.notification-item-header :deep([data-scope='badge']) {
  flex-shrink: 0;
}

.notification-item-content {
  margin-top: 4px;
  max-height: 96px;
  overflow: hidden;
  font-size: 12px;
  color: hsl(var(--muted-foreground));
  line-height: 1.5;
  white-space: pre-wrap;
  word-break: break-word;
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
  padding: 8px;
  background: hsl(var(--muted) / 25%);
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
