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
      渲染核心：
      · v-if="Component"：守卫重定向的瞬态 / 未匹配路由时 Component 为空，不进入过渡，避免 out-in 对空节点卡死留白；
      · KeepAlive :include="cachedTabNames"：缓存「所有已打开且开了缓存的标签」。缓存标识统一用路由名：
        tab.name → cachedTabNames → include，页面被 router/dynamic.ts 包了一层「名字=路由名」的壳，
        故按名匹配永不落空——切走再切回不重渲染，关闭标签才销毁。
      注意：Transition / KeepAlive 各要求「严格唯一子节点」，其内部不可夹注释或多节点（否则编译报 expects exactly one child）。
    -->
    <RouterView v-slot="{ Component, route: currentRoute }">
      <template v-if="Component">
        <Transition :name="transitionName" mode="out-in" appear>
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
