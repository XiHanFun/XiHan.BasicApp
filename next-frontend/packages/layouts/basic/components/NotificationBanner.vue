<script setup lang="ts">
import type { AppUserInboxDisplayItem } from '~/types'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { Icon } from '~/iconify'
import { NotificationContentFormat } from '~/types/enums'
import { resolveBannerTone, useBannerNotices } from '../composables/use-banner-notices'

defineOptions({ name: 'NotificationBanner' })

const { t } = useI18n()
const router = useRouter()
const { banners, active, activeIndex, paused, dismiss } = useBannerNotices()

/** 视觉类型 → 默认图标（公告项自带 icon 时优先） */
const TONE_ICON: Record<string, string> = {
  info: 'lucide:info',
  success: 'lucide:check-circle-2',
  warning: 'lucide:alert-triangle',
  error: 'lucide:alert-circle',
  primary: 'lucide:megaphone',
}

function resolveIcon(item: AppUserInboxDisplayItem): string {
  const icon = item.icon
  if (!icon) {
    return TONE_ICON[resolveBannerTone(item)] ?? 'lucide:megaphone'
  }
  return icon.includes(':') ? icon : `lucide:${icon}`
}

/** 内容首行（横幅只展示一句话摘要；Markdown/HTML 去噪不强求，详情走跳转） */
function contentFirstLine(item: AppUserInboxDisplayItem): string {
  const raw = item.content ?? ''
  if (item.contentFormat === NotificationContentFormat.Html) {
    return raw.replace(/<[^>]+>/g, ' ').replace(/\s+/g, ' ').trim().split('\n')[0] ?? ''
  }
  return raw.replace(/\r/g, '').split('\n').find(line => line.trim().length > 0)?.trim() ?? ''
}

/** 详情跳转：外链新窗口、内链路由 */
function onDetail(item: AppUserInboxDisplayItem): void {
  if (!item.link) {
    return
  }
  if (/^https?:\/\//.test(item.link)) {
    window.open(item.link, '_blank', 'noopener,noreferrer')
    return
  }
  void router.push(item.link)
}
</script>

<template>
  <Transition name="banner-slide">
    <div
      v-if="active"
      class="notif-banner-wrap"
      @mouseenter="paused = true"
      @mouseleave="paused = false"
    >
      <Transition name="banner-fade" mode="out-in">
        <div
          :key="active.basicId"
          class="notif-banner"
          :class="`notif-banner--${resolveBannerTone(active)}`"
        >
          <Icon :icon="resolveIcon(active)" width="16" height="16" class="notif-banner__icon" />
          <div class="notif-banner__body">
            <span class="notif-banner__title">{{ active.title }}</span>
            <span v-if="contentFirstLine(active)" class="notif-banner__text">
              {{ contentFirstLine(active) }}
            </span>
          </div>
          <!-- 多条：圆点指示 + 点击切换（自动 5s 轮播，悬停暂停） -->
          <div v-if="banners.length > 1" class="notif-banner__dots">
            <button
              v-for="(item, index) in banners"
              :key="item.basicId"
              type="button"
              class="notif-banner__dot"
              :class="{ 'is-active': index === activeIndex }"
              :aria-label="item.title"
              :title="item.title"
              @click="activeIndex = index"
            />
          </div>
          <button
            v-if="active.link"
            type="button"
            class="notif-banner__action"
            @click="onDetail(active)"
          >
            {{ t('header.notification.gate.banner_detail') }}
            <Icon icon="lucide:arrow-right" width="12" height="12" />
          </button>
          <button
            type="button"
            class="notif-banner__close"
            :title="t('header.notification.gate.banner_close')"
            :aria-label="t('header.notification.gate.banner_close')"
            @click="dismiss(active)"
          >
            <Icon icon="lucide:x" width="14" height="14" />
          </button>
        </div>
      </Transition>
    </div>
  </Transition>
</template>

<style scoped>
.notif-banner-wrap {
  flex: 0 0 auto;
  overflow: hidden;
  padding: 6px 12px 0;
}

/* 出现/关闭：slideDown / slideUp */
.banner-slide-enter-active,
.banner-slide-leave-active {
  transition:
    max-height 0.25s ease,
    opacity 0.25s ease,
    transform 0.25s ease;
  max-height: 48px;
}

.banner-slide-enter-from,
.banner-slide-leave-to {
  max-height: 0;
  opacity: 0;
  transform: translateY(-8px);
}

/* 多条切换：fade */
.banner-fade-enter-active,
.banner-fade-leave-active {
  transition: opacity 0.2s ease;
}

.banner-fade-enter-from,
.banner-fade-leave-to {
  opacity: 0;
}

/* 横幅本体：固定 36px 单行 */
.notif-banner {
  display: flex;
  align-items: center;
  gap: 8px;
  width: 100%;
  height: 36px;
  padding: 0 10px;
  border-radius: 8px;
  border: 1px solid;
}

/* 5 态配色（按通知类型/优先级映射） */
.notif-banner--info {
  border-color: #3b82f64d;
  background: #3b82f614;
  color: #1d4ed8;
}

.notif-banner--success {
  border-color: #22c55e4d;
  background: #22c55e14;
  color: #15803d;
}

.notif-banner--warning {
  border-color: #f59e0b55;
  background: #f59e0b16;
  color: #b45309;
}

.notif-banner--error {
  border-color: #ef44444d;
  background: #ef444414;
  color: #b91c1c;
}

.notif-banner--primary {
  border-color: hsl(var(--primary) / 30%);
  background: hsl(var(--primary) / 8%);
  color: hsl(var(--primary));
}

.notif-banner__icon {
  flex-shrink: 0;
}

.notif-banner__body {
  display: flex;
  min-width: 0;
  flex: 1;
  align-items: baseline;
  gap: 8px;
}

.notif-banner__title {
  flex-shrink: 0;
  font-weight: 600;
  font-size: 13px;
}

.notif-banner__text {
  overflow: hidden;
  flex: 1;
  min-width: 0;
  font-size: 13px;
  opacity: 0.75;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.notif-banner__dots {
  display: flex;
  flex-shrink: 0;
  align-items: center;
  gap: 5px;
}

.notif-banner__dot {
  width: 6px;
  height: 6px;
  padding: 0;
  border: none;
  border-radius: 999px;
  background: currentcolor;
  opacity: 0.25;
  cursor: pointer;
  transition:
    opacity 0.2s ease,
    transform 0.2s ease;
}

.notif-banner__dot.is-active {
  opacity: 0.9;
  transform: scale(1.25);
}

.notif-banner__action {
  display: inline-flex;
  flex-shrink: 0;
  align-items: center;
  gap: 3px;
  padding: 2px 8px;
  border: none;
  border-radius: 6px;
  background: transparent;
  color: currentcolor;
  font-size: 12px;
  font-weight: 600;
  cursor: pointer;
  transition: background 0.15s ease;
}

.notif-banner__action:hover {
  background: hsl(var(--foreground) / 8%);
}

.notif-banner__close {
  display: inline-flex;
  flex-shrink: 0;
  align-items: center;
  justify-content: center;
  width: 22px;
  height: 22px;
  padding: 0;
  border: none;
  border-radius: 6px;
  background: transparent;
  color: currentcolor;
  opacity: 0.6;
  cursor: pointer;
  transition:
    background 0.15s ease,
    opacity 0.15s ease;
}

.notif-banner__close:hover {
  opacity: 1;
  background: hsl(var(--foreground) / 8%);
}
</style>
