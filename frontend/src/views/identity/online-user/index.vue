<script setup lang="ts">
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import type { OnlineUserListItemDto, OnlineUserSummaryDto, PageResult } from '@/api'
import { NTag, useDialog, useMessage } from 'naive-ui'
import { h, onBeforeUnmount, onMounted, ref } from 'vue'
import { createPageRequest, DeviceType, onlineUserApi, userSessionApi } from '@/api'
import { Icon, SchemaPage } from '~/components'
import { getOptionLabel } from '~/utils'

defineOptions({ name: 'IdentityOnlineUserPage' })

const message = useMessage()
const dialog = useDialog()

const schemaPageRef = ref<InstanceType<typeof SchemaPage> | null>(null)

// ── 概览（30s 自动刷新） ─────────────────────────────────────────
const SUMMARY_REFRESH_MS = 30_000
const summary = ref<OnlineUserSummaryDto | null>(null)
let summaryTimer: ReturnType<typeof setInterval> | null = null

async function loadSummary() {
  try {
    summary.value = await onlineUserApi.summary()
  }
  catch {
    // 概览失败不打扰列表使用
  }
}

onMounted(() => {
  void loadSummary()
  summaryTimer = setInterval(() => void loadSummary(), SUMMARY_REFRESH_MS)
})

onBeforeUnmount(() => {
  if (summaryTimer) {
    clearInterval(summaryTimer)
    summaryTimer = null
  }
})

// ── 列表 ─────────────────────────────────────────────────────────
const deviceTypeOptions = [
  { label: '未知', value: DeviceType.Unknown },
  { label: 'Web', value: DeviceType.Web },
  { label: 'iOS', value: DeviceType.iOS },
  { label: 'Android', value: DeviceType.Android },
  { label: 'Windows', value: DeviceType.Windows },
  { label: 'macOS', value: DeviceType.macOS },
  { label: 'Linux', value: DeviceType.Linux },
  { label: '平板', value: DeviceType.Tablet },
  { label: '小程序', value: DeviceType.MiniProgram },
  { label: 'API', value: DeviceType.Api },
]

function formatDuration(seconds: number): string {
  if (seconds < 60) {
    return `${seconds} 秒`
  }
  if (seconds < 3600) {
    return `${Math.floor(seconds / 60)} 分钟`
  }
  if (seconds < 86400) {
    return `${Math.floor(seconds / 3600)} 小时 ${Math.floor((seconds % 3600) / 60)} 分`
  }
  return `${Math.floor(seconds / 86400)} 天 ${Math.floor((seconds % 86400) / 3600)} 小时`
}

const fields: ListFieldSchema[] = [
  { key: 'keyword', title: '关键词', dataType: 'string', visible: false, searchable: true, searchPlaceholder: '搜索会话/设备/系统/浏览器', order: 0 },
  {
    key: 'userName',
    title: '用户',
    dataType: 'string',
    minWidth: 140,
    order: 10,
    render: (row) => {
      const r = row as unknown as OnlineUserListItemDto
      const name = r.nickName || r.userName || `#${r.userId}`
      return h('div', { style: 'display:flex;flex-direction:column;line-height:1.35' }, [
        h('span', { style: 'font-weight:500' }, name),
        r.userName && r.nickName ? h('span', { style: 'font-size:12px;opacity:.65' }, `@${r.userName}`) : null,
      ])
    },
  },
  {
    key: 'isRealtimeOnline',
    title: '实时连接',
    dataType: 'boolean',
    width: 100,
    order: 11,
    render: (row) => {
      const online = (row as unknown as OnlineUserListItemDto).isRealtimeOnline
      return h(
        NTag,
        { size: 'small', round: true, bordered: false, type: online ? 'success' : 'default' },
        () => online ? '在线' : '登录态',
      )
    },
  },
  {
    key: 'deviceType',
    title: '设备类型',
    dataType: 'enum',
    searchable: true,
    options: deviceTypeOptions,
    searchPlaceholder: '设备类型',
    width: 100,
    order: 12,
    render: row => h(
      NTag,
      { size: 'small', round: true, bordered: false, type: 'info' },
      () => getOptionLabel(deviceTypeOptions, (row as unknown as OnlineUserListItemDto).deviceType),
    ),
  },
  { key: 'deviceName', title: '设备名称', dataType: 'string', minWidth: 120, order: 13 },
  { key: 'operatingSystem', title: '操作系统', dataType: 'string', minWidth: 110, order: 14 },
  { key: 'browser', title: '浏览器', dataType: 'string', minWidth: 110, order: 15 },
  { key: 'ipAddressMasked', title: 'IP 地址', dataType: 'string', minWidth: 120, order: 16 },
  { key: 'location', title: '登录地点', dataType: 'string', minWidth: 140, order: 17 },
  { key: 'loginTime', title: '登录时间', dataType: 'datetime', minWidth: 170, order: 18 },
  { key: 'lastActivityTime', title: '最后活动', dataType: 'datetime', sortable: true, minWidth: 170, order: 19 },
  {
    key: 'onlineDurationSeconds',
    title: '在线时长',
    dataType: 'string',
    width: 130,
    order: 20,
    render: row => h('span', formatDuration((row as unknown as OnlineUserListItemDto).onlineDurationSeconds)),
  },
  { key: 'userSessionId', title: '会话标识', dataType: 'string', visible: false, minWidth: 180, order: 21 },
  { key: 'userId', title: '用户主键', dataType: 'string', visible: false, advancedSearch: true, order: 30 },
]

function toStr(v: unknown): string | undefined {
  return (v as string | undefined)?.trim() || undefined
}

const schema: PageSchema = {
  pageCode: 'identity.online-user',
  pageName: '在线用户',
  rowKey: 'basicId',
  scrollX: 1700,
  fields,
  resource: {
    page: (params) => {
      const f = params.filters
      return onlineUserApi.page({
        ...createPageRequest({ page: { pageIndex: params.page, pageSize: params.pageSize } }),
        keyword: toStr(f.keyword),
        deviceType: (f.deviceType as DeviceType | undefined) ?? undefined,
        userId: toStr(f.userId),
      }) as unknown as Promise<PageResult<Record<string, unknown>>>
    },
  },
  actions: [
    { key: 'revoke', title: '强制下线', scope: 'row', icon: 'lucide:log-out', type: 'error', permission: 'saas:user-session:revoke' },
  ],
}

function onAction(payload: SchemaActionPayload) {
  const row = payload.row as unknown as OnlineUserListItemDto | undefined
  if (payload.key === 'revoke' && row) {
    confirmRevoke(row)
  }
}

function confirmRevoke(row: OnlineUserListItemDto) {
  const name = row.nickName || row.userName || `#${row.userId}`
  dialog.warning({
    title: '强制下线',
    content: `确定将「${name}」的该会话强制下线吗？对方将立即收到下线通知并退出登录。`,
    positiveText: '强制下线',
    negativeText: '取消',
    onPositiveClick: async () => {
      try {
        await userSessionApi.revokeSession({ basicId: row.basicId, reason: '管理员强制下线' })
        message.success('已强制下线')
        void loadSummary()
        void schemaPageRef.value?.reload()
      }
      catch (e) {
        message.error((e as Error).message || '强制下线失败')
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
          <span class="ou-card__label">实时在线用户</span>
        </div>
      </div>
      <div class="ou-card">
        <span class="ou-card__icon is-session">
          <Icon icon="lucide:monitor-smartphone" width="18" height="18" />
        </span>
        <div class="ou-card__body">
          <span class="ou-card__value">{{ summary?.activeSessions ?? '–' }}</span>
          <span class="ou-card__label">活跃会话</span>
        </div>
      </div>
      <div class="ou-card">
        <span class="ou-card__icon is-user">
          <Icon icon="lucide:users" width="18" height="18" />
        </span>
        <div class="ou-card__body">
          <span class="ou-card__value">{{ summary?.activeUsers ?? '–' }}</span>
          <span class="ou-card__label">活跃用户</span>
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
  grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
  gap: 12px;
  /* 与 SchemaPage 内部 p-3（12px）水平对齐；下方间距由 SchemaPage 自身上内边距提供 */
  padding: 12px 12px 0;
}

.ou-table {
  flex: 1;
  min-height: 0;
}

.ou-card {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 14px 16px;
  border: 1px solid var(--n-border-color, hsl(var(--border)));
  border-radius: 10px;
  background: var(--n-color, hsl(var(--card)));
}

.ou-card__icon {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 36px;
  height: 36px;
  border-radius: 9px;
  flex-shrink: 0;
}

.ou-card__icon.is-online {
  color: #16a34a;
  background: rgb(22 163 74 / 12%);
}

.ou-card__icon.is-session {
  color: #2563eb;
  background: rgb(37 99 235 / 12%);
}

.ou-card__icon.is-user {
  color: #9333ea;
  background: rgb(147 51 234 / 12%);
}

.ou-card__body {
  display: flex;
  flex-direction: column;
  line-height: 1.3;
}

.ou-card__value {
  font-size: 20px;
  font-weight: 600;
  font-variant-numeric: tabular-nums;
}

.ou-card__label {
  font-size: 12px;
  opacity: 0.65;
}
</style>
