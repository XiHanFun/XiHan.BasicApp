<script setup lang="ts">
import type { NotificationListItemDto } from '@/api'
import { NCarousel, NEmpty, NIcon } from 'naive-ui'
import { onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { createPageRequest, notificationApi, NotificationType } from '@/api'
import { Icon } from '~/iconify'
import { formatDate } from '~/utils'

defineOptions({ name: 'AnnouncementWidget' })

const { t } = useI18n()
const router = useRouter()

// 轮播公告：已发布的「公告」类型通知（与「通知公告」管理页同源；无权限/接口失败静默为空）
const announcements = ref<NotificationListItemDto[]>([])

/** 通知类型 → 标签 key + 主色 */
interface TypeMeta { label: string, from: string, to: string }
const DEFAULT_META: TypeMeta = { label: 'workbench.dashboard.type_system', from: '#3b82f6', to: '#2563eb' }
const TYPE_META: Record<string, TypeMeta> = {
  System: DEFAULT_META,
  Security: { label: 'workbench.dashboard.type_security', from: '#f59e0b', to: '#ea580c' },
  Business: { label: 'workbench.dashboard.type_business', from: '#22c55e', to: '#059669' },
  Todo: { label: 'workbench.dashboard.type_todo', from: '#8b5cf6', to: '#6d28d9' },
  Emergency: { label: 'workbench.dashboard.type_emergency', from: '#ef4444', to: '#b91c1c' },
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

onMounted(async () => {
  try {
    const result = await notificationApi.page({
      ...createPageRequest({ page: { pageIndex: 1, pageSize: 6 } }),
      isPublished: true,
      notificationType: NotificationType.System,
    })
    announcements.value = result.items ?? []
  }
  catch {
    announcements.value = []
  }
})
</script>

<template>
  <NCarousel
    v-if="announcements.length"
    autoplay
    show-arrow
    :interval="5000"
    class="announce-carousel"
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
  <div v-else class="announce-empty">
    <NEmpty :description="t('workbench.widgets.announcement.empty')" />
  </div>
</template>

<style scoped>
.announce-carousel {
  height: 190px;
  border: 1px solid hsl(var(--border));
  border-radius: 12px;
  background: hsl(var(--card));
  overflow: hidden;
}

.announce-empty {
  display: flex;
  align-items: center;
  justify-content: center;
  height: 190px;
  border: 1px solid hsl(var(--border));
  border-radius: 12px;
  background: hsl(var(--card));
}

/* 只铺满高度；宽度交给 NCarousel 自身按像素计算
   （强行设 width:100% 会让 flex 轨道挤窄当前页、露出相邻幻灯片缝隙） */
.announce-carousel :deep(.n-carousel__slide) {
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
</style>
