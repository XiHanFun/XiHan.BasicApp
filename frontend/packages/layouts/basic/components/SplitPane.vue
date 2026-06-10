<script lang="ts" setup>
import { computed, ref } from 'vue'
import { useSplitViewStore } from '~/stores'

defineOptions({ name: 'SplitPane' })

const splitView = useSplitViewStore()

/** iframe reload 令牌：变更 src 触发重载（由分割线工具组调用 reload 暴露方法） */
const reloadKey = ref(0)

/** 右侧 pane 的 iframe 源：目标路径 + 内容-only 模式（hash 路由） */
const paneSrc = computed(() => {
  const path = splitView.rightPath
  if (!path) {
    return ''
  }
  const base = `${window.location.origin}${window.location.pathname}`
  const sep = path.includes('?') ? '&' : '?'
  return `${base}#${path}${sep}__pane=1&__k=${reloadKey.value}`
})

function reload(): void {
  reloadKey.value += 1
}

defineExpose({ reload })
</script>

<template>
  <iframe
    v-if="paneSrc"
    :src="paneSrc"
    class="split-pane-frame"
    title="split-pane"
  />
</template>

<style scoped>
.split-pane-frame {
  display: block;
  width: 100%;
  height: 100%;
  border: none;
  background: hsl(var(--background));
}
</style>
