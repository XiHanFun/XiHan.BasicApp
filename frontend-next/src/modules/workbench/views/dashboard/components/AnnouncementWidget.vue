<script setup lang="ts">
import { XhCarouselAutoplayTrigger, XhCarouselIndicator, XhCarouselIndicatorGroup, XhCarouselItem, XhCarouselList, XhCarouselNextTrigger, XhCarouselPrevTrigger, XhCarouselRoot, XhCarouselViewport, XhEmptyStateDescription, XhEmptyStateIndicator, XhEmptyStateRoot, XhEmptyStateTitle } from '@xihan-ui/vue'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { Icon } from '~/iconify'
import { formatDate } from '~/utils'
import { useWidgetData } from './widget-data'

defineOptions({ name: 'AnnouncementWidget' })

interface Announcement { id: string, title: string, kind?: string | null, time?: string | null, link?: string | null }

/** 收件箱的入口（与壳层通知中心的「查看全部」同一处） */
const INBOX_PATH = '/notification/inbox'

const { t } = useI18n()
const router = useRouter()

// 轮播公告：本人收件箱里最近发布的几条（工作台 notification.announcements，与收件箱同一口径）
const { data, loading } = useWidgetData('notification.announcements')
const announcements = computed<Announcement[]>(() => data.value?.items ?? [])
/** 不止一条才轮播：只有一条时不自动翻页，也不画翻页、播放与指示点 */
const multiple = computed(() => announcements.value.length > 1)

/** 通知类型 → 标签 key + 主色。语气与通知管理页、收件箱的类型标签一致（系统 info、安全 warning、业务 success、
 *  紧急 danger），取组件库的语气色阶，跟着应用的状态色与深浅两态走；待办在标签里是 neutral，这里保留紫色好与系统类区分 */
interface TypeMeta { label: string, accent: string }
const DEFAULT_META: TypeMeta = { label: 'workbench.dashboard.type_system', accent: 'var(--xh-color-info-600)' }
const TYPE_META: Record<string, TypeMeta> = {
  System: DEFAULT_META,
  Security: { label: 'workbench.dashboard.type_security', accent: 'var(--xh-color-warning-600)' },
  Business: { label: 'workbench.dashboard.type_business', accent: 'var(--xh-color-success-600)' },
  Todo: { label: 'workbench.dashboard.type_todo', accent: 'var(--xh-color-purple-500)' },
  Emergency: { label: 'workbench.dashboard.type_emergency', accent: 'var(--xh-color-danger-600)' },
}

function metaOf(type?: string | null): TypeMeta {
  const meta = (type && TYPE_META[type]) || DEFAULT_META
  return { ...meta, label: t(meta.label) }
}

function slideStyle(item: Announcement) {
  const accent = metaOf(item.kind).accent
  // 浅色「纯色」背景：所有列同色，避免相邻幻灯片边缘透出导致的发丝缝
  return { background: `color-mix(in srgb, ${accent} 7%, var(--xh-bg-surface))` }
}

function badgeStyle(item: Announcement) {
  const accent = metaOf(item.kind).accent
  // 文字往前景色兑到 60%（组件库提示条的兑法）：深色态下不至于压在深底上发闷
  return {
    color: `color-mix(in oklab, ${accent} 60%, var(--xh-fg-default))`,
    background: `color-mix(in oklab, ${accent} 14%, transparent)`,
  }
}

function openAnnouncement(item: Announcement) {
  if (item.link) {
    if (/^https?:\/\//.test(item.link)) {
      window.open(item.link, '_blank', 'noopener,noreferrer')
    }
    else {
      void router.push(item.link)
    }
    return
  }
  void router.push(INBOX_PATH)
}
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
          :key="item.id"
          :index="slideIndex"
        >
          <div
            class="carousel-slide"
            :class="{ 'carousel-slide--controls': multiple }"
            :style="slideStyle(item)"
            @click="openAnnouncement(item)"
          >
            <span class="slide-deco" :style="{ color: metaOf(item.kind).accent }" style="display: inline-flex; font-size: 150px"><Icon icon="lucide:megaphone" /></span>
            <!-- 类型与发布时间同在顶上一行：下沿整条留给指示点与翻页钮，窄屏也不会压到文字上 -->
            <div class="slide-meta">
              <span class="slide-badge" :style="badgeStyle(item)">{{ metaOf(item.kind).label }}</span>
              <span v-if="item.time" class="slide-time">
                <Icon width="13" height="13" icon="lucide:clock" />
                {{ formatDate(item.time, 'YYYY-MM-DD HH:mm') }}
              </span>
            </div>
            <div class="slide-title">
              {{ item.title || t('workbench.dashboard.system_notice') }}
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
    <span v-if="loading" role="status">{{ t('common.loading') }}</span>
    <XhEmptyStateRoot v-else size="sm">
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

  block-size: calc(var(--xh-space-8) * 6);
  border: 1px solid hsl(var(--border));
  border-radius: var(--xh-shape-surface);
  background: hsl(var(--card));
  overflow: hidden;
}

.announce-empty {
  display: flex;
  align-items: center;
  justify-content: center;
  block-size: calc(var(--xh-space-8) * 6);
  border: 1px solid hsl(var(--border));
  border-radius: var(--xh-shape-surface);
  background: hsl(var(--card));
  color: var(--xh-fg-muted);
}

/* 只铺满高度；宽度交给轮播组件自身按像素计算
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
  right: calc(var(--xh-space-8) + var(--xh-space-2));
  top: 50%;
  transform: translateY(-50%) rotate(-8deg);
  opacity: 0.1;
  pointer-events: none;
}

.slide-badge {
  padding: var(--xh-space-0_5) var(--xh-space-2_5);
  font-size: var(--xh-font-size-xs);
  font-weight: var(--xh-font-weight-medium);
  border-radius: var(--xh-radius-full);
}

.slide-title {
  position: relative;
  z-index: 1;
  font-size: var(--xh-font-size-2xl);
  font-weight: var(--xh-font-weight-bold);
  letter-spacing: -0.01em;
  color: hsl(var(--foreground));
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  max-width: 82%;
}

.slide-time {
  display: flex;
  align-items: center;
  gap: var(--xh-space-1);
  font-size: var(--xh-font-size-xs);
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
  border-radius: var(--xh-radius-md);
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
