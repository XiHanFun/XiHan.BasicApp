<script setup lang="ts">
import {
  NCard,
  NGrid,
  NGridItem,
  NIcon,
  NProgress,
  NSkeleton,
  NStatistic,
  NTag,
  NTimeline,
  NTimelineItem,
} from 'naive-ui'
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { workbenchApi, serverManagementApi } from '@/api'
import { Icon } from '~/iconify'
import { useUserStore } from '~/stores'
import { formatDate } from '~/utils'

defineOptions({ name: 'WorkbenchDashboardPage' })

const router = useRouter()
const userStore = useUserStore()

const loading = ref(true)
const loginCount = ref(0)
const accessCount = ref(0)
const operationCount = ref(0)
const apiCallCount = ref(0)
const onlineTime = ref(0)
const unreadCount = ref(0)
const pendingConfirmCount = ref(0)
const lastLoginTime = ref<string | null>(null)
const serverCpu = ref(0)
const serverMemory = ref(0)
const serverDisk = ref(0)

const greeting = computed(() => {
  const h = new Date().getHours()
  if (h < 6) return '夜深了'
  if (h < 9) return '早上好'
  if (h < 12) return '上午好'
  if (h < 14) return '中午好'
  if (h < 18) return '下午好'
  if (h < 22) return '晚上好'
  return '夜深了'
})

const onlineTimeText = computed(() => formatDuration(onlineTime.value))
const lastLoginText = computed(() => lastLoginTime.value ? formatDate(lastLoginTime.value) : '暂无记录')

const statCards = computed(() => [
  {
    label: '登录次数',
    value: loginCount.value,
    sub: '今日登录行为',
    icon: 'lucide:log-in',
    color: '#3b82f6',
    bg: 'rgba(59,130,246,0.1)',
  },
  {
    label: '访问次数',
    value: accessCount.value,
    sub: '今日页面访问',
    icon: 'lucide:activity',
    color: '#22c55e',
    bg: 'rgba(34,197,94,0.1)',
  },
  {
    label: '操作次数',
    value: operationCount.value,
    sub: '今日业务操作',
    icon: 'lucide:clipboard-list',
    color: '#ef4444',
    bg: 'rgba(239,68,68,0.1)',
  },
  {
    label: 'API 调用',
    value: apiCallCount.value,
    sub: '今日接口调用',
    icon: 'lucide:globe',
    color: '#f59e0b',
    bg: 'rgba(245,158,11,0.1)',
  },
])

const systemMetrics = computed(() => [
  { label: 'CPU', value: serverCpu.value, color: '#3b82f6' },
  { label: '内存', value: serverMemory.value, color: '#22c55e' },
  { label: '磁盘', value: serverDisk.value, color: '#f59e0b' },
])

const quickLinks = [
  { label: '站内信', desc: '查看系统消息', icon: 'lucide:inbox', to: '/workbench/inbox', color: '#3b82f6' },
  { label: '用户管理', desc: '账号与权限', icon: 'lucide:users', to: '/system/user', color: '#22c55e' },
  { label: '角色管理', desc: '角色与授权', icon: 'lucide:shield-user', to: '/system/role', color: '#8b5cf6' },
  { label: '菜单管理', desc: '导航配置', icon: 'lucide:list-tree', to: '/platform/menu', color: '#f59e0b' },
  { label: '日志管理', desc: '审计与追踪', icon: 'lucide:file-search', to: '/log/login', color: '#ef4444' },
  { label: '个人中心', desc: '资料与安全', icon: 'lucide:user-round-cog', to: '/workbench/profile', color: '#06b6d4' },
]

const recentActivities = [
  { time: '10 分钟前', text: '超级管理员 更新了角色权限配置' },
  { time: '30 分钟前', text: '系统 执行了数据备份任务' },
  { time: '1 小时前', text: '超级管理员 创建了新用户' },
  { time: '2 小时前', text: '系统 完成日志归档' },
]

async function fetchDashboardData() {
  try {
    const summary = await workbenchApi.dashboard.summary()
    loginCount.value = summary.statistics.loginCount
    accessCount.value = summary.statistics.accessCount
    operationCount.value = summary.statistics.operationCount
    apiCallCount.value = summary.statistics.apiCallCount
    onlineTime.value = summary.statistics.onlineTime
    lastLoginTime.value = summary.statistics.lastLoginTime ?? null
    unreadCount.value = summary.inbox.unreadCount
    pendingConfirmCount.value = summary.inbox.pendingConfirmCount
  }
  catch {
    resetStats()
  }
  finally {
    loading.value = false
  }
}

async function fetchServerMetrics() {
  try {
    const [cpu, memory, disks] = await Promise.all([
      serverManagementApi.getCpuInfo(),
      serverManagementApi.getMemoryInfo(),
      serverManagementApi.getDiskInfo(),
    ])
    serverCpu.value = Math.round(cpu.usagePercentage)
    serverMemory.value = Math.round(memory.usagePercentage)
    const avgDisk = disks.length > 0
      ? Math.round(disks.reduce((sum, d) => sum + (100 - d.availableRate), 0) / disks.length)
      : 0
    serverDisk.value = avgDisk
  }
  catch {
    serverCpu.value = 0
    serverMemory.value = 0
    serverDisk.value = 0
  }
}

function resetStats() {
  loginCount.value = 0
  accessCount.value = 0
  operationCount.value = 0
  apiCallCount.value = 0
  onlineTime.value = 0
  unreadCount.value = 0
  pendingConfirmCount.value = 0
  lastLoginTime.value = null
}

function formatDuration(seconds: number) {
  if (seconds <= 0) return '0 分钟'
  const hours = Math.floor(seconds / 3600)
  const minutes = Math.floor((seconds % 3600) / 60)
  if (hours > 0) return `${hours} 小时 ${minutes} 分钟`
  return `${Math.max(minutes, 1)} 分钟`
}

function getProgressStatus(value: number) {
  if (value >= 90) return 'error'
  if (value >= 70) return 'warning'
  return 'success'
}

onMounted(() => {
  fetchDashboardData()
  fetchServerMetrics()
})
</script>

<template>
  <div class="dashboard">
    <NCard :bordered="false" class="welcome-card">
      <div class="welcome-inner">
        <div class="welcome-info">
          <h2 class="welcome-title">
            {{ greeting }}，{{ userStore.nickname || userStore.username }}
          </h2>
          <p class="welcome-sub">
            {{ formatDate(new Date(), 'YYYY年MM月DD日 dddd') }}
          </p>
          <div class="welcome-tags">
            <NTag :bordered="false" round size="small" type="info">
              今日在线 {{ onlineTimeText }}
            </NTag>
            <NTag :bordered="false" round size="small" type="default">
              最后登录 {{ lastLoginText }}
            </NTag>
          </div>
        </div>
        <div class="welcome-stats">
          <div class="welcome-stat">
            <span class="welcome-stat-value">{{ unreadCount }}</span>
            <span class="welcome-stat-label">未读消息</span>
          </div>
          <div class="welcome-stat">
            <span class="welcome-stat-value">{{ pendingConfirmCount }}</span>
            <span class="welcome-stat-label">待确认</span>
          </div>
        </div>
      </div>
    </NCard>

    <NGrid :cols="4" :item-responsive="true" :x-gap="14" :y-gap="14" responsive="screen">
      <NGridItem v-for="card in statCards" :key="card.label" span="4 s:2 m:1">
        <NCard :bordered="false" class="stat-card">
          <NSkeleton v-if="loading" :height="88" :width="'100%'" />
          <div v-else class="stat-inner">
            <div class="stat-icon" :style="{ backgroundColor: card.bg }">
              <NIcon :color="card.color" size="24">
                <Icon :icon="card.icon" />
              </NIcon>
            </div>
            <div class="stat-body">
              <NStatistic :value="card.value" class="stat-value">
                <template #suffix>
                  <span class="stat-sub">{{ card.sub }}</span>
                </template>
              </NStatistic>
              <span class="stat-label">{{ card.label }}</span>
            </div>
          </div>
        </NCard>
      </NGridItem>
    </NGrid>

    <NGrid :cols="3" :item-responsive="true" :x-gap="14" :y-gap="14" responsive="screen">
      <NGridItem span="3 m:2">
        <NCard :bordered="false" class="section-card">
          <template #header>
            <div class="section-header">
              <NIcon class="section-icon" size="16">
                <Icon icon="lucide:zap" />
              </NIcon>
              <span>快捷入口</span>
            </div>
          </template>
          <div class="quick-grid">
            <button
              v-for="link in quickLinks"
              :key="link.label"
              class="quick-item"
              type="button"
              @click="router.push(link.to)"
            >
              <div class="quick-icon" :style="{ backgroundColor: `${link.color}18` }">
                <NIcon :style="{ color: link.color }" size="22">
                  <Icon :icon="link.icon" />
                </NIcon>
              </div>
              <div class="quick-text">
                <span class="quick-label">{{ link.label }}</span>
                <span class="quick-desc">{{ link.desc }}</span>
              </div>
            </button>
          </div>
        </NCard>
      </NGridItem>

      <NGridItem span="3 m:1">
        <NCard :bordered="false" class="section-card">
          <template #header>
            <div class="section-header">
              <NIcon class="section-icon" size="16">
                <Icon icon="lucide:cpu" />
              </NIcon>
              <span>系统状态</span>
            </div>
          </template>
          <div class="metrics-list">
            <div v-for="metric in systemMetrics" :key="metric.label" class="metric-item">
              <div class="metric-header">
                <span class="metric-label">{{ metric.label }}</span>
                <span class="metric-value" :style="{ color: metric.color }">{{ metric.value }}%</span>
              </div>
              <NProgress
                :color="metric.color"
                :percentage="metric.value"
                :processing="metric.value > 70"
                :status="getProgressStatus(metric.value)"
                :height="6"
                border-radius="3px"
              />
            </div>
          </div>
        </NCard>
      </NGridItem>

      <NGridItem span="3 m:1">
        <NCard :bordered="false" class="section-card">
          <template #header>
            <div class="section-header">
              <NIcon class="section-icon" size="16">
                <Icon icon="lucide:clock" />
              </NIcon>
              <span>最近动态</span>
            </div>
          </template>
          <NTimeline>
            <NTimelineItem
              v-for="(item, index) in recentActivities"
              :key="index"
              :color="index === 0 ? '#3b82f6' : undefined"
              :time="item.time"
              line-type="dashed"
            >
              {{ item.text }}
            </NTimelineItem>
          </NTimeline>
        </NCard>
      </NGridItem>
    </NGrid>
  </div>
</template>

<style scoped>
.dashboard {
  display: flex;
  flex-direction: column;
  gap: 14px;
  padding: 16px;
  max-width: 1400px;
  margin: 0 auto;
  width: 100%;
}

/* Welcome Card */
.welcome-card {
  background: linear-gradient(135deg, hsl(var(--primary) / 0.06), hsl(var(--primary) / 0.02)) !important;
  border: 1px solid hsl(var(--border)) !important;
  border-radius: 12px !important;
}

.welcome-card :deep(.n-card__content) {
  padding: 20px 24px !important;
}

.welcome-inner {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 24px;
}

.welcome-title {
  margin: 0;
  color: hsl(var(--foreground));
  font-size: 20px;
  font-weight: 700;
  letter-spacing: -0.02em;
}

.welcome-sub {
  margin: 4px 0 10px;
  color: hsl(var(--muted-foreground));
  font-size: 13px;
}

.welcome-tags {
  display: flex;
  gap: 8px;
  flex-wrap: wrap;
}

.welcome-stats {
  display: none;
  gap: 24px;
  flex-shrink: 0;
}

@media (min-width: 768px) {
  .welcome-stats {
    display: flex;
  }
}

.welcome-stat {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 2px;
}

.welcome-stat-value {
  color: hsl(var(--foreground));
  font-size: 24px;
  font-weight: 700;
}

.welcome-stat-label {
  color: hsl(var(--muted-foreground));
  font-size: 11px;
}

/* Stat Cards */
.stat-card {
  border-radius: 12px !important;
  transition: transform 0.15s ease, box-shadow 0.15s ease;
}

.stat-card :deep(.n-card__content) {
  padding: 18px 20px !important;
}

.stat-card:hover {
  transform: translateY(-1px);
  box-shadow: 0 4px 16px hsl(var(--foreground) / 0.06);
}

.stat-inner {
  display: flex;
  align-items: center;
  gap: 16px;
}

.stat-icon {
  display: flex;
  flex-shrink: 0;
  align-items: center;
  justify-content: center;
  width: 52px;
  height: 52px;
  border-radius: 14px;
}

.stat-body {
  min-width: 0;
  flex: 1;
}

.stat-value :deep(.n-statistic__value) {
  display: flex;
  align-items: baseline;
  gap: 8px;
}

.stat-value :deep(.n-statistic__value__suffix) {
  font-size: 12px;
}

.stat-label {
  color: hsl(var(--muted-foreground));
  font-size: 13px;
  font-weight: 500;
}

.stat-sub {
  color: hsl(var(--muted-foreground));
  font-size: 12px;
}

/* Section Card */
.section-card {
  border-radius: 12px !important;
  height: 100%;
}

.section-card :deep(.n-card__header) {
  padding: 14px 20px 10px;
}

.section-card :deep(.n-card__content) {
  padding: 12px 20px 18px;
}

.section-header {
  display: flex;
  align-items: center;
  gap: 8px;
  color: hsl(var(--foreground));
  font-size: 14px;
  font-weight: 600;
}

.section-icon {
  color: hsl(var(--primary));
}

/* Quick Links */
.quick-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 4px;
}

@media (min-width: 900px) {
  .quick-grid {
    grid-template-columns: repeat(6, 1fr);
  }
}

.quick-item {
  display: flex;
  align-items: center;
  flex-direction: column;
  gap: 10px;
  padding: 16px 8px;
  border: none;
  border-radius: 10px;
  background: transparent;
  cursor: pointer;
  outline: none;
  transition: background 0.15s ease;
  text-align: center;
}

.quick-item:hover {
  background: hsl(var(--accent));
}

.quick-icon {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 44px;
  height: 44px;
  border-radius: 12px;
}

.quick-text {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.quick-label {
  color: hsl(var(--foreground));
  font-size: 12px;
  font-weight: 500;
}

.quick-desc {
  color: hsl(var(--muted-foreground));
  font-size: 10px;
}

/* System Metrics */
.metrics-list {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.metric-item {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.metric-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.metric-label {
  color: hsl(var(--muted-foreground));
  font-size: 12px;
  font-weight: 500;
}

.metric-value {
  font-size: 13px;
  font-weight: 700;
  font-variant-numeric: tabular-nums;
}
</style>
