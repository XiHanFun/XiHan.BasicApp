<script setup lang="ts">
import type { AppUserInboxDisplayItem } from '~/types'
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { Icon } from '~/iconify'
import { useAppContext } from '~/stores'
import { NotificationContentFormat, NotificationPriority, NotificationType } from '~/types/enums'

defineOptions({ name: 'NotificationBanner' })

const { t } = useI18n()
const router = useRouter()
const appContext = useAppContext()

const banners = ref<AppUserInboxDisplayItem[]>([])

/** 高警示级别（优先级紧急 / 类型紧急）走警示色，其余走主色/信息色 */
function isAlert(item: AppUserInboxDisplayItem): boolean {
  return item.priority === NotificationPriority.Urgent
    || item.priority === NotificationPriority.High
    || item.notificationType === NotificationType.Emergency
}

/** 横幅按优先级（高警示在前）排序，便于「只突出最高一条」 */
const sortedBanners = computed(() =>
  [...banners.value].sort((a, b) => Number(isAlert(b)) - Number(isAlert(a))),
)

/** 当前置顶展示的一条（其余折叠为「还有 N 条」） */
const primaryBanner = computed<AppUserInboxDisplayItem | null>(() => sortedBanners.value[0] ?? null)
const restCount = computed(() => Math.max(0, sortedBanners.value.length - 1))

/** 内容首行（横幅只展示标题旁的一句话摘要，去除 Markdown/HTML 噪声不强求） */
function contentFirstLine(item: AppUserInboxDisplayItem): string {
  const raw = item.content ?? ''
  if (item.contentFormat === NotificationContentFormat.Html) {
    return raw.replace(/<[^>]+>/g, ' ').replace(/\s+/g, ' ').trim().split('\n')[0] ?? ''
  }
  return raw.replace(/\r/g, '').split('\n').find(line => line.trim().length > 0)?.trim() ?? ''
}

function resolveIcon(item: AppUserInboxDisplayItem): string {
  const icon = item.icon
  if (!icon) {
    return 'lucide:megaphone'
  }
  return icon.includes(':') ? icon : `lucide:${icon}`
}

function onDetail(item: AppUserInboxDisplayItem): void {
  if (item.link) {
    void router.push(item.link)
  }
}

function onClose(item: AppUserInboxDisplayItem): void {
  banners.value = banners.value.filter(b => b.basicId !== item.basicId)
}

onMounted(async () => {
  try {
    banners.value = await appContext.apis.userInboxApi.banner()
  }
  catch {
    // 横幅拉取失败静默：不渲染即可
  }
})
</script>

<template>
  <div v-if="primaryBanner" class="notif-banner-wrap">
    <div
      class="notif-banner"
      :class="isAlert(primaryBanner) ? 'notif-banner--alert' : 'notif-banner--info'"
    >
      <Icon :icon="resolveIcon(primaryBanner)" width="18" height="18" class="notif-banner__icon" />
      <div class="notif-banner__body">
        <span class="notif-banner__title">{{ primaryBanner.title }}</span>
        <span v-if="contentFirstLine(primaryBanner)" class="notif-banner__text">
          {{ contentFirstLine(primaryBanner) }}
        </span>
        <span v-if="restCount > 0" class="notif-banner__more">
          {{ t('header.notification.gate.banner_more', { n: restCount }) }}
        </span>
      </div>
      <button
        v-if="primaryBanner.link"
        type="button"
        class="notif-banner__action"
        @click="onDetail(primaryBanner)"
      >
        {{ t('header.notification.gate.banner_detail') }}
      </button>
      <button
        type="button"
        class="notif-banner__close"
        :title="t('header.notification.gate.banner_close')"
        :aria-label="t('header.notification.gate.banner_close')"
        @click="onClose(primaryBanner)"
      >
        <Icon icon="lucide:x" width="15" height="15" />
      </button>
    </div>
  </div>
</template>

<style scoped>
.notif-banner-wrap {
  flex: 0 0 auto;
  padding: 6px 12px 0;
}

.notif-banner {
  display: flex;
  align-items: center;
  gap: 8px;
  width: 100%;
  padding: 8px 12px;
  border-radius: 8px;
  border: 1px solid hsl(var(--border));
  background: hsl(var(--muted));
  color: hsl(var(--foreground));
}

.notif-banner--info {
  border-color: hsl(var(--primary) / 30%);
  background: hsl(var(--primary) / 8%);
}

.notif-banner--alert {
  border-color: hsl(var(--destructive) / 35%);
  background: hsl(var(--destructive) / 10%);
  color: hsl(var(--destructive));
}

.notif-banner__icon {
  flex-shrink: 0;
}

.notif-banner--info .notif-banner__icon {
  color: hsl(var(--primary));
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
  text-overflow: ellipsis;
  white-space: nowrap;
  color: hsl(var(--foreground) / 75%);
}

.notif-banner--alert .notif-banner__text {
  color: hsl(var(--destructive) / 85%);
}

.notif-banner__more {
  flex-shrink: 0;
  font-size: 12px;
  opacity: 0.7;
}

.notif-banner__action {
  flex-shrink: 0;
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
  width: 24px;
  height: 24px;
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
