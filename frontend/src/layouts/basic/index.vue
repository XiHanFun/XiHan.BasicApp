<script lang="ts" setup>
import { computed } from 'vue'
import { NLayout, NLayoutSider, NLayoutHeader, NLayoutContent } from 'naive-ui'
import { useAppStore } from '~/stores'
import AppSidebar from './components/AppSidebar.vue'
import AppHeader from './components/AppHeader.vue'
import AppTabbar from './components/AppTabbar.vue'

defineOptions({ name: 'BasicLayout' })

const appStore = useAppStore()
const collapsed = computed(() => appStore.sidebarCollapsed)
const showSider = computed(() => appStore.layoutMode !== 'top')
const siderWidth = computed(() => {
  if (collapsed.value) return 64
  return appStore.layoutMode === 'mix' ? 208 : 240
})
</script>

<template>
  <NLayout :has-sider="showSider" class="h-full bg-[var(--bg-base)]">
    <!-- 侧边栏 -->
    <NLayoutSider
      v-if="showSider"
      :width="siderWidth"
      :collapsed-width="64"
      :collapsed="collapsed"
      collapse-mode="width"
      bordered
      :native-scrollbar="false"
      class="transition-all duration-300"
    >
      <AppSidebar />
    </NLayoutSider>

    <!-- 主内容区 -->
    <NLayout class="flex flex-col bg-[var(--bg-base)]">
      <!-- 顶部导航 -->
      <NLayoutHeader bordered class="shrink-0 bg-[var(--header-bg)]">
        <AppHeader />
      </NLayoutHeader>
      <AppTabbar />

      <!-- 页面内容 -->
      <NLayoutContent
        class="flex-1 overflow-auto"
        :native-scrollbar="false"
        :content-style="{ padding: '16px', minHeight: '100%' }"
      >
        <div class="min-h-full rounded-lg">
          <RouterView v-slot="{ Component, route }">
            <Transition name="fade" mode="out-in">
              <KeepAlive :include="route.meta?.keepAlive ? [route.name as string] : []">
                <component :is="Component" :key="route.fullPath" />
              </KeepAlive>
            </Transition>
          </RouterView>
        </div>
      </NLayoutContent>
    </NLayout>
  </NLayout>
</template>
