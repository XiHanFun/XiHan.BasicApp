<script lang="ts" setup>
import PageLoader from '~/components/common/PageLoader.vue'
import { useAppStore, useTabbarStore } from '~/stores'

interface LayoutContentRendererProps {
  transitionName: string
}

defineOptions({ name: 'LayoutContentRenderer' })

defineProps<LayoutContentRendererProps>()

const tabbarStore = useTabbarStore()
const appStore = useAppStore()
</script>

<template>
  <div class="layout-content-renderer">
    <RouterView v-slot="{ Component, route: currentRoute }">
      <Transition :name="transitionName" mode="out-in" appear>
        <KeepAlive :include="currentRoute.meta?.keepAlive ? [currentRoute.name as string] : []">
          <component
            :is="Component"
            :key="`${currentRoute.fullPath}_${tabbarStore.getRefreshSeed(currentRoute.fullPath)}`"
          />
        </KeepAlive>
      </Transition>
    </RouterView>

    <!-- 页面切换 Loading：开启后导航期间在内容区居中展示所选加载动画 -->
    <Transition name="xh-loading-fade">
      <div v-if="appStore.transitionLoading && appStore.pageLoading" class="content-loading">
        <PageLoader :name="appStore.loadingName" :size="76" :fixed-color="appStore.loadingFixedColor" />
      </div>
    </Transition>
  </div>
</template>

<style scoped>
.layout-content-renderer {
  position: relative;
  height: 100%;
}

.content-loading {
  position: absolute;
  inset: 0;
  z-index: 6;
  display: grid;
  place-items: center;
  background: hsl(var(--background) / 0.55);
  backdrop-filter: blur(1px);
}

.xh-loading-fade-enter-active,
.xh-loading-fade-leave-active {
  transition: opacity 0.2s ease;
}

.xh-loading-fade-enter-from,
.xh-loading-fade-leave-to {
  opacity: 0;
}
</style>
