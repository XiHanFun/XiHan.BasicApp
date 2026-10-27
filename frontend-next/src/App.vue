<script lang="ts" setup>
import { provideXhConfig } from '@xihan-ui/vue'
import { computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import AppWatermark from '~/components/common/AppWatermark.vue'
import DynamicIsland from '~/components/common/DynamicIsland.vue'
import LockScreen from '~/components/common/LockScreen.vue'
import { useGlobalShortcuts, useHtmlStyle } from '~/composables'
import { useXhUiConfig } from '~/hooks'
import { BasicLayout } from '~/layouts'
import AuthLayout from '~/views/_core/authentication/AuthLayout.vue'

defineOptions({ name: 'App' })

// 组件库的语言标记与内建文案（日期选择器的星期、分页 / 下拉的读屏名等）随应用 locale 切换；传的是 computed，切语言时组件跟着重渲
provideXhConfig(useXhUiConfig())
useHtmlStyle()
useGlobalShortcuts()

// 壳由路由决定：认证页走 AuthLayout，其余走管理端壳（守卫保证未登录到不了这里）
const route = useRoute()
const layout = computed(() => (route.meta.layout === 'auth' ? AuthLayout : BasicLayout))

onMounted(() => {
  document.getElementById('app-loading')?.classList.add('hidden')
})
</script>

<template>
  <div class="h-full">
    <component :is="layout" />
    <LockScreen />
    <AppWatermark />
    <DynamicIsland />
  </div>
</template>
