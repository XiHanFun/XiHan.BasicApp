<script setup lang="ts">
import {
  NCard,
  NGrid,
  NGridItem,
  NIcon,
  NNumberAnimation,
  NSkeleton,
} from 'naive-ui'
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { workbenchApi } from '@/api'
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

const greeting = computed(() => {
  const h = new Date().getHours()
  if (h < 6)
    return '夜深了'
  if (h < 9)
    return '早上好'
  if (h < 12)
    return '上午好'
  if (h < 14)
    return '中午好'
  if (h < 18)
    return '下午好'
  if (h < 22)
    return '晚上好'
  return '夜深了'
})

const onlineTimeText = computed(() => formatDuration(onlineTime.value))
const lastLoginText = computed(() => lastLoginTime.value ? formatDate(lastLoginTime.value) : '暂无记录')

const quickLinks = [
  { label: '站内信', icon: 'lucide:inbox', to: '/workbench/inbox', color: '#2080f0' },
  { label: '用户管理', icon: 'lucide:users', to: '/system/user', color: '#18a058' },
  { label: '角色管理', icon: 'lucide:shield', to: '/system/role', color: '#8b5cf6' },
  { label: '菜单管理', icon: 'lucide:list-tree', to: '/platform/menu', color: '#f0a020' },
  { label: '日志管理', icon: 'lucide:file-search', to: '/log/login', color: '#d03050' },
  { label: '个人中心', icon: 'lucide:user-round-cog', to: '/workbench/profile', color: '#06b6d4' },
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
    loginCount.value = 0
    accessCount.value = 0
    operationCount.value = 0
    apiCallCount.value = 0
    onlineTime.value = 0
    unreadCount.value = 0
    pendingConfirmCount.value = 0
    lastLoginTime.value = null
  }
  finally {
    loading.value = false
  }
}

function formatDuration(seconds: number) {
  if (seconds <= 0) {
    return '0 分钟'
  }

  const hours = Math.floor(seconds / 3600)
  const minutes = Math.floor((seconds % 3600) / 60)
  if (hours > 0) {
    return `${hours} 小时 ${minutes} 分钟`
  }
  return `${Math.max(minutes, 1)} 分钟`
}

onMounted(fetchDashboardData)
</script>

<template>
  <div class="dashboard-wrapper">
    <NCard :bordered="false" class="welcome-card">
      <div class="welcome-inner">
        <div>
          <h2 class="welcome-title">
            {{ greeting }}，{{ userStore.nickname || userStore.username }}
          </h2>
          <p class="welcome-sub">
            {{ formatDate(new Date(), 'YYYY年MM月DD日 dddd') }}
          </p>
        </div>
        <div class="welcome-meta">
          <span>今日在线时长 {{ onlineTimeText }}</span>
          <span>最后登录 {{ lastLoginText }}</span>
          <span>未读消息 {{ unreadCount }} 条，待确认 {{ pendingConfirmCount }} 条</span>
        </div>
      </div>
    </NCard>

    <NGrid :cols="4" :item-responsive="true" :x-gap="14" :y-gap="14" responsive="screen">
      <NGridItem span="4 s:2 m:1">
        <NCard :bordered="false" class="stat-card">
          <NSkeleton v-if="loading" :height="80" />
          <div v-else class="stat-inner">
            <div class="stat-icon" style="background: #eff6ff;">
              <NIcon color="#2080f0" size="24">
                <Icon icon="lucide:log-in" />
              </NIcon>
            </div>
            <div class="stat-detail">
              <span class="stat-label">登录次数</span>
              <span class="stat-value">
                <NNumberAnimation :duration="1200" :from="0" :to="loginCount" />
              </span>
              <span class="stat-sub">今日登录行为</span>
            </div>
          </div>
        </NCard>
      </NGridItem>

      <NGridItem span="4 s:2 m:1">
        <NCard :bordered="false" class="stat-card">
          <NSkeleton v-if="loading" :height="80" />
          <div v-else class="stat-inner">
            <div class="stat-icon" style="background: #f0fdf4;">
              <NIcon color="#18a058" size="24">
                <Icon icon="lucide:activity" />
              </NIcon>
            </div>
            <div class="stat-detail">
              <span class="stat-label">访问次数</span>
              <span class="stat-value">
                <NNumberAnimation :duration="1200" :from="0" :to="accessCount" />
              </span>
              <span class="stat-sub">今日访问行为</span>
            </div>
          </div>
        </NCard>
      </NGridItem>

      <NGridItem span="4 s:2 m:1">
        <NCard :bordered="false" class="stat-card">
          <NSkeleton v-if="loading" :height="80" />
          <div v-else class="stat-inner">
            <div class="stat-icon" style="background: #fef3f2;">
              <NIcon color="#d03050" size="24">
                <Icon icon="lucide:clipboard-list" />
              </NIcon>
            </div>
            <div class="stat-detail">
              <span class="stat-label">操作次数</span>
              <span class="stat-value">
                <NNumberAnimation :duration="1200" :from="0" :to="operationCount" />
              </span>
              <span class="stat-sub">今日业务操作</span>
            </div>
          </div>
        </NCard>
      </NGridItem>

      <NGridItem span="4 s:2 m:1">
        <NCard :bordered="false" class="stat-card">
          <NSkeleton v-if="loading" :height="80" />
          <div v-else class="stat-inner">
            <div class="stat-icon" style="background: #fefce8;">
              <NIcon color="#f0a020" size="24">
                <Icon icon="lucide:globe" />
              </NIcon>
            </div>
            <div class="stat-detail">
              <span class="stat-label">API 调用</span>
              <span class="stat-value">
                <NNumberAnimation :duration="1200" :from="0" :to="apiCallCount" />
              </span>
              <span class="stat-sub">今日接口调用</span>
            </div>
          </div>
        </NCard>
      </NGridItem>
    </NGrid>

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
            <NIcon :style="{ color: link.color }" size="20">
              <Icon :icon="link.icon" />
            </NIcon>
          </div>
          <span class="quick-label">{{ link.label }}</span>
        </button>
      </div>
    </NCard>
  </div>
</template>

<style scoped>
.dashboard-wrapper {
  display: flex;
  flex-direction: column;
  gap: 14px;
  padding: 16px;
}

.welcome-card {
  background: linear-gradient(135deg, hsl(var(--primary) / 0.08), hsl(var(--primary) / 0.03)) !important;
  border: 1px solid hsl(var(--primary) / 0.12) !important;
}

.welcome-inner {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
}

.welcome-title {
  margin: 0;
  color: hsl(var(--foreground));
  font-size: 18px;
  font-weight: 600;
}

.welcome-sub {
  margin-top: 4px;
  color: hsl(var(--muted-foreground));
  font-size: 13px;
}

.welcome-meta {
  display: none;
  flex-direction: column;
  gap: 4px;
  color: hsl(var(--muted-foreground));
  font-size: 12px;
  text-align: right;
}

@media (min-width: 768px) {
  .welcome-meta {
    display: flex;
  }
}

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

.stat-icon {
  display: flex;
  flex-shrink: 0;
  align-items: center;
  justify-content: center;
  width: 48px;
  height: 48px;
  border-radius: 12px;
}

.stat-detail {
  display: flex;
  min-width: 0;
  flex-direction: column;
}

.stat-label {
  color: hsl(var(--muted-foreground));
  font-size: 12px;
}

.stat-value {
  color: hsl(var(--foreground));
  font-size: 20px;
  font-weight: 700;
  line-height: 1.3;
}

.stat-sub {
  overflow: hidden;
  color: hsl(var(--muted-foreground));
  font-size: 11px;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.section-card :deep(.n-card-header) {
  padding-bottom: 10px;
}

.section-header {
  display: flex;
  align-items: center;
  gap: 6px;
  color: hsl(var(--foreground));
  font-size: 14px;
  font-weight: 600;
}

.section-icon {
  color: hsl(var(--primary));
}

.quick-grid {
  display: grid;
  grid-template-columns: repeat(6, 1fr);
  gap: 10px;
}

@media (max-width: 768px) {
  .quick-grid {
    grid-template-columns: repeat(3, 1fr);
  }
}

.quick-item {
  display: flex;
  align-items: center;
  flex-direction: column;
  gap: 8px;
  padding: 14px 8px;
  border: none;
  border-radius: 10px;
  background: transparent;
  cursor: pointer;
  outline: none;
  transition: background 0.15s ease;
}

.quick-item:hover {
  background: hsl(var(--accent));
}

.quick-icon {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 40px;
  height: 40px;
  border-radius: 12px;
}

.quick-label {
  color: hsl(var(--foreground));
  font-size: 12px;
}
</style>
