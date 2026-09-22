<script lang="ts" setup>
import PageLoader from '~/components/common/PageLoader.vue'
import { useAppStore, useTabbarStore } from '~/stores'
import { playPageEnter } from './page-motion'

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

      css=false：进场交给 WAAPI（见 page-motion.ts）。Vue 的类式过渡要在页面根上增删五次 class，
      而页面根是页内所有浮层的祖先，XiHan.UI 的 Portal 视觉环境桥盯着祖先链的 class，
      改一次就把整张自定义属性表重读一遍——实测一次 class 变更 6 万次 getPropertyValue、约 200ms。

      leave 当场收尾：本应用不播页面离场动画（新旧两页并存一帧会跳）。钩子要声明两个形参
      Vue 才认它接管收尾（hasExplicitCallback 看的是形参个数），同步调 done 即刻摘除旧页，
      KeepAlive 的 onDeactivated 也就不再压着不发。
    -->
    <RouterView v-slot="{ Component, route: currentRoute }">
      <template v-if="Component">
        <Transition
          :css="false"
          appear
          @enter="(el, done) => playPageEnter(el, transitionName, done)"
          @appear="(el, done) => playPageEnter(el, transitionName, done)"
          @leave="(_el, done) => done()"
        >
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

  /* 进场动画带 translateX(30px)，落在外层的滚动容器里会横向撑出一条滚动条。
     用 clip 而不是 hidden：hidden 会把另一根轴一并变成滚动容器，动到整层的滚动模型 */
  overflow-x: clip;
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
  transition: opacity var(--xh-motion-duration-enter) var(--xh-motion-ease-enter);
}

.xh-loading-fade-enter-from,
.xh-loading-fade-leave-to {
  opacity: 0;
}
</style>
