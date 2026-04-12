<script lang="ts" setup>
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
import { Icon } from '~/iconify'
import { useAppContext, useUserStore } from '~/stores'
import { formatDate } from '~/utils'

defineOptions({ name: 'WorkspacePage' })

const router = useRouter()
const userStore = useUserStore()
const { apis } = useAppContext()

const loading = ref(true)
const userTotal = ref(0)
const onlineCount = ref(0)
const todayOperationCount = ref(0)
const todayAccessCount = ref(0)

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

const quickLinks = [
  { label: '用户管理', icon: 'lucide:users', to: '/system/user', color: '#18a058' },
  { label: '角色管理', icon: 'lucide:shield', to: '/system/role', color: '#2080f0' },
  { label: '菜单管理', icon: 'lucide:list-tree', to: '/platform/menu', color: '#f0a020' },
  { label: '部门管理', icon: 'lucide:building-2', to: '/system/department', color: '#8b5cf6' },
  { label: '操作日志', icon: 'lucide:history', to: '/log/oplog', color: '#d03050' },
  { label: '参数配置', icon: 'lucide:sliders-horizontal', to: '/platform/config', color: '#06b6d4' },
]

async function fetchDashboardData() {
  try {
    const [users, sessions, operations, accesses] = await Promise.allSettled([
      apis.userApi.page({ page: 1, pageSize: 1 }),
      apis.userSessionApi.page({ page: 1, pageSize: 1 }),
      apis.operationLogApi.page({ page: 1, pageSize: 1 }),
      apis.accessLogApi.page({ page: 1, pageSize: 1 }),
    ])

    if (users.status === 'fulfilled')
      userTotal.value = users.value.total
    if (sessions.status === 'fulfilled')
      onlineCount.value = sessions.value.total
    if (operations.status === 'fulfilled')
      todayOperationCount.value = operations.value.total
    if (accesses.status === 'fulfilled')
      todayAccessCount.value = accesses.value.total
  }
  finally {
    loading.value = false
  }
}

onMounted(fetchDashboardData)
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

    <!-- 统计卡片 -->
    <NGrid :x-gap="14" :y-gap="14" :cols="4" responsive="screen" :item-responsive="true">
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

      <NGridItem span="4 s:2 m:1">
        <NCard :bordered="false" class="stat-card">
          <NSkeleton v-if="loading" :height="80" />
          <div v-else class="stat-inner">
            <div class="stat-icon" style="background: #fef3f2;">
              <NIcon size="24" color="#d03050">
                <Icon icon="lucide:file-edit" />
              </NIcon>
            </div>
            <div class="stat-detail">
              <span class="stat-label">操作记录</span>
              <span class="stat-value">
                <NNumberAnimation :from="0" :to="todayOperationCount" :duration="1200" />
              </span>
              <span class="stat-sub">操作日志总量</span>
            </div>
          </div>
        </NCard>
      </NGridItem>

      <NGridItem span="4 s:2 m:1">
        <NCard :bordered="false" class="stat-card">
          <NSkeleton v-if="loading" :height="80" />
          <div v-else class="stat-inner">
            <div class="stat-icon" style="background: #fefce8;">
              <NIcon size="24" color="#f0a020">
                <Icon icon="lucide:globe" />
              </NIcon>
            </div>
            <div class="stat-detail">
              <span class="stat-label">访问记录</span>
              <span class="stat-value">
                <NNumberAnimation :from="0" :to="todayAccessCount" :duration="1200" />
              </span>
              <span class="stat-sub">访问日志总量</span>
            </div>
          </div>
        </NCard>
      </NGridItem>
    </NGrid>

    <!-- 快捷入口 -->
    <NCard :bordered="false" class="section-card">
      <template #header>
        <div class="section-header">
          <NIcon size="16" class="section-icon">
            <Icon icon="lucide:zap" />
          </NIcon>
          <span>快捷入口</span>
        </div>
      </template>
      <div class="quick-grid">
        <button
          v-for="link in quickLinks"
          :key="link.label"
          type="button"
          class="quick-item"
          @click="router.push(link.to)"
        >
          <div class="quick-icon" :style="{ backgroundColor: `${link.color}18` }">
            <NIcon size="20" :style="{ color: link.color }">
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
  flex-direction: column;
  align-items: center;
  gap: 8px;
  padding: 14px 8px;
  border: none;
  border-radius: 10px;
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
  width: 40px;
  height: 40px;
  border-radius: 12px;
}

.quick-label {
  font-size: 12px;
  color: hsl(var(--foreground));
}
</style>
