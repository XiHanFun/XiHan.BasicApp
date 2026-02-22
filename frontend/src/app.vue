<script lang="ts" setup>
import { NConfigProvider, NDialogProvider, NLoadingBarProvider, NMessageProvider, NNotificationProvider } from 'naive-ui'
import { computed, onMounted, onUnmounted, ref, watchEffect } from 'vue'
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

const lockScreenVisible = ref(false)

function handleGlobalShortcuts(e: KeyboardEvent) {
  if (!appStore.shortcutEnable)
    return

  // Ctrl/Cmd + K：全局搜索
  if (appStore.shortcutSearch && (e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'k') {
    e.preventDefault()
    window.dispatchEvent(new Event('xihan-open-global-search'))
    return
  }

  // Alt + Q：退出登录
  if (appStore.shortcutLogout && e.altKey && e.key.toLowerCase() === 'q') {
    e.preventDefault()
    authStore.logout()
    return
  }

  // Alt + L：锁屏
  if (appStore.shortcutLock && e.altKey && e.key.toLowerCase() === 'l') {
    e.preventDefault()
    lockScreenVisible.value = true
  }
}

function handleLockScreenRequest() {
  if (appStore.widgetLockScreen) {
    lockScreenVisible.value = true
  }
}

onMounted(() => {
  window.addEventListener('keydown', handleGlobalShortcuts)
  window.addEventListener('xihan-lock-screen', handleLockScreenRequest)
})

onUnmounted(() => {
  window.removeEventListener('keydown', handleGlobalShortcuts)
  window.removeEventListener('xihan-lock-screen', handleLockScreenRequest)
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
    <NLoadingBarProvider>
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
            <!-- 锁屏遮罩 -->
            <Teleport to="body">
              <div
                v-if="lockScreenVisible"
                class="lock-screen-mask"
                @click.self="lockScreenVisible = false"
              >
                <div class="lock-screen-card">
                  <div class="mb-4 text-base font-semibold text-[hsl(var(--foreground))]">
                    屏幕已锁定
                  </div>
                  <p class="mb-4 text-sm text-[hsl(var(--muted-foreground))]">
                    点击空白处或按 Esc 解锁
                  </p>
                  <button
                    class="unlock-btn"
                    @click="lockScreenVisible = false"
                  >
                    解锁
                  </button>
                </div>
              </div>
            </Teleport>
          </NDialogProvider>
        </NNotificationProvider>
      </NMessageProvider>
    </NLoadingBarProvider>
  </NConfigProvider>
</template>

<style>
.watermark-layer {
  pointer-events: none;
  position: fixed;
  inset: 0;
  z-index: 1999;
  background-repeat: repeat;
  user-select: none;
}

.lock-screen-mask {
  position: fixed;
  inset: 0;
  z-index: 9999;
  display: flex;
  align-items: center;
  justify-content: center;
  background: hsl(220 16% 6% / 0.85);
  backdrop-filter: blur(12px);
  -webkit-backdrop-filter: blur(12px);
}

.lock-screen-card {
  padding: 32px 40px;
  border-radius: 16px;
  background: hsl(var(--card));
  border: 1px solid hsl(var(--border));
  text-align: center;
  box-shadow: 0 20px 60px hsl(0 0% 0% / 0.3);
}

.unlock-btn {
  padding: 8px 28px;
  border-radius: 8px;
  background: hsl(var(--primary));
  color: hsl(var(--primary-foreground));
  font-size: 14px;
  font-weight: 500;
  cursor: pointer;
  transition: opacity 0.15s ease;
}

.unlock-btn:hover {
  opacity: 0.88;
}
</style>
