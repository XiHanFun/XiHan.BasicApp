<script lang="ts" setup>
import { darkTheme, NConfigProvider, NDialogProvider, NGlobalStyle, NLoadingBarProvider, NMessageProvider, NNotificationProvider } from 'naive-ui'
import { computed, onMounted } from 'vue'
import { RouterView } from 'vue-router'
import AppWatermark from '~/components/common/AppWatermark.vue'
import DynamicIsland from '~/components/common/DynamicIsland.vue'
import LockScreen from '~/components/common/LockScreen.vue'
import { useGlobalShortcuts, useHtmlStyle } from '~/composables'
import { useNaiveLocale, useTheme } from '~/hooks'

defineOptions({ name: 'App' })

const { isDark, themeOverrides } = useTheme()
const naiveTheme = computed(() => (isDark.value ? darkTheme : undefined))

// naive-ui 内置组件文案（日期选择器星期/清除/此刻、分页「X / 页」等）随应用 locale 切换
const { naiveLocale, naiveDateLocale } = useNaiveLocale()

useHtmlStyle()
useGlobalShortcuts()

onMounted(() => {
  document.getElementById('app-loading')?.classList.add('hidden')
})
</script>

<template>
  <NConfigProvider
    :date-locale="naiveDateLocale"
    :locale="naiveLocale"
    :theme="naiveTheme"
    :theme-overrides="themeOverrides"
    class="h-full"
  >
    <NLoadingBarProvider>
      <NDialogProvider>
        <NNotificationProvider placement="bottom-right">
          <NMessageProvider>
            <RouterView />
            <LockScreen />
            <AppWatermark />
            <DynamicIsland />
          </NMessageProvider>
        </NNotificationProvider>
      </NDialogProvider>
    </NLoadingBarProvider>
    <NGlobalStyle />
  </NConfigProvider>
</template>
