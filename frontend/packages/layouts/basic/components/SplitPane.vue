<script lang="ts" setup>
import type { Component } from 'vue'
import { NEmpty } from 'naive-ui'
import { computed, defineAsyncComponent, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useSplitViewStore } from '~/stores'

defineOptions({ name: 'SplitPane' })

const splitView = useSplitViewStore()
const router = useRouter()

/** 重载令牌：变更 key 触发组件重建（由分割线工具组调用 reload） */
const reloadKey = ref(0)

/** 懒加载路由组件 → 异步组件包装缓存（按原始 loader 引用缓存，避免重复包装导致反复重挂载） */
const asyncCache = new Map<unknown, Component>()

/**
 * 解析右侧路径对应的路由叶子组件，应用内直接渲染（同一应用实例，无 iframe、零重启零闪烁）。
 * 注：pane 内组件经 useRoute() 读到的是主路由；对自包含的列表页（pageCode 驱动）无影响。
 */
const paneComponent = computed<Component | null>(() => {
  const path = splitView.rightPath
  if (!path) {
    return null
  }
  const resolved = router.resolve(path)
  const record = resolved.matched[resolved.matched.length - 1]
  const raw = record?.components?.default
  if (!raw) {
    return null
  }
  // 懒加载路由（() => import(...)）：包装为异步组件；已是组件对象则直接用
  if (typeof raw === 'function' && !('render' in raw) && !('setup' in raw)) {
    let wrapped = asyncCache.get(raw)
    if (!wrapped) {
      wrapped = defineAsyncComponent(raw as () => Promise<Component>)
      asyncCache.set(raw, wrapped)
    }
    return wrapped
  }
  return raw as Component
})

function reload(): void {
  reloadKey.value += 1
}

defineExpose({ reload })
</script>

<template>
  <div class="split-pane-body">
    <component
      :is="paneComponent"
      v-if="paneComponent"
      :key="`${splitView.rightPath}#${reloadKey}`"
    />
    <NEmpty v-else description="无法加载该页面" class="py-12" />
  </div>
</template>

<style scoped>
/* 背景保持透明：与左侧主视图透出同一布局底色，避免左右空隙色不一致 */
.split-pane-body {
  height: 100%;
  width: 100%;
  overflow: auto;
}
</style>
