<script lang="ts" setup>
import { provideXhConfig } from '@xihan-ui/vue'
import { onMounted } from 'vue'
import { RouterView } from 'vue-router'
import AppWatermark from '~/components/common/AppWatermark.vue'
import DynamicIsland from '~/components/common/DynamicIsland.vue'
import LockScreen from '~/components/common/LockScreen.vue'
import { useGlobalShortcuts, useHtmlStyle } from '~/composables'
import { useXhUiConfig } from '~/hooks'

defineOptions({ name: 'App' })

// 组件库的语言标记与内建文案（日期选择器的星期、分页/下拉的读屏名等）随应用 locale 切换。
// 传的是 computed，切语言时组件跟着重渲。
provideXhConfig(useXhUiConfig())

useHtmlStyle()
useGlobalShortcuts()

onMounted(() => {
  document.getElementById('app-loading')?.classList.add('hidden')
})
</script>

<template>
  <div class="h-full">
    <RouterView />
    <LockScreen />
    <AppWatermark />
    <DynamicIsland />
  </div>
</template>
