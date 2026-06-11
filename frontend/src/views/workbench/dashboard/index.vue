<script setup lang="ts">
import {
  NCard,
  NGrid,
  NGridItem,
  NIcon,
  NTag,
  NTimeline,
  NTimelineItem,
} from 'naive-ui'
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { workbenchApi } from '@/api'
import { usePermission } from '~/hooks'
import { Icon } from '~/iconify'
import { useUserStore } from '~/stores'
import { formatDate } from '~/utils'

defineOptions({ name: 'WorkbenchDashboardPage' })

const router = useRouter()
const userStore = useUserStore()
const { hasPermission } = usePermission()

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

// TODO: 确认各快捷入口的权限码与后台权限中心一致，当前为基于模块命名约定的占位码。
const quickLinkDefinitions = [
  { label: '站内信', desc: '查看系统消息', icon: 'lucide:inbox', to: '/workbench/inbox', color: '#3b82f6', permission: 'workbench:inbox:view' },
  { label: '用户管理', desc: '账号与权限', icon: 'lucide:users', to: '/system/user', color: '#22c55e', permission: 'saas:user:read' },
  { label: '角色管理', desc: '角色与授权', icon: 'lucide:shield-user', to: '/system/role', color: '#8b5cf6', permission: 'saas:role:read' },
  { label: '菜单管理', desc: '导航配置', icon: 'lucide:list-tree', to: '/platform/menu', color: '#f59e0b', permission: 'platform:menu:view' },
  { label: '日志管理', desc: '审计与追踪', icon: 'lucide:file-search', to: '/log/login', color: '#ef4444', permission: 'log:login:view' },
  { label: '个人中心', desc: '资料与安全', icon: 'lucide:user-round-cog', to: '/workbench/profile', color: '#06b6d4', permission: null },
] as const

const quickLinks = computed(() =>
  quickLinkDefinitions.filter(link => !link.permission || hasPermission(link.permission)),
)

// TODO: Replace with API call when backend provides ActivityFeed endpoint
// const recentActivities = await workbenchApi.dashboard.recentActivities()
const recentActivities = [
  { time: '10 分钟前', text: '超级管理员 更新了角色权限配置' },
  { time: '30 分钟前', text: '系统 执行了数据备份任务' },
  { time: '1 小时前', text: '超级管理员 创建了新用户' },
  { time: '2 小时前', text: '系统 完成日志归档' },
]

async function fetchDashboardData() {
  try {
    const summary = await workbenchApi.dashboard.summary()
    onlineTime.value = summary.statistics.onlineTime
    lastLoginTime.value = summary.statistics.lastLoginTime ?? null
    unreadCount.value = summary.inbox.unreadCount
    pendingConfirmCount.value = summary.inbox.pendingConfirmCount
  }
  catch {
    onlineTime.value = 0
    lastLoginTime.value = null
    unreadCount.value = 0
    pendingConfirmCount.value = 0
  }
}

function formatDuration(seconds: number) {
  if (seconds <= 0)
    return '0 分钟'
  const hours = Math.floor(seconds / 3600)
  const minutes = Math.floor((seconds % 3600) / 60)
  if (hours > 0)
    return `${hours} 小时 ${minutes} 分钟`
  return `${Math.max(minutes, 1)} 分钟`
}

onMounted(() => {
  fetchDashboardData()
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
          <button class="welcome-stat" type="button" @click="router.push('/workbench/inbox')">
            <span class="welcome-stat-value">{{ unreadCount }}</span>
            <span class="welcome-stat-label">未读消息</span>
          </button>
          <button class="welcome-stat" type="button" @click="router.push('/workbench/inbox')">
            <span class="welcome-stat-value">{{ pendingConfirmCount }}</span>
            <span class="welcome-stat-label">待确认</span>
          </button>
        </div>
      </div>
    </NCard>

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
  gap: 8px;
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
  padding: 8px 16px;
  border: none;
  border-radius: 10px;
  background: transparent;
  cursor: pointer;
  outline: none;
  transition: background 0.15s ease;
}

.welcome-stat:hover {
  background: hsl(var(--primary) / 0.08);
}

.welcome-stat-value {
  color: hsl(var(--foreground));
  font-size: 24px;
  font-weight: 700;
  font-variant-numeric: tabular-nums;
}

.welcome-stat-label {
  color: hsl(var(--muted-foreground));
  font-size: 11px;
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
  transition:
    background 0.15s ease,
    transform 0.15s ease;
  text-align: center;
}

.quick-item:hover {
  background: hsl(var(--accent));
  transform: translateY(-1px);
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
</style>
