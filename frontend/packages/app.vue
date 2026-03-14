<script lang="ts" setup>
import {
  NConfigProvider,
  NDialogProvider,
  NLoadingBarProvider,
  NMessageProvider,
  NNotificationProvider,
} from 'naive-ui'
import AppWatermark from '~/components/common/AppWatermark.vue'
import LockScreen from '~/components/common/LockScreen.vue'
import { useGlobalShortcuts, useHtmlStyle } from '~/composables'
import { useNaiveLocale, useTheme } from '~/hooks'

defineOptions({ name: 'App' })

const { naiveTheme, themeOverrides } = useTheme()
const { naiveLocale, naiveDateLocale } = useNaiveLocale()

useHtmlStyle()
useGlobalShortcuts()
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
            <AppWatermark />
            <LockScreen />
          </NDialogProvider>
        </NNotificationProvider>
      </NMessageProvider>
    </NLoadingBarProvider>
  </NConfigProvider>
</template>
