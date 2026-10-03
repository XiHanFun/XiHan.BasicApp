<script setup lang="ts">
import type { NotificationListItemDto } from '@/api'
import { XhCarouselAutoplayTrigger, XhCarouselIndicator, XhCarouselIndicatorGroup, XhCarouselItem, XhCarouselList, XhCarouselNextTrigger, XhCarouselPrevTrigger, XhCarouselRoot, XhCarouselViewport, XhEmptyStateDescription, XhEmptyStateIndicator, XhEmptyStateRoot, XhEmptyStateTitle } from '@xihan-ui/vue'
import { computed, onMounted, ref } from 'vue'
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
/** 不止一条才轮播：只有一条时不自动翻页，也不画翻页、播放与指示点 */
const multiple = computed(() => announcements.value.length > 1)

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
  <!-- 张数由作者声明（组件库不数 DOM）：不给就是 0 张，指示点、翻页与自动播放全都不动 -->
  <XhCarouselRoot
    v-if="announcements.length"
    v-slot="{ totalPages }"
    :slide-count="announcements.length"
    :autoplay="multiple ? 5000 : false"
    loop
    class="announce-carousel"
  >
    <XhCarouselViewport>
      <XhCarouselList>
        <XhCarouselItem
          v-for="(item, slideIndex) in announcements"
          :key="item.basicId"
          :index="slideIndex"
        >
          <div
            class="carousel-slide"
            :class="{ 'carousel-slide--controls': multiple }"
            :style="slideStyle(item)"
            @click="openAnnouncement(item)"
          >
            <span class="slide-deco" :style="{ color: metaOf(item.notificationType).from }" style="display: inline-flex; font-size: 150px"><Icon icon="lucide:megaphone" /></span>
            <!-- 类型与发布时间同在顶上一行：下沿整条留给指示点与翻页钮，窄屏也不会压到文字上 -->
            <div class="slide-meta">
              <span class="slide-badge" :style="badgeStyle(item)">{{ metaOf(item.notificationType).label }}</span>
              <span v-if="item.sendTime" class="slide-time">
                <Icon width="13" height="13" icon="lucide:clock" />
                {{ formatDate(item.sendTime, 'YYYY-MM-DD HH:mm') }}
              </span>
            </div>
            <div class="slide-title">
              {{ item.title || t('workbench.dashboard.system_notice') }}
            </div>
            <div v-if="item.content" class="slide-content">
              {{ item.content }}
            </div>
          </div>
        </XhCarouselItem>
      </XhCarouselList>
    </XhCarouselViewport>

    <template v-if="multiple">
      <div class="carousel-arrows">
        <!-- 开了自动播放就必须给播放开关：它是唯一能停住自动翻页、且不会被悬停焦点重新拉起的入口 -->
        <XhCarouselAutoplayTrigger v-slot="{ stopped }" class="carousel-arrow">
          <Icon width="16" height="16" :icon="stopped ? 'lucide:play' : 'lucide:pause'" />
        </XhCarouselAutoplayTrigger>
        <XhCarouselPrevTrigger class="carousel-arrow">
          <Icon width="18" height="18" icon="lucide:arrow-left" />
        </XhCarouselPrevTrigger>
        <XhCarouselNextTrigger class="carousel-arrow">
          <Icon width="18" height="18" icon="lucide:arrow-right" />
        </XhCarouselNextTrigger>
      </div>

      <!-- 指示点交给组件库画：细指针是 8px 圆点、当前页拉长成带播放进度的胶囊；触屏下盒子撑成 44px 命中区、点改由伪元素画，
           所以这里只定位，不改点的尺寸与底色 -->
      <XhCarouselIndicatorGroup class="carousel-dots">
        <XhCarouselIndicator
          v-for="index of totalPages"
          :key="index"
          :index="index - 1"
        />
      </XhCarouselIndicatorGroup>
    </template>
  </XhCarouselRoot>
  <div v-else class="announce-empty">
    <XhEmptyStateRoot size="sm">
      <XhEmptyStateIndicator>
        <Icon icon="lucide:inbox" width="28" height="28" />
      </XhEmptyStateIndicator>
      <XhEmptyStateTitle>{{ t('common.no_data') }}</XhEmptyStateTitle>
      <XhEmptyStateDescription>{{ t('workbench.widgets.announcement.empty') }}</XhEmptyStateDescription>
    </XhEmptyStateRoot>
  </div>
</template>

<style scoped>
.announce-carousel {
  /* 下沿控件条：翻页 / 播放钮的边长，也是指示点那一行的高度 */
  --announce-control-size: var(--xh-space-8);

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
.announce-carousel :deep([data-scope='carousel']:is([data-part='viewport'], [data-part='list'], [data-part='item'])) {
  height: 100%;
}

.carousel-slide {
  position: relative;
  display: flex;
  flex-direction: column;
  justify-content: center;
  gap: var(--xh-space-2);
  width: 100%;
  height: 100%;
  padding: var(--xh-space-5) var(--xh-space-7);
  box-sizing: border-box;
  color: hsl(var(--foreground));
  cursor: pointer;
  user-select: none;
  overflow: hidden;
}

/* 有控件条时，正文底部让出控件条的高度与它的下边距，文字不会钻到钮与指示点底下 */
.carousel-slide--controls {
  padding-block-end: calc(var(--announce-control-size) + var(--xh-space-4) + var(--xh-space-2));
}

.slide-meta {
  position: relative;
  z-index: 1;
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: var(--xh-space-1) var(--xh-space-3);
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
  gap: var(--xh-space-1);
  font-size: 12px;
  color: hsl(var(--muted-foreground));
}

/* 自定义箭头：右下角 */
.carousel-arrows {
  position: absolute;
  right: var(--xh-space-4);
  bottom: var(--xh-space-4);
  display: flex;
  gap: var(--xh-space-2);
  z-index: 2;
}

/* 组件库把翻页 / 播放钮各自钉在轨道两端（absolute + 居中位移、48px 浮钮档），
   这里收回右下角一组里顺排；换面与按压缩放的过渡沿用 Action Control 家族那一套，不另写 */
.carousel-arrow {
  position: static;
  translate: none;
  display: flex;
  align-items: center;
  justify-content: center;
  width: var(--announce-control-size);
  min-inline-size: 0;
  height: var(--announce-control-size);
  color: hsl(var(--foreground));
  background: hsl(var(--card));
  border: 1px solid hsl(var(--border));
  border-radius: 8px;
  cursor: pointer;
}

.carousel-arrow:hover {
  background: hsl(var(--accent));
}

/* 指示点挪到左下角，与右下角的钮同一行、竖直居中，左边对齐正文（组件库缺省是下沿居中，那条 -50% 位移一并撤掉） */
.carousel-dots {
  inset-inline-start: var(--xh-space-7);
  inset-block-end: var(--xh-space-4);
  block-size: var(--announce-control-size);
  translate: none;
  z-index: 2;
}
</style>
