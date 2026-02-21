<script lang="ts" setup>
import { NConfigProvider, NDialogProvider, NMessageProvider, NNotificationProvider } from 'naive-ui'
import { computed, onMounted, onUnmounted, watchEffect } from 'vue'
import { useAuthStore } from '@/store/auth'
import { useNaiveLocale, useTheme } from '~/hooks'
import { useAppStore } from '~/stores'

defineOptions({ name: 'App' })

const appStore = useAppStore()
const authStore = useAuthStore()
const { isDark, naiveTheme, themeOverrides } = useTheme()
const { naiveLocale, naiveDateLocale } = useNaiveLocale()

const htmlClass = computed(() => (isDark.value ? 'dark' : 'light'))

// 水印：动态生成 SVG data URI，全屏 repeat 平铺
const watermarkStyle = computed(() => {
  const text = appStore.watermarkText || 'XiHan BasicApp'
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="200" height="140">
    <text transform="rotate(-25, 100, 70)" x="10" y="80"
      fill="#808080" fill-opacity="0.18" font-size="13" font-family="sans-serif">
      ${text}
    </text>
  </svg>`
  const encoded = `url("data:image/svg+xml,${encodeURIComponent(svg)}")`
  return { backgroundImage: encoded, backgroundSize: '200px 140px' }
})

// 同步 html class 以支持 Tailwind dark mode
watchEffect(() => {
  document.documentElement.className = htmlClass.value
  document.documentElement.style.filter = appStore.grayscaleEnabled
    ? 'grayscale(100%)'
    : appStore.colorWeaknessEnabled
      ? 'invert(0.8) hue-rotate(180deg)'
      : ''
  document.documentElement.style.fontSize = `${appStore.fontSize}px`
})

function handleGlobalShortcuts(e: KeyboardEvent) {
  if (!appStore.shortcutEnable)
    return

  if (appStore.shortcutSearch && (e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'k') {
    e.preventDefault()
    window.dispatchEvent(new Event('xihan-open-global-search'))
  }

  if (appStore.shortcutLogout && e.ctrlKey && e.shiftKey && e.key.toLowerCase() === 'q') {
    e.preventDefault()
    authStore.logout()
  }

  if (appStore.shortcutLock && e.ctrlKey && e.key.toLowerCase() === 'l') {
    e.preventDefault()
    // TODO: 锁屏功能待接入
  }
}

onMounted(() => {
  window.addEventListener('keydown', handleGlobalShortcuts)
})

onUnmounted(() => {
  window.removeEventListener('keydown', handleGlobalShortcuts)
})
</script>

<template>
  <NConfigProvider
    :locale="naiveLocale"
    :date-locale="naiveDateLocale"
    :theme="naiveTheme"
    :theme-overrides="themeOverrides"
    class="h-full"
  >
    <NMessageProvider placement="top" :duration="3000" :max="5">
      <NNotificationProvider placement="top-right">
        <NDialogProvider>
          <RouterView />
          <!-- 水印：SVG data URI repeat 平铺，全屏均匀分布 -->
          <div
            v-if="appStore.watermarkEnabled"
            class="watermark-layer"
            :style="watermarkStyle"
          />
        </NDialogProvider>
      </NNotificationProvider>
    </NMessageProvider>
  </NConfigProvider>
</template>

<style>
/**
 * 水印：使用 CSS background-image + repeating-linear-gradient 实现全屏均匀分布。
 * 通过 SVG data URI 生成单格水印，然后 repeat 平铺到整个视口。
 * 优势：不依赖 JS 循环，不受内容区域影响，真正覆盖整个 fixed 层。
 */
.watermark-layer {
  pointer-events: none;
  position: fixed;
  inset: 0;
  z-index: 1999;
  /* backgroundImage 和 backgroundSize 由 computed watermarkStyle 动态注入 */
  background-repeat: repeat;
  user-select: none;
}
</style>
