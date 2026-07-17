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
    <!--
      v-if：守卫重定向瞬态 Component 为空时不渲染。
      KeepAlive include：按路由名缓存已打开的标签（组件名已在 router/dynamic.ts 对齐路由名）。
      禁加 mode="out-in"：与 KeepAlive 组合有卡死缺陷（缓存页离场时 isLeaving 复位回调丢失
      → 此后所有页面永久空白）。Transition/KeepAlive 内部须严格单子节点，勿夹注释。
    -->
    <RouterView v-slot="{ Component, route: currentRoute }">
      <template v-if="Component">
        <Transition :name="transitionName" appear>
          <KeepAlive :include="tabbarStore.cachedTabNames">
            <component
              :is="Component"
              :key="`${currentRoute.fullPath}_${tabbarStore.getRefreshSeed(currentRoute.fullPath)}`"
            />
          </KeepAlive>
        </Transition>
      </template>
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

/* 统一禁用页面离场动画（覆盖所有动画风格），并避免新旧页并存一帧的跳动；排除加载遮罩 */
.layout-content-renderer > :deep([class*='-leave-active']:not(.content-loading)) {
  display: none !important;
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
