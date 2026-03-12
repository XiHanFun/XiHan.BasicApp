<script lang="ts" setup>
import type { SysCpuInfo, SysMemoryInfo, SysOperationLog, SysRuntimeInfo } from '~/types'
import { Icon } from '@iconify/vue'
import {
  NCard,
  NGrid,
  NGridItem,
  NIcon,
  NNumberAnimation,
  NProgress,
  NSkeleton,
  NTag,
  NTimeline,
  NTimelineItem,
} from 'naive-ui'
import { computed, onMounted, onUnmounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import {
  getCpuInfoApi,
  getMemoryInfoApi,
  getOperationLogPageApi,
  getRuntimeInfoApi,
  getUserPageApi,
  getUserSessionPageApi,
} from '@/api'
import { useUserStore } from '~/stores'
import { formatDate } from '~/utils'

defineOptions({ name: 'WorkspacePage' })

const router = useRouter()
const userStore = useUserStore()

const loading = ref(true)
const runtimeInfo = ref<SysRuntimeInfo | null>(null)
const cpuInfo = ref<SysCpuInfo | null>(null)
const memoryInfo = ref<SysMemoryInfo | null>(null)
const userTotal = ref(0)
const onlineCount = ref(0)
const recentLogs = ref<SysOperationLog[]>([])

let refreshTimer: ReturnType<typeof setInterval> | null = null

function formatBytes(bytes: number) {
  if (bytes === 0) return '0 B'
  const k = 1024
  const sizes = ['B', 'KB', 'MB', 'GB', 'TB']
  const i = Math.floor(Math.log(bytes) / Math.log(k))
  return `${(bytes / k ** i).toFixed(1)} ${sizes[i]}`
}

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

const quickLinks = [
  { label: '用户管理', icon: 'lucide:users', to: '/system/user', color: '#18a058' },
  { label: '角色管理', icon: 'lucide:shield', to: '/system/role', color: '#2080f0' },
  { label: '菜单管理', icon: 'lucide:list-tree', to: '/platform/menu', color: '#f0a020' },
  { label: '系统监控', icon: 'lucide:activity', to: '/platform/server', color: '#d03050' },
  { label: '操作日志', icon: 'lucide:history', to: '/log/oplog', color: '#8b5cf6' },
  { label: '参数配置', icon: 'lucide:sliders-horizontal', to: '/platform/config', color: '#06b6d4' },
]

const operationTypeMap: Record<string, { label: string, type: 'success' | 'info' | 'warning' | 'error' | 'default' }> = {
  Create: { label: '新增', type: 'success' },
  Update: { label: '修改', type: 'warning' },
  Delete: { label: '删除', type: 'error' },
  Query: { label: '查询', type: 'info' },
  Login: { label: '登录', type: 'success' },
  Logout: { label: '登出', type: 'default' },
  Export: { label: '导出', type: 'info' },
  Import: { label: '导入', type: 'info' },
  Other: { label: '其他', type: 'default' },
}

function getOperationTag(type?: string) {
  const key = type ?? ''
  return operationTypeMap[key] || { label: key || '未知', type: 'default' as const }
}

async function fetchDashboardData() {
  try {
    const [runtime, cpu, memory, users, sessions, logs] = await Promise.allSettled([
      getRuntimeInfoApi(),
      getCpuInfoApi(),
      getMemoryInfoApi(),
      getUserPageApi({ page: 1, pageSize: 1 }),
      getUserSessionPageApi({ page: 1, pageSize: 1 }),
      getOperationLogPageApi({ page: 1, pageSize: 8 }),
    ])

    if (runtime.status === 'fulfilled') runtimeInfo.value = runtime.value
    if (cpu.status === 'fulfilled') cpuInfo.value = cpu.value
    if (memory.status === 'fulfilled') memoryInfo.value = memory.value
    if (users.status === 'fulfilled') userTotal.value = users.value.total
    if (sessions.status === 'fulfilled') onlineCount.value = sessions.value.total
    if (logs.status === 'fulfilled') recentLogs.value = logs.value.items
  }
  finally {
    loading.value = false
  }
}

async function refreshHardware() {
  const [cpu, memory] = await Promise.allSettled([
    getCpuInfoApi(),
    getMemoryInfoApi(),
  ])
  if (cpu.status === 'fulfilled') cpuInfo.value = cpu.value
  if (memory.status === 'fulfilled') memoryInfo.value = memory.value
}

onMounted(() => {
  fetchDashboardData()
  refreshTimer = setInterval(refreshHardware, 15000)
})

onUnmounted(() => {
  if (refreshTimer) {
    clearInterval(refreshTimer)
  }
})
</script>

<template>
  <div class="dashboard-wrapper">
    <!-- 欢迎横幅 -->
    <NCard :bordered="false" class="welcome-card">
      <div class="flex items-center justify-between">
        <div>
          <h2 class="welcome-title">
            {{ greeting }}，{{ userStore.nickname || userStore.username }}
          </h2>
          <p class="welcome-sub">
            {{ formatDate(new Date(), 'YYYY年MM月DD日 dddd') }}
          </p>
        </div>
        <div class="welcome-icon">
          <NIcon size="48" class="text-[hsl(var(--primary))] opacity-40">
            <Icon icon="lucide:layout-dashboard" />
          </NIcon>
        </div>
      </div>
    </NCard>

    <!-- 概览卡片 -->
    <NGrid :x-gap="14" :y-gap="14" :cols="4" responsive="screen" :item-responsive="true">
      <!-- CPU -->
      <NGridItem span="4 s:2 m:1">
        <NCard :bordered="false" class="stat-card">
          <NSkeleton v-if="loading" :height="80" />
          <div v-else class="stat-inner">
            <div class="stat-progress">
              <NProgress
                type="circle"
                :percentage="cpuInfo?.usagePercentage ?? 0"
                :stroke-width="6"
                :indicator-text-color="'hsl(var(--foreground))'"
                :rail-color="'hsl(var(--muted) / 0.5)'"
                :color="(cpuInfo?.usagePercentage ?? 0) > 80 ? '#d03050' : '#18a058'"
                style="width: 64px; height: 64px;"
              />
            </div>
            <div class="stat-detail">
              <span class="stat-label">CPU 使用率</span>
              <span class="stat-value">{{ (cpuInfo?.usagePercentage ?? 0).toFixed(1) }}%</span>
              <span class="stat-sub">{{ cpuInfo?.processorName?.split(' ').slice(0, 3).join(' ') || '-' }}</span>
            </div>
          </div>
        </NCard>
      </NGridItem>

      <!-- 内存 -->
      <NGridItem span="4 s:2 m:1">
        <NCard :bordered="false" class="stat-card">
          <NSkeleton v-if="loading" :height="80" />
          <div v-else class="stat-inner">
            <div class="stat-progress">
              <NProgress
                type="circle"
                :percentage="memoryInfo?.usagePercentage ?? 0"
                :stroke-width="6"
                :indicator-text-color="'hsl(var(--foreground))'"
                :rail-color="'hsl(var(--muted) / 0.5)'"
                :color="(memoryInfo?.usagePercentage ?? 0) > 80 ? '#d03050' : '#2080f0'"
                style="width: 64px; height: 64px;"
              />
            </div>
            <div class="stat-detail">
              <span class="stat-label">内存使用</span>
              <span class="stat-value">{{ formatBytes(memoryInfo?.usedBytes ?? 0) }}</span>
              <span class="stat-sub">共 {{ formatBytes(memoryInfo?.totalBytes ?? 0) }}</span>
            </div>
          </div>
        </NCard>
      </NGridItem>

      <!-- 用户总数 -->
      <NGridItem span="4 s:2 m:1">
        <NCard :bordered="false" class="stat-card">
          <NSkeleton v-if="loading" :height="80" />
          <div v-else class="stat-inner">
            <div class="stat-icon" style="background: #eff6ff;">
              <NIcon size="24" color="#2080f0">
                <Icon icon="lucide:users" />
              </NIcon>
            </div>
            <div class="stat-detail">
              <span class="stat-label">用户总数</span>
              <span class="stat-value">
                <NNumberAnimation :from="0" :to="userTotal" :duration="1200" />
              </span>
              <span class="stat-sub">已注册用户</span>
            </div>
          </div>
        </NCard>
      </NGridItem>

      <!-- 在线会话 -->
      <NGridItem span="4 s:2 m:1">
        <NCard :bordered="false" class="stat-card">
          <NSkeleton v-if="loading" :height="80" />
          <div v-else class="stat-inner">
            <div class="stat-icon" style="background: #f0fdf4;">
              <NIcon size="24" color="#18a058">
                <Icon icon="lucide:shield-check" />
              </NIcon>
            </div>
            <div class="stat-detail">
              <span class="stat-label">在线会话</span>
              <span class="stat-value">
                <NNumberAnimation :from="0" :to="onlineCount" :duration="1200" />
              </span>
              <span class="stat-sub">当前活跃</span>
            </div>
          </div>
        </NCard>
      </NGridItem>
    </NGrid>

    <!-- 下半部分 -->
    <NGrid :x-gap="14" :y-gap="14" :cols="3" responsive="screen" :item-responsive="true">
      <!-- 运行环境 -->
      <NGridItem span="3 m:1">
        <NCard :bordered="false" class="section-card">
          <template #header>
            <div class="section-header">
              <NIcon size="16" class="section-icon">
                <Icon icon="lucide:server" />
              </NIcon>
              <span>运行环境</span>
            </div>
          </template>
          <NSkeleton v-if="loading" :height="200" />
          <div v-else class="env-list">
            <div class="env-item">
              <span class="env-key">操作系统</span>
              <span class="env-val">{{ runtimeInfo?.osDescription || '-' }}</span>
            </div>
            <div class="env-item">
              <span class="env-key">主机名称</span>
              <span class="env-val">{{ runtimeInfo?.machineName || '-' }}</span>
            </div>
            <div class="env-item">
              <span class="env-key">运行框架</span>
              <span class="env-val">{{ runtimeInfo?.frameworkDescription || '-' }}</span>
            </div>
            <div class="env-item">
              <span class="env-key">CLR 版本</span>
              <span class="env-val">{{ runtimeInfo?.clrVersion || '-' }}</span>
            </div>
            <div class="env-item">
              <span class="env-key">系统架构</span>
              <span class="env-val">{{ runtimeInfo?.osArchitecture || '-' }}</span>
            </div>
            <div class="env-item">
              <span class="env-key">处理器数</span>
              <span class="env-val">{{ runtimeInfo?.processorCount ?? '-' }}</span>
            </div>
            <div class="env-item">
              <span class="env-key">系统运行</span>
              <span class="env-val">{{ runtimeInfo?.systemUptime || '-' }}</span>
            </div>
            <div class="env-item">
              <span class="env-key">进程运行</span>
              <span class="env-val">{{ runtimeInfo?.processUptime || '-' }}</span>
            </div>
            <div class="env-item">
              <span class="env-key">工作内存</span>
              <span class="env-val">{{ formatBytes(runtimeInfo?.workingSet ?? 0) }}</span>
            </div>
          </div>

          <!-- 快捷入口 -->
          <div class="quick-section">
            <div class="section-header mb-3">
              <NIcon size="16" class="section-icon">
                <Icon icon="lucide:zap" />
              </NIcon>
              <span>快捷入口</span>
            </div>
            <div class="quick-grid">
              <button
                v-for="link in quickLinks"
                :key="link.label"
                type="button"
                class="quick-item"
                @click="router.push(link.to)"
              >
                <div class="quick-icon" :style="{ backgroundColor: `${link.color}18` }">
                  <NIcon size="18" :style="{ color: link.color }">
                    <Icon :icon="link.icon" />
                  </NIcon>
                </div>
                <span class="quick-label">{{ link.label }}</span>
              </button>
            </div>
          </div>
        </NCard>
      </NGridItem>

      <!-- 最近操作 -->
      <NGridItem span="3 m:2">
        <NCard :bordered="false" class="section-card">
          <template #header>
            <div class="section-header">
              <NIcon size="16" class="section-icon">
                <Icon icon="lucide:history" />
              </NIcon>
              <span>最近操作</span>
            </div>
          </template>
          <NSkeleton v-if="loading" :height="300" />
          <NTimeline v-else-if="recentLogs.length" class="log-timeline">
            <NTimelineItem
              v-for="log in recentLogs"
              :key="log.basicId"
            >
              <template #header>
                <div class="log-header">
                  <span class="log-user">{{ log.userName || '系统' }}</span>
                  <NTag :type="getOperationTag(log.operationType).type" size="small" :bordered="false">
                    {{ getOperationTag(log.operationType).label }}
                  </NTag>
                </div>
              </template>
              <div class="log-body">
                <p class="log-title">
                  {{ log.title || log.function || log.requestUrl }}
                </p>
                <div class="log-meta">
                  <span v-if="log.operationIp" class="log-meta-item">
                    <NIcon size="12"><Icon icon="lucide:map-pin" /></NIcon>
                    {{ log.operationIp }}
                  </span>
                  <span v-if="log.executionTime" class="log-meta-item">
                    <NIcon size="12"><Icon icon="lucide:timer" /></NIcon>
                    {{ log.executionTime }}ms
                  </span>
                  <span class="log-meta-item">
                    <NIcon size="12"><Icon icon="lucide:clock" /></NIcon>
                    {{ formatDate(log.operationTime || log.createdTime || '', 'MM-DD HH:mm') }}
                  </span>
                </div>
              </div>
            </NTimelineItem>
          </NTimeline>
          <div v-else class="empty-hint">
            暂无操作记录
          </div>
        </NCard>
      </NGridItem>
    </NGrid>
  </div>
</template>

<style scoped>
.dashboard-wrapper {
  display: flex;
  flex-direction: column;
  gap: 14px;
}

/* 欢迎卡片 */
.welcome-card {
  background: linear-gradient(135deg, hsl(var(--primary) / 0.08), hsl(var(--primary) / 0.03)) !important;
  border: 1px solid hsl(var(--primary) / 0.12) !important;
}

.welcome-title {
  font-size: 18px;
  font-weight: 600;
  color: hsl(var(--foreground));
  margin: 0;
}

.welcome-sub {
  margin-top: 4px;
  font-size: 13px;
  color: hsl(var(--muted-foreground));
}

.welcome-icon {
  display: none;
}

@media (min-width: 640px) {
  .welcome-icon {
    display: block;
  }
}

/* 统计卡片 */
.stat-card {
  transition: box-shadow 0.2s ease;
}

.stat-card:hover {
  box-shadow: 0 2px 12px hsl(var(--foreground) / 0.06);
}

.stat-inner {
  display: flex;
  align-items: center;
  gap: 14px;
}

.stat-progress {
  flex-shrink: 0;
}

.stat-icon {
  flex-shrink: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  width: 48px;
  height: 48px;
  border-radius: 12px;
}

.stat-detail {
  display: flex;
  flex-direction: column;
  min-width: 0;
}

.stat-label {
  font-size: 12px;
  color: hsl(var(--muted-foreground));
}

.stat-value {
  font-size: 20px;
  font-weight: 700;
  color: hsl(var(--foreground));
  line-height: 1.3;
}

.stat-sub {
  font-size: 11px;
  color: hsl(var(--muted-foreground));
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

/* 区块卡片 */
.section-card :deep(.n-card-header) {
  padding-bottom: 10px;
}

.section-header {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 14px;
  font-weight: 600;
  color: hsl(var(--foreground));
}

.section-icon {
  color: hsl(var(--primary));
}

/* 运行环境 */
.env-list {
  display: flex;
  flex-direction: column;
  gap: 0;
}

.env-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 7px 0;
  border-bottom: 1px dashed hsl(var(--border) / 0.6);
}

.env-item:last-child {
  border-bottom: none;
}

.env-key {
  font-size: 12px;
  color: hsl(var(--muted-foreground));
  flex-shrink: 0;
}

.env-val {
  font-size: 12px;
  color: hsl(var(--foreground));
  text-align: right;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  max-width: 65%;
}

/* 快捷入口 */
.quick-section {
  margin-top: 16px;
  padding-top: 14px;
  border-top: 1px solid hsl(var(--border) / 0.5);
}

.quick-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 8px;
}

.quick-item {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 6px;
  padding: 10px 4px;
  border: none;
  border-radius: 8px;
  background: transparent;
  cursor: pointer;
  transition: background 0.15s ease;
  outline: none;
}

.quick-item:hover {
  background: hsl(var(--accent));
}

.quick-icon {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 36px;
  height: 36px;
  border-radius: 10px;
}

.quick-label {
  font-size: 11px;
  color: hsl(var(--foreground));
}

/* 操作日志时间线 */
.log-timeline {
  padding-left: 2px;
}

.log-header {
  display: flex;
  align-items: center;
  gap: 8px;
}

.log-user {
  font-size: 13px;
  font-weight: 500;
  color: hsl(var(--foreground));
}

.log-body {
  margin-top: 2px;
}

.log-title {
  font-size: 12px;
  color: hsl(var(--foreground) / 0.8);
  margin: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.log-meta {
  display: flex;
  gap: 12px;
  margin-top: 4px;
  flex-wrap: wrap;
}

.log-meta-item {
  display: inline-flex;
  align-items: center;
  gap: 3px;
  font-size: 11px;
  color: hsl(var(--muted-foreground));
}

.empty-hint {
  display: flex;
  align-items: center;
  justify-content: center;
  height: 200px;
  color: hsl(var(--muted-foreground));
  font-size: 13px;
}
</style>
