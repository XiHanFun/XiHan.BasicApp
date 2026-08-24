<script setup lang="ts">
import { onMounted, ref, shallowRef, watch } from 'vue'
import { getScrollRoot } from '~/composables/useScrollRoot'
import { Icon } from '~/iconify'

defineOptions({ name: 'XihanBackTop' })

const props = withDefaults(defineProps<XihanBackTopProps>(), {
  scrollY: 0,
})

interface XihanBackTopProps {
  /** 布局报来的滚动量，用作重新认领滚动容器的信号 */
  scrollY?: number
}

// 真正在滚的那个容器由布局登记，交给组件库当滚动源
const target = shallowRef<HTMLElement | null>(getScrollRoot())
// 组件库的滚动观察在挂载那一刻认定容器，容器换人就换 key 让它重挂
const trackKey = ref(0)

function claimScrollRoot() {
  const next = getScrollRoot()
  if (next === target.value)
    return
  target.value = next
  trackKey.value += 1
}

onMounted(claimScrollRoot)
watch(() => props.scrollY, claimScrollRoot)
</script>

<template>
  <XhBackTopRoot :key="trackKey" :target="target">
    <XhBackTopTrigger>
      <Icon icon="lucide:chevron-up" width="18" height="18" />
    </XhBackTopTrigger>
  </XhBackTopRoot>
</template>
