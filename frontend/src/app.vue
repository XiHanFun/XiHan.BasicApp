<script lang="ts" setup>
import { computed, watchEffect } from 'vue'
import {
  NConfigProvider,
  NDialogProvider,
  NMessageProvider,
  NNotificationProvider,
} from 'naive-ui'
import { useTheme, useNaiveLocale } from '~/hooks'

defineOptions({ name: 'App' })

const { isDark, naiveTheme, themeOverrides } = useTheme()
const { naiveLocale, naiveDateLocale } = useNaiveLocale()

const htmlClass = computed(() => (isDark.value ? 'dark' : 'light'))

// 同步 html class 以支持 Tailwind dark mode
watchEffect(() => {
  document.documentElement.className = htmlClass.value
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
        </NDialogProvider>
      </NNotificationProvider>
    </NMessageProvider>
  </NConfigProvider>
</template>
