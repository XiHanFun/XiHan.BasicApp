<script setup lang="ts">
import type { InboxItem, InboxScope } from './inbox'
import type { NotificationType } from '~/contracts'
import type { SelectOption } from '~/types'
import type { NotificationContentFormat as ShellContentFormat } from '~/types/enums'
import { XhButton, XhButtonIndicator, XhButtonLabel, XhButtonPrefix, XhCardContent, XhCardRoot, XhEmptyStateDescription, XhEmptyStateIndicator, XhEmptyStateRoot, XhEmptyStateTitle, XhFieldControl, XhFieldLabel, XhFieldRoot, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { computed, onActivated, onDeactivated, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { Icon, NotificationContent, SchemaPagination, XIconButton, XSelect } from '~/components'
import { confirmDanger, toast } from '~/composables'
import { NotificationType as TYPES } from '~/contracts'
import { useNotificationStore } from '~/stores'
import { formatDate } from '~/utils'
import { errorText, notificationTypeLabel, notificationTypeTone } from '../../labels'
import {
  clampPage,
  confirmItem,
  deleteItem,
  INBOX_PAGE_SIZE,
  inboxCounts,
  isUnconfirmed,
  isUnread,
  loadInbox,
  loadPending,
  markAllRead,
  markRead,
  needsAttention,
  resolveLink,
  toHeaderItem,
  typeColor,
} from './inbox'

defineOptions({ name: 'NotificationInboxView' })

const router = useRouter()
const notificationStore = useNotificationStore()
const { t } = useI18n()

const loading = ref(false)
const scope = ref<InboxScope>('all')
const typeFilter = ref<NotificationType | null>(null)
const page = ref(1)
const items = ref<InboxItem[]>([])
const total = ref(0)
/** 最近的待处理条目：页头计数由它算，也拿它刷新顶栏通知中心 */
const pending = ref<InboxItem[]>([])
const counts = computed(() => inboxCounts(pending.value))

const typeOptions = computed<SelectOption[]>(() => [
  {
    label: t('notification.inbox.type_all'),
    value: '',
  },
  ...TYPES.map(type => ({
    label: notificationTypeLabel(t, type),
    value: type,
  })),
])

const scopeOptions = computed<SelectOption[]>(() => [
  {
    label: t('notification.inbox.scope_all'),
    value: 'all',
  },
  {
    label: t('notification.inbox.scope_pending'),
    value: 'pending',
  },
])

function changeType(value: unknown) {
  typeFilter.value = typeof value === 'string' && value
    ? value as NotificationType
    : null
  page.value = 1
  void refresh()
}

function changeScope(value: unknown) {
  scope.value = value === 'pending'
    ? 'pending'
    : 'all'
  page.value = 1
  void refresh()
}

function changePage(value: number) {
  page.value = value
  void refresh()
}

function iconStyle(item: InboxItem) {
  const color = typeColor(item.notificationType)
  return { color, background: `color-mix(in srgb, ${color} 14%, transparent)` }
}

async function loadList() {
  const result = await loadInbox({ type: typeFilter.value, scope: scope.value, page: page.value })
  const count = Number(result.total)
  // 删掉或读掉末页最后几条后页码越界：退回到最后一页再取
  const last = clampPage(page.value, count)
  if (result.items.length === 0 && last < page.value) {
    page.value = last
    return loadList()
  }
  items.value = result.items
  total.value = count
}

async function loadCounts() {
  pending.value = await loadPending()
  notificationStore.setItems(pending.value.map(toHeaderItem))
}

async function refresh() {
  loading.value = true
  try {
    await Promise.all([
      loadList(),
      loadCounts(),
    ])
  }
  catch (error) {
    toast.danger(errorText(error) || t('notification.inbox.load_failed'))
  }
  finally {
    loading.value = false
  }
}

async function handleMarkRead(item: InboxItem) {
  if (!isUnread(item))
    return
  try {
    await markRead(item.id)
    item.status = 'Read'
    item.readAt = item.readAt ?? new Date().toISOString()
    notificationStore.markItemRead(item.id)
    void refresh()
  }
  catch (error) {
    toast.danger(errorText(error) || t('notification.inbox.mark_read_failed'))
  }
}

async function handleConfirm(item: InboxItem) {
  try {
    await confirmItem(item.id)
    const now = new Date().toISOString()
    item.status = 'Read'
    item.readAt = item.readAt ?? now
    item.confirmedAt = item.confirmedAt ?? now
    notificationStore.markItemConfirmed(item.id)
    void refresh()
  }
  catch (error) {
    toast.danger(errorText(error) || t('notification.inbox.confirm_failed'))
  }
}

async function handleMarkAllRead() {
  if (counts.value.unread === 0)
    return
  try {
    await markAllRead()
    notificationStore.markAllRead()
    await refresh()
  }
  catch (error) {
    toast.danger(errorText(error) || t('notification.inbox.mark_all_read_failed'))
  }
}

/** 从本人收件箱删除一条（通知本身不受影响） */
function handleDelete(item: InboxItem) {
  void confirmDanger({
    title: t('notification.inbox.delete'),
    content: t('notification.inbox.delete_confirm', { title: item.title }),
    okText: t('common.actions.delete'),
    cancelText: t('common.actions.cancel'),
    onOk: async () => {
      try {
        await deleteItem(item.id)
        toast.success(t('notification.inbox.delete_success'))
        await refresh()
      }
      catch (error) {
        toast.danger(errorText(error) || t('notification.inbox.delete_failed'))
      }
    },
  })
}

function handleOpenLink(item: InboxItem) {
  const target = resolveLink(item.link)
  if (!target)
    return
  if (target.kind === 'route')
    void router.push(target.path)
  else
    window.open(target.url, '_blank', 'noopener,noreferrer')
}

// 页面缓存在标签里：切回来重取一次，计数与顶栏跟上别处的变化
let deactivated = false
onMounted(refresh)
onActivated(() => {
  if (deactivated) {
    deactivated = false
    void refresh()
  }
})
onDeactivated(() => {
  deactivated = true
})
</script>

<template>
  <div class="inbox-page">
    <!-- 筛选：与列表页的搜索栏同款（字段名在上、控件在下、同一标准宽），改了即生效，没有查询钮 -->
    <XhCardRoot class="inbox-card" variant="outline">
      <XhCardContent>
        <div class="inbox-search">
          <XhFieldRoot class="inbox-search__item">
            <XhFieldLabel class="inbox-search__label">
              {{ t('notification.inbox.type_placeholder') }}
            </XhFieldLabel>
            <XhFieldControl>
              <XSelect
                :value="typeFilter ?? ''"
                :options="typeOptions"
                clearable
                size="sm"
                :placeholder="t('notification.inbox.type_placeholder')"
                @update:value="changeType"
              />
            </XhFieldControl>
          </XhFieldRoot>
          <XhFieldRoot class="inbox-search__item">
            <XhFieldLabel class="inbox-search__label">
              {{ t('notification.inbox.scope_label') }}
            </XhFieldLabel>
            <XhFieldControl>
              <XSelect
                :value="scope"
                :options="scopeOptions"
                size="sm"
                @update:value="changeScope"
              />
            </XhFieldControl>
          </XhFieldRoot>
        </div>
      </XhCardContent>
    </XhCardRoot>

    <!-- 工具条：与列表页同一张卡——页面操作（全部已读）靠左，计数与刷新靠右 -->
    <XhCardRoot class="inbox-card inbox-card--toolbar" variant="outline">
      <XhCardContent>
        <div class="inbox-toolbar">
          <XhButton :disabled="counts.unread === 0" :loading="loading" size="sm" variant="subtle" tone="brand" @click="handleMarkAllRead">
            <XhButtonIndicator />
            <XhButtonPrefix><Icon icon="lucide:check-check" /></XhButtonPrefix>
            <XhButtonLabel>{{ t('notification.inbox.mark_all_read') }}</XhButtonLabel>
          </XhButton>
          <div class="inbox-toolbar__status">
            <XhTagRoot variant="subtle" size="sm">
              <XhTagLabel>{{ t('notification.inbox.unread_count', { n: counts.unread }) }}</XhTagLabel>
            </XhTagRoot>
            <XhTagRoot variant="subtle" size="sm" :tone="counts.confirm > 0 ? 'warning' : 'neutral'">
              <XhTagLabel>{{ t('notification.inbox.confirm_count', { n: counts.confirm }) }}</XhTagLabel>
            </XhTagRoot>
            <XIconButton icon="lucide:refresh-cw" :label="t('notification.inbox.refresh')" :loading="loading" @click="refresh" />
          </div>
        </div>
      </XhCardContent>
    </XhCardRoot>

    <div v-if="loading && items.length === 0" class="inbox-skeleton" aria-hidden="true">
      <div v-for="index in 5" :key="index" class="inbox-skeleton__row">
        <span class="xh-skeleton-bone xh-skeleton-bone--circle inbox-skeleton__icon" />
        <div class="inbox-skeleton__body">
          <span class="xh-skeleton-bone" />
          <span class="xh-skeleton-bone" />
        </div>
      </div>
    </div>

    <XhEmptyStateRoot v-else-if="items.length === 0" class="inbox-empty">
      <XhEmptyStateIndicator>
        <Icon icon="lucide:inbox" width="28" height="28" />
      </XhEmptyStateIndicator>
      <XhEmptyStateTitle>{{ t('common.no_data') }}</XhEmptyStateTitle>
      <XhEmptyStateDescription>{{ t('notification.inbox.empty') }}</XhEmptyStateDescription>
    </XhEmptyStateRoot>

    <div v-else class="inbox-list" data-testid="inbox-list">
      <article
        v-for="item in items"
        :key="item.id"
        class="inbox-item"
        :class="{ 'inbox-item--attention': needsAttention(item) }"
      >
        <div class="inbox-item__icon" :style="iconStyle(item)" aria-hidden="true">
          <Icon :icon="item.icon || 'lucide:bell'" width="18" />
        </div>

        <div class="inbox-item__main">
          <div class="inbox-item__head">
            <div class="inbox-item__title">
              {{ item.title }}
            </div>
            <div class="inbox-item__tags">
              <XhTagRoot variant="subtle" :tone="notificationTypeTone(item.notificationType)" size="sm">
                <XhTagLabel>{{ notificationTypeLabel(t, item.notificationType) }}</XhTagLabel>
              </XhTagRoot>
              <XhTagRoot v-if="isUnread(item)" variant="subtle" size="sm" tone="warning">
                <XhTagLabel>{{ t('notification.inbox.unread') }}</XhTagLabel>
              </XhTagRoot>
              <XhTagRoot v-if="isUnconfirmed(item)" variant="subtle" size="sm" tone="danger">
                <XhTagLabel>{{ t('notification.inbox.pending_confirm') }}</XhTagLabel>
              </XhTagRoot>
            </div>
          </div>

          <NotificationContent
            v-if="item.content"
            class="inbox-item__content"
            :content="item.content"
            :format="item.contentFormat as ShellContentFormat"
          />
          <p v-else class="inbox-item__content">
            {{ t('notification.inbox.no_content') }}
          </p>

          <div class="inbox-item__foot">
            <span>{{ formatDate(item.publishedAt) }}</span>
            <span v-if="item.readAt">{{ t('notification.inbox.read_at', { time: formatDate(item.readAt) }) }}</span>
            <span v-if="item.confirmedAt">{{ t('notification.inbox.confirmed_at', { time: formatDate(item.confirmedAt) }) }}</span>
          </div>
        </div>

        <div class="inbox-item__actions">
          <XIconButton v-if="isUnread(item)" icon="lucide:check" tone="brand" :label="t('notification.inbox.mark_read')" @click="handleMarkRead(item)" />
          <XIconButton v-if="isUnconfirmed(item)" icon="lucide:badge-check" tone="warning" :label="t('notification.inbox.confirm')" @click="handleConfirm(item)" />
          <XIconButton v-if="resolveLink(item.link)" icon="lucide:external-link" :label="t('notification.inbox.open_link')" @click="handleOpenLink(item)" />
          <XIconButton icon="lucide:trash-2" tone="danger" :label="t('notification.inbox.delete')" @click="handleDelete(item)" />
        </div>
      </article>
    </div>

    <div v-if="total > INBOX_PAGE_SIZE" class="inbox-footer">
      <SchemaPagination :total="total" :page="page" :page-size="INBOX_PAGE_SIZE" :page-sizes="[INBOX_PAGE_SIZE]" @update:page="changePage" />
    </div>
  </div>
</template>

<style scoped>
.inbox-page {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-2);
  padding: var(--xh-space-3);
  height: 100%;
  min-height: 0;
}

/* 两张卡的内边距与列表页的搜索卡、工具条卡同档（SchemaPage 的 .xh-schema-card） */
.inbox-card {
  --xh-card-p: var(--xh-surface-pad-md) var(--xh-surface-pad-lg);

  flex-shrink: 0;
}

.inbox-card--toolbar {
  --xh-card-p: var(--xh-surface-pad-sm) var(--xh-surface-pad-lg);
}

/* 筛选项：与列表页搜索栏同款——项宽取控件下限（列表页的标准项宽就落在这一档）、字段名在上小字淡色、控件铺满项宽 */
.inbox-search {
  display: flex;
  flex-wrap: wrap;
  gap: var(--xh-space-2_5) var(--xh-space-3);
  align-items: flex-end;
}

.inbox-search__item {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-0_5);
  inline-size: var(--xh-control-min-w);
}

.inbox-search__label {
  font-size: var(--xh-font-size-xs);
  font-weight: 400;
  line-height: 1.4;
  color: var(--xh-fg-subtle);
}

/* 操作靠左；计数与刷新作为流的最后一项推到右侧 */
.inbox-toolbar {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: var(--xh-space-2);
}

.inbox-toolbar__status {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: var(--xh-space-2);
  margin-inline-start: auto;
}

.inbox-skeleton {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-2);
}

.inbox-skeleton__row {
  display: flex;
  gap: var(--xh-space-3);
  padding: var(--xh-space-4);
  border: 1px solid var(--xh-border-default);
  border-radius: var(--xh-shape-surface);
  background: var(--xh-bg-surface);
}

.inbox-skeleton__icon {
  inline-size: 2.375rem;
  block-size: 2.375rem;
  flex-shrink: 0;
}

.inbox-skeleton__body {
  display: flex;
  flex: 1;
  flex-direction: column;
  gap: var(--xh-space-2);
  min-width: 0;
}

.inbox-empty {
  flex: 1;
  justify-content: center;
}

/* 行式列表（与顶栏通知中心一致）：靠分隔线区分条目，不留间隙、无卡片边框 */
.inbox-list {
  display: flex;
  flex: 1;
  flex-direction: column;
  min-height: 0;
  overflow-y: auto;
}

.inbox-item {
  display: grid;
  grid-template-columns: auto minmax(0, 1fr) auto;
  gap: var(--xh-space-3);
  align-items: start;
  padding: var(--xh-space-4);
  border-bottom: 1px solid var(--xh-border-subtle);
  transition: background-color var(--xh-motion-duration-micro) var(--xh-motion-ease-enter);
}

.inbox-item:last-child {
  border-bottom: none;
}

.inbox-item:hover {
  background: var(--xh-bg-subtle-hover);
}

/* 未读 / 待处理：只铺一层浅品牌底 */
.inbox-item--attention {
  background: color-mix(in oklab, var(--xh-bg-brand) 4%, transparent);
}

.inbox-item--attention:hover {
  background: color-mix(in oklab, var(--xh-bg-brand) 8%, transparent);
}

.inbox-item__icon {
  display: flex;
  align-items: center;
  justify-content: center;
  inline-size: 2.375rem;
  block-size: 2.375rem;
  border-radius: var(--xh-shape-inset);
  flex-shrink: 0;
}

.inbox-item__main {
  min-width: 0;
}

.inbox-item__head {
  display: flex;
  align-items: center;
  gap: var(--xh-space-2);
}

.inbox-item__title {
  flex: 0 1 auto;
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  font-size: var(--xh-font-size-md);
  font-weight: var(--xh-font-weight-semibold);
  color: var(--xh-fg-default);
}

.inbox-item__tags {
  display: flex;
  align-items: center;
  gap: var(--xh-space-1);
  flex-shrink: 0;
}

.inbox-item__content {
  margin: var(--xh-space-2) 0 0;
  color: var(--xh-fg-muted);
  line-height: 1.55;
  word-break: break-word;
}

.inbox-item__foot {
  display: flex;
  flex-wrap: wrap;
  gap: var(--xh-space-3);
  margin-top: var(--xh-space-2);
  font-size: var(--xh-text-caption-size);
  color: var(--xh-fg-subtle);
}

.inbox-item__actions {
  display: flex;
  flex-wrap: nowrap;
  gap: var(--xh-space-1);
}

.inbox-footer {
  display: flex;
  flex-shrink: 0;
  justify-content: flex-end;
}

@media (prefers-reduced-motion: reduce) {
  .inbox-item {
    transition: none;
  }
}

@media (max-width: 45rem) {
  .inbox-search__item {
    inline-size: 100%;
  }

  .inbox-item {
    grid-template-columns: auto minmax(0, 1fr);
  }

  .inbox-item__actions {
    grid-column: 2;
  }

  .inbox-item__head {
    align-items: flex-start;
    flex-direction: column;
  }

  .inbox-item__title {
    white-space: normal;
  }
}
</style>
