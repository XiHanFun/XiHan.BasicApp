<script setup lang="ts">
import type { OnlineSessionRow, SessionSummary } from './use-online-users'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import { computed, h, onActivated, onBeforeUnmount, onDeactivated, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { Icon, renderTag, SchemaPage } from '~/components'
import { dialog, toast } from '~/composables'
import { DEVICE_TYPES, formatDuration, loadSessionSummary, onlineSeconds, onlineSessionsResource, revokeSession, sessionUserName } from './use-online-users'

defineOptions({ name: 'IdentityOnlineUsersView' })

const { t } = useI18n()

const schemaPageRef = ref<InstanceType<typeof SchemaPage> | null>(null)
const resource = onlineSessionsResource()
const asRow = (row: object) => row as OnlineSessionRow

// ── 概览（30s 自动刷新） ─────────────────────────────────────────
const SUMMARY_REFRESH_MS = 30_000
const summary = ref<SessionSummary | null>(null)
let summaryTimer: ReturnType<typeof setInterval> | null = null

async function loadSummary() {
  try {
    summary.value = await loadSessionSummary()
  }
  catch {
    // 概览失败不打扰列表使用
  }
}

function startSummaryPolling() {
  if (summaryTimer) {
    return
  }

  void loadSummary()
  summaryTimer = setInterval(() => void loadSummary(), SUMMARY_REFRESH_MS)
}

function stopSummaryPolling() {
  if (summaryTimer) {
    clearInterval(summaryTimer)
    summaryTimer = null
  }
}

onMounted(startSummaryPolling)
onActivated(startSummaryPolling)
onDeactivated(stopSummaryPolling)
onBeforeUnmount(stopSummaryPolling)

// ── 列表 ─────────────────────────────────────────────────────────
function deviceLabel(device: string | null | undefined): string {
  switch (device) {
    case 'Desktop': return t('identity.online_users.device_desktop')
    case 'Mobile': return t('identity.online_users.device_mobile')
    case 'Tablet': return t('identity.online_users.device_tablet')
    case 'Bot': return t('identity.online_users.device_bot')
    default: return t('identity.online_users.device_unknown')
  }
}

const deviceOptions = computed(() => DEVICE_TYPES.map(value => ({ label: deviceLabel(value), value })))

const fields = computed<ListFieldSchema[]>(() => [
  { key: 'keyword', title: t('identity.online_users.col_keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('identity.online_users.keyword_placeholder'), order: 0 },
  {
    key: 'userName',
    title: t('identity.online_users.col_user'),
    dataType: 'string',
    minWidth: 140,
    order: 10,
    render: (row) => {
      const r = asRow(row)
      return h('div', { class: 'ou-user' }, [
        h('span', { class: 'ou-user__name' }, sessionUserName(r)),
        r.userName && r.displayName ? h('span', { class: 'ou-user__login' }, `@${r.userName}`) : null,
      ])
    },
  },
  {
    key: 'isRealtimeOnline',
    title: t('identity.online_users.col_realtime'),
    dataType: 'boolean',
    width: 100,
    order: 11,
    render: (row) => {
      const online = asRow(row).isRealtimeOnline
      return renderTag(online ? t('identity.online_users.realtime_online') : t('identity.online_users.realtime_logged'), online ? 'success' : 'neutral')
    },
  },
  {
    key: 'device',
    title: t('identity.online_users.col_device_type'),
    dataType: 'enum',
    searchable: true,
    searchMultiple: true,
    options: deviceOptions.value,
    searchPlaceholder: t('identity.online_users.device_type_placeholder'),
    width: 100,
    order: 12,
    render: row => renderTag(deviceLabel(asRow(row).device), 'info'),
  },
  { key: 'os', title: t('identity.online_users.col_os'), dataType: 'string', advancedSearch: true, minWidth: 110, order: 14 },
  { key: 'browser', title: t('identity.online_users.col_browser'), dataType: 'string', advancedSearch: true, minWidth: 110, order: 15 },
  { key: 'ipAddressMasked', title: t('identity.online_users.col_ip'), dataType: 'string', minWidth: 120, order: 16 },
  { key: 'location', title: t('identity.online_users.col_location'), dataType: 'string', minWidth: 140, order: 17 },
  { key: 'loginAt', title: t('identity.online_users.col_login_time'), dataType: 'datetime', sortable: true, minWidth: 170, order: 18 },
  { key: 'lastActiveAt', title: t('identity.online_users.col_last_activity'), dataType: 'datetime', sortable: true, minWidth: 170, order: 19 },
  {
    key: 'onlineDuration',
    title: t('identity.online_users.col_online_duration'),
    dataType: 'string',
    width: 130,
    order: 20,
    render: row => h('span', formatDuration(onlineSeconds(asRow(row)), t)),
  },
  { key: 'id', title: t('identity.online_users.col_session_id'), dataType: 'string', visible: false, minWidth: 180, order: 21 },
  { key: 'userId', title: t('identity.online_users.col_user_id'), dataType: 'string', visible: false, advancedSearch: true, order: 30 },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'identity.online-users',
  pageName: t('identity.online_users.title'),
  exportPermission: 'identity.sessions.read',
  rowKey: 'id',
  fields: fields.value,
  resource: resource as unknown as PageSchema['resource'],
  actions: [
    { key: 'revoke', title: t('identity.online_users.action_revoke'), scope: 'row', icon: 'lucide:log-out', tone: 'danger', permission: 'identity.sessions.revoke', visible: row => !asRow(row as object).isCurrent },
  ],
}))

function onAction(payload: SchemaActionPayload) {
  const row = payload.row ? asRow(payload.row) : undefined
  if (payload.key === 'revoke' && row) {
    confirmRevoke(row)
  }
}

function confirmRevoke(row: OnlineSessionRow) {
  void dialog.confirm({
    badge: 'warning',
    tone: 'danger',
    title: t('identity.online_users.revoke_title'),
    content: t('identity.online_users.revoke_content', { name: sessionUserName(row) }),
    okText: t('identity.online_users.revoke_confirm'),
    cancelText: t('common.actions.cancel'),
    onOk: async () => {
      try {
        await revokeSession(row.id, t('identity.online_users.revoke_reason'))
        toast.success(t('identity.online_users.revoke_done'))
        void loadSummary()
        void schemaPageRef.value?.reload()
      }
      catch (e) {
        toast.danger((e as Error).message || t('identity.online_users.revoke_failed'))
      }
    },
  })
}
</script>

<template>
  <div class="online-user-page">
    <div class="ou-summary">
      <div class="ou-card">
        <span class="ou-card__icon is-online">
          <Icon icon="lucide:radio" width="18" height="18" />
        </span>
        <div class="ou-card__body">
          <span class="ou-card__value">{{ summary?.realtimeOnlineUsers ?? '–' }}</span>
          <span class="ou-card__label">{{ t('identity.online_users.summary_realtime_online') }}</span>
        </div>
      </div>
      <div class="ou-card">
        <span class="ou-card__icon is-session">
          <Icon icon="lucide:monitor-smartphone" width="18" height="18" />
        </span>
        <div class="ou-card__body">
          <span class="ou-card__value">{{ summary?.activeSessions ?? '–' }}</span>
          <span class="ou-card__label">{{ t('identity.online_users.summary_active_sessions') }}</span>
        </div>
      </div>
      <div class="ou-card">
        <span class="ou-card__icon is-user">
          <Icon icon="lucide:users" width="18" height="18" />
        </span>
        <div class="ou-card__body">
          <span class="ou-card__value">{{ summary?.activeUsers ?? '–' }}</span>
          <span class="ou-card__label">{{ t('identity.online_users.summary_active_users') }}</span>
        </div>
      </div>
    </div>

    <!-- SchemaPage 自带满高布局（h-full），须给它定高容器，分页脚才能贴底、表格内部滚动 -->
    <div class="ou-table">
      <SchemaPage ref="schemaPageRef" :schema="schema" @action="onAction" />
    </div>
  </div>
</template>

<style scoped>
.online-user-page {
  display: flex;
  flex-direction: column;
  height: 100%;
  overflow: hidden;
}

.ou-summary {
  flex-shrink: 0;
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(min(100%, 11.25rem), 1fr));
  gap: var(--xh-space-3);
  /* 与 SchemaPage 内部 p-3 水平对齐；下方间距由 SchemaPage 自身上内边距提供 */
  padding: var(--xh-space-3) var(--xh-space-3) 0;
}

.ou-table {
  flex: 1;
  min-height: 0;
}

.ou-card {
  display: flex;
  align-items: center;
  gap: var(--xh-space-3);
  padding: var(--xh-space-3) var(--xh-space-4);
  border: 1px solid var(--xh-border-default);
  border-radius: var(--xh-shape-surface);
  background: var(--xh-bg-surface);
}

.ou-card__icon {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 2.25rem;
  height: 2.25rem;
  border-radius: var(--xh-shape-inset);
  flex-shrink: 0;
}

/* 在线 → success、会话 → 品牌色、用户 → 紫色只为区分；底是同色 12% 淡底 */
.ou-card__icon.is-online {
  color: var(--xh-fg-success);
  background: color-mix(in oklab, var(--xh-color-success-600) 12%, transparent);
}

.ou-card__icon.is-session {
  color: var(--xh-fg-brand);
  background: color-mix(in oklab, var(--xh-bg-brand) 12%, transparent);
}

.ou-card__icon.is-user {
  color: var(--xh-color-purple-600);
  background: color-mix(in oklab, var(--xh-color-purple-600) 12%, transparent);
}

.ou-card__body {
  display: flex;
  flex-direction: column;
  line-height: 1.3;
}

.ou-card__value {
  font-size: var(--xh-font-size-xl);
  font-weight: var(--xh-font-weight-semibold);
  font-variant-numeric: tabular-nums;
}

.ou-card__label {
  font-size: var(--xh-text-secondary-size);
  color: var(--xh-fg-muted);
}

/* 列 render 的 h() 节点不带页面 scope：样式写 :deep() */
:deep(.ou-user) {
  display: flex;
  flex-direction: column;
  line-height: 1.35;
}

:deep(.ou-user__name) {
  font-weight: var(--xh-font-weight-medium);
}

:deep(.ou-user__login) {
  font-size: var(--xh-text-secondary-size);
  color: var(--xh-fg-muted);
}
</style>
