<script setup lang="ts">
import type { NotificationListItemDto, UserInboxItemDto } from '@/api'
import {
  NCard,
  NCarousel,
  NEmpty,
  NGrid,
  NGridItem,
  NIcon,
  NTimeline,
  NTimelineItem,
} from 'naive-ui'
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { createPageRequest, notificationApi, NotificationType, workbenchApi } from '@/api'
import { Icon } from '~/iconify'
import { useFavoritesStore } from '~/stores'
import { formatDate } from '~/utils'

defineOptions({ name: 'WorkbenchDashboardPage' })

const router = useRouter()
const favoritesStore = useFavoritesStore()
const { t, te } = useI18n()

// ── 统计（全部来自后端 DashboardSummary，真实数据） ───────────────
const accessCount = ref(0)
const operationCount = ref(0)
const loginCount = ref(0)
const apiCallCount = ref(0)

// ── 最近动态（真实：站内信最新消息） ───────────────────────────
const latestItems = ref<UserInboxItemDto[]>([])

// ── 轮播公告（真实：已发布的「公告」类型通知） ─────────────────
const announcements = ref<NotificationListItemDto[]>([])

const statCards = computed(() => [
  { key: 'access', label: t('workbench.dashboard.stat_access'), value: accessCount.value, icon: 'lucide:mouse-pointer-click', color: '#3b82f6' },
  { key: 'operation', label: t('workbench.dashboard.stat_operation'), value: operationCount.value, icon: 'lucide:activity', color: '#22c55e' },
  { key: 'login', label: t('workbench.dashboard.stat_login'), value: loginCount.value, icon: 'lucide:log-in', color: '#8b5cf6' },
  { key: 'api', label: t('workbench.dashboard.stat_api'), value: apiCallCount.value, icon: 'lucide:webhook', color: '#f59e0b' },
])

// 快捷入口复用「收藏夹」数据：用户右键标签收藏的常用菜单
const QUICK_PALETTE = ['#3b82f6', '#22c55e', '#8b5cf6', '#f59e0b', '#ef4444', '#06b6d4', '#ec4899', '#14b8a6']

/** 收藏标题多为 i18n key（如 menu.user），渲染时翻译；非 key 原样展示 */
function displayTitle(title: string) {
  return te(title) ? t(title) : title
}

function resolveIcon(icon?: string | null) {
  if (!icon) {
    return 'lucide:bookmark'
  }
  return icon.includes(':') ? icon : `lucide:${icon}`
}

const quickLinks = computed(() =>
  favoritesStore.favorites.map((fav, index) => ({
    key: fav.path,
    label: displayTitle(fav.title),
    icon: resolveIcon(fav.icon),
    to: fav.path,
    color: QUICK_PALETTE[index % QUICK_PALETTE.length] ?? '#3b82f6',
  })),
)

/** 通知类型 → 标签 key + 渐变配色 */
interface TypeMeta { label: string, from: string, to: string }
const DEFAULT_META: TypeMeta = { label: 'workbench.dashboard.type_announcement', from: '#2563eb', to: '#4f46e5' }
const TYPE_META: Record<string, TypeMeta> = {
  Announcement: DEFAULT_META,
  System: { label: 'workbench.dashboard.type_system', from: '#0ea5e9', to: '#2563eb' },
  Warning: { label: 'workbench.dashboard.type_warning', from: '#f59e0b', to: '#ea580c' },
  Error: { label: 'workbench.dashboard.type_error', from: '#ef4444', to: '#b91c1c' },
  User: { label: 'workbench.dashboard.type_user', from: '#10b981', to: '#059669' },
}

function metaOf(type?: NotificationType | null): TypeMeta {
  const meta = (type && TYPE_META[type]) || DEFAULT_META
  return { ...meta, label: t(meta.label) }
}

function slideStyle(item: NotificationListItemDto) {
  const accent = metaOf(item.notificationType).from
  // 浅色「纯色」背景：所有列同色，避免相邻幻灯片边缘透出导致的发丝缝
  return { background: `color-mix(in srgb, ${accent} 7%, hsl(var(--card)))` }
}

function badgeStyle(item: NotificationListItemDto) {
  const accent = metaOf(item.notificationType).from
  return { color: accent, background: `color-mix(in srgb, ${accent} 14%, transparent)` }
}

function openAnnouncement(item: NotificationListItemDto) {
  if (item.link) {
    if (/^https?:\/\//.test(item.link)) {
      window.open(item.link, '_blank', 'noopener,noreferrer')
    }
    else {
      void router.push(item.link)
    }
    return
  }
  void router.push('/workbench/inbox')
}

function formatRelative(value?: string | null) {
  if (!value) {
    return '-'
  }
  const diff = Date.now() - new Date(value).getTime()
  const minutes = Math.floor(diff / 60000)
  if (minutes < 1) {
    return t('workbench.dashboard.just_now')
  }
  if (minutes < 60) {
    return t('workbench.dashboard.minutes_ago', { n: minutes })
  }
  const hours = Math.floor(minutes / 60)
  if (hours < 24) {
    return t('workbench.dashboard.hours_ago', { n: hours })
  }
  const days = Math.floor(hours / 24)
  if (days < 7) {
    return t('workbench.dashboard.days_ago', { n: days })
  }
  return formatDate(value, 'MM-DD HH:mm')
}

async function fetchDashboardData() {
  try {
    const summary = await workbenchApi.dashboard.summary()
    accessCount.value = summary.statistics.accessCount
    operationCount.value = summary.statistics.operationCount
    loginCount.value = summary.statistics.loginCount
    apiCallCount.value = summary.statistics.apiCallCount
    latestItems.value = summary.inbox.latestItems ?? []
  }
  catch {
    accessCount.value = 0
    operationCount.value = 0
    loginCount.value = 0
    apiCallCount.value = 0
    latestItems.value = []
  }
}

async function fetchAnnouncements() {
  try {
    // 与「通知公告」管理页同源：已发布的「公告」类型通知（非仅当前用户已收）
    const result = await notificationApi.page({
      ...createPageRequest({ page: { pageIndex: 1, pageSize: 6 } }),
      isPublished: true,
      notificationType: NotificationType.Announcement,
    })
    announcements.value = result.items ?? []
  }
  catch {
    announcements.value = []
  }
}

onMounted(() => {
  fetchDashboardData()
  fetchAnnouncements()
  // 跨端同步：拉取后端收藏夹覆盖本地（尽力而为，端点未就绪/离线静默回退本地）
  void favoritesStore.hydrate()
})
</script>

<template>
  <div class="dashboard">
    <!-- 轮播公告（真实通知数据，自定义箭头与控制点） -->
    <NCarousel
      v-if="announcements.length"
      autoplay
      show-arrow
      :interval="5000"
      class="dash-carousel"
    >
      <div
        v-for="item in announcements"
        :key="item.basicId"
        class="carousel-slide"
        :style="slideStyle(item)"
        @click="openAnnouncement(item)"
      >
        <NIcon class="slide-deco" :size="150" :style="{ color: metaOf(item.notificationType).from }">
          <Icon icon="lucide:megaphone" />
        </NIcon>
        <span class="slide-badge" :style="badgeStyle(item)">{{ metaOf(item.notificationType).label }}</span>
        <div class="slide-title">
          {{ item.title || t('workbench.dashboard.system_notice') }}
        </div>
        <div v-if="item.content" class="slide-content">
          {{ item.content }}
        </div>
        <div class="slide-time">
          <NIcon size="13">
            <Icon icon="lucide:clock" />
          </NIcon>
          {{ item.sendTime ? formatDate(item.sendTime, 'YYYY-MM-DD HH:mm') : '' }}
        </div>
      </div>

      <template #arrow="{ prev, next }">
        <div class="carousel-arrows">
          <button class="carousel-arrow" type="button" @click.stop="prev">
            <NIcon size="18">
              <Icon icon="lucide:arrow-left" />
            </NIcon>
          </button>
          <button class="carousel-arrow" type="button" @click.stop="next">
            <NIcon size="18">
              <Icon icon="lucide:arrow-right" />
            </NIcon>
          </button>
        </div>
      </template>

      <template #dots="{ total, currentIndex, to }">
        <ul class="carousel-dots">
          <li
            v-for="index of total"
            :key="index"
            class="carousel-dot"
            :class="{ 'is-active': currentIndex === index - 1 }"
            @click="to(index - 1)"
          />
        </ul>
      </template>
    </NCarousel>

    <!-- 真实统计卡片 -->
    <NGrid :cols="4" :item-responsive="true" :x-gap="14" :y-gap="14" responsive="screen">
      <NGridItem v-for="stat in statCards" :key="stat.key" span="2 m:1">
        <NCard :bordered="false" class="stat-card">
          <div class="stat-inner">
            <div class="stat-icon" :style="{ backgroundColor: `${stat.color}18` }">
              <NIcon :style="{ color: stat.color }" size="22">
                <Icon :icon="stat.icon" />
              </NIcon>
            </div>
            <div class="stat-text">
              <span class="stat-value">{{ stat.value }}</span>
              <span class="stat-label">{{ stat.label }}</span>
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
              <span>{{ t('workbench.dashboard.quick_entry') }}</span>
            </div>
          </template>
          <div v-if="quickLinks.length" class="quick-grid">
            <button
              v-for="link in quickLinks"
              :key="link.key"
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
              </div>
            </button>
          </div>
          <NEmpty v-else class="quick-empty" :description="t('workbench.dashboard.quick_empty')">
            <template #icon>
              <NIcon><Icon icon="lucide:star" /></NIcon>
            </template>
          </NEmpty>
        </NCard>
      </NGridItem>

      <NGridItem span="3 m:1">
        <NCard :bordered="false" class="section-card">
          <template #header>
            <div class="section-header">
              <NIcon class="section-icon" size="16">
                <Icon icon="lucide:clock" />
              </NIcon>
              <span>{{ t('workbench.dashboard.recent_activity') }}</span>
            </div>
          </template>
          <NTimeline v-if="latestItems.length">
            <NTimelineItem
              v-for="(item, index) in latestItems"
              :key="item.basicId"
              :color="index === 0 ? '#3b82f6' : undefined"
              :time="formatRelative(item.sendTime)"
              line-type="dashed"
            >
              <div class="activity-title">
                {{ item.title }}
              </div>
              <div v-if="item.content" class="activity-desc">
                {{ item.content }}
              </div>
            </NTimelineItem>
          </NTimeline>
          <NEmpty v-else class="activity-empty" :description="t('workbench.dashboard.activity_empty')">
            <template #icon>
              <NIcon><Icon icon="lucide:inbox" /></NIcon>
            </template>
          </NEmpty>
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

/* Carousel（浅色主题） */
.dash-carousel {
  height: 190px;
  border: 1px solid hsl(var(--border));
  border-radius: 12px;
  background: hsl(var(--card));
  overflow: hidden;
}

/* 只铺满高度；宽度交给 NCarousel 自身按像素计算
   （强行设 width:100% 会让 flex 轨道挤窄当前页、露出相邻幻灯片缝隙） */
.dash-carousel :deep(.n-carousel__slide) {
  height: 100%;
}

.carousel-slide {
  position: relative;
  display: flex;
  flex-direction: column;
  justify-content: center;
  gap: 10px;
  width: 100%;
  height: 100%;
  padding: 24px 28px;
  box-sizing: border-box;
  color: hsl(var(--foreground));
  cursor: pointer;
  user-select: none;
  overflow: hidden;
}

/* 右侧装饰大图标：填充空白、点缀但不抢内容 */
.slide-deco {
  position: absolute;
  right: 40px;
  top: 50%;
  transform: translateY(-50%) rotate(-8deg);
  opacity: 0.1;
  pointer-events: none;
}

.slide-badge {
  position: relative;
  z-index: 1;
  align-self: flex-start;
  padding: 2px 10px;
  font-size: 12px;
  font-weight: 500;
  border-radius: 999px;
}

.slide-title {
  position: relative;
  z-index: 1;
  font-size: 22px;
  font-weight: 700;
  letter-spacing: -0.01em;
  color: hsl(var(--foreground));
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  max-width: 82%;
}

.slide-content {
  position: relative;
  z-index: 1;
  font-size: 14px;
  line-height: 1.5;
  color: hsl(var(--muted-foreground));
  display: -webkit-box;
  -webkit-line-clamp: 2;
  line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
  max-width: 82%;
}

.slide-time {
  display: flex;
  align-items: center;
  gap: 4px;
  margin-top: 2px;
  font-size: 12px;
  color: hsl(var(--muted-foreground));
}

/* 自定义箭头：右下角 */
.carousel-arrows {
  position: absolute;
  right: 16px;
  bottom: 16px;
  display: flex;
  gap: 8px;
  z-index: 2;
}

.carousel-arrow {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 32px;
  height: 32px;
  color: hsl(var(--foreground));
  background: hsl(var(--card));
  border: 1px solid hsl(var(--border));
  border-radius: 8px;
  cursor: pointer;
  transition: background 0.15s ease;
}

.carousel-arrow:hover {
  background: hsl(var(--accent));
}

/* 自定义控制点：左下角 */
.carousel-dots {
  position: absolute;
  left: 24px;
  bottom: 22px;
  display: flex;
  gap: 6px;
  margin: 0;
  padding: 0;
  list-style: none;
  z-index: 2;
}

.carousel-dot {
  width: 16px;
  height: 4px;
  background: hsl(var(--border));
  border-radius: 999px;
  cursor: pointer;
  transition:
    width 0.3s ease,
    background 0.3s ease;
}

.carousel-dot.is-active {
  width: 28px;
  background: hsl(var(--primary));
}

/* Stat Cards */
.stat-card {
  border: 1px solid hsl(var(--border)) !important;
  border-radius: 12px !important;
}

.stat-card :deep(.n-card__content) {
  padding: 16px 18px !important;
}

.stat-inner {
  display: flex;
  align-items: center;
  gap: 14px;
}

.stat-icon {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 46px;
  height: 46px;
  border-radius: 12px;
  flex-shrink: 0;
}

.stat-text {
  display: flex;
  flex-direction: column;
  gap: 2px;
  min-width: 0;
}

.stat-value {
  color: hsl(var(--foreground));
  font-size: 24px;
  font-weight: 700;
  font-variant-numeric: tabular-nums;
  line-height: 1.1;
}

.stat-label {
  color: hsl(var(--muted-foreground));
  font-size: 12px;
}

/* Section Card */
.section-card {
  border: 1px solid hsl(var(--border)) !important;
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

.quick-empty {
  padding: 32px 0;
}

/* Recent Activity */
.activity-title {
  color: hsl(var(--foreground));
  font-size: 13px;
  font-weight: 500;
}

.activity-desc {
  margin-top: 2px;
  color: hsl(var(--muted-foreground));
  font-size: 12px;
  overflow: hidden;
  text-overflow: ellipsis;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  line-clamp: 2;
  -webkit-box-orient: vertical;
}

.activity-empty {
  padding: 28px 0;
}
</style>
