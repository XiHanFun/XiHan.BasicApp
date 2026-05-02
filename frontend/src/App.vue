<script lang="ts" setup>
import { darkTheme, dateZhCN, NConfigProvider, NDialogProvider, NGlobalStyle, NLoadingBarProvider, NMessageProvider, NNotificationProvider, zhCN } from 'naive-ui'
import { computed, onMounted } from 'vue'
import { RouterView } from 'vue-router'
import LockScreen from '~/components/common/LockScreen.vue'
import { useGlobalShortcuts, useHtmlStyle } from '~/composables'
import { useTheme } from '~/hooks'

defineOptions({ name: 'App' })

const { isDark, themeOverrides } = useTheme()
const naiveTheme = computed(() => (isDark.value ? darkTheme : undefined))

useHtmlStyle()
useGlobalShortcuts()

onMounted(() => {
  document.getElementById('app-loading')?.classList.add('hidden')
})
</script>

<template>
  <NConfigProvider
    :date-locale="dateZhCN"
    :locale="zhCN"
    :theme="naiveTheme"
    :theme-overrides="themeOverrides"
    class="h-full"
  >
    <NLoadingBarProvider>
      <NDialogProvider>
        <NNotificationProvider>
          <NMessageProvider>
            <RouterView />
            <LockScreen />
          </NMessageProvider>
        </NNotificationProvider>
      </NDialogProvider>
    </NLoadingBarProvider>
    <NGlobalStyle />
  </NConfigProvider>
</template>
