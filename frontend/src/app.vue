<script lang="ts" setup>
import { computed, onMounted, onUnmounted, watchEffect } from 'vue'
import { NConfigProvider, NDialogProvider, NMessageProvider, NNotificationProvider } from 'naive-ui'
import { useTheme, useNaiveLocale } from '~/hooks'
import { useAppStore } from '~/stores'
import { useAuthStore } from '@/store/auth'

defineOptions({ name: 'App' })

const appStore = useAppStore()
const authStore = useAuthStore()
const { isDark, naiveTheme, themeOverrides } = useTheme()
const { naiveLocale, naiveDateLocale } = useNaiveLocale()

const htmlClass = computed(() => (isDark.value ? 'dark' : 'light'))

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
  if (!appStore.shortcutEnable) return

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
    alert('锁屏功能待接入')
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
          <div
            v-if="appStore.watermarkEnabled"
            class="pointer-events-none fixed inset-0 z-[1999] overflow-hidden opacity-15"
          >
            <div
              class="absolute left-[-20%] top-[20%] w-[160%] rotate-[-20deg] select-none text-xs text-gray-500"
            >
              <div v-for="row in 12" :key="row" class="mb-10 whitespace-nowrap">
                <span v-for="col in 12" :key="`${row}-${col}`" class="mr-10">
                  {{ appStore.watermarkText }}
                </span>
              </div>
            </div>
          </div>
        </NDialogProvider>
      </NNotificationProvider>
    </NMessageProvider>
  </NConfigProvider>
</template>
