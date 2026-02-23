<script setup lang="ts">
import { Icon } from '@iconify/vue'
import { computed } from 'vue'

defineOptions({ name: 'XihanBackTop' })

const props = withDefaults(defineProps<XihanBackTopProps>(), {
  threshold: 200,
  scrollY: 0,
})

interface XihanBackTopProps {
  /** 当前内容区滚动距离，由父级注入 */
  scrollY?: number
  /** 触发显示的阈值（px） */
  threshold?: number
  /** NLayoutContent 的组件实例（用于滚动回顶） */
  contentRef?: any
}

const visible = computed(() => props.scrollY > props.threshold)

function scrollToTop() {
  const inst = props.contentRef as { $el?: Element } | null | undefined
  const container = inst?.$el?.querySelector?.('.n-scrollbar-container')
  if (container) {
    container.scrollTo({ top: 0, behavior: 'smooth' })
  }
  else {
    window.scrollTo({ top: 0, behavior: 'smooth' })
  }
}
</script>

<template>
  <Transition name="xihan-back-top">
    <button
      v-if="visible"
      class="xihan-back-top"
      aria-label="回到顶部"
      @click="scrollToTop"
    >
      <Icon icon="lucide:chevron-up" width="18" height="18" />
    </button>
  </Transition>
</template>

<style scoped>
.xihan-back-top {
  position: fixed;
  right: 24px;
  bottom: 32px;
  z-index: 100;
  display: flex;
  align-items: center;
  justify-content: center;
  width: 36px;
  height: 36px;
  border-radius: 50%;
  border: 1px solid hsl(var(--border));
  background: hsl(var(--card));
  color: hsl(var(--foreground));
  box-shadow: 0 4px 16px hsl(var(--foreground) / 10%);
  cursor: pointer;
  transition:
    background 0.2s ease,
    box-shadow 0.2s ease,
    transform 0.15s ease;
}

.xihan-back-top:hover {
  background: hsl(var(--accent));
  box-shadow: 0 6px 20px hsl(var(--foreground) / 15%);
  transform: translateY(-2px);
}

.xihan-back-top:active {
  transform: translateY(0);
}

.xihan-back-top-enter-active,
.xihan-back-top-leave-active {
  transition:
    opacity 0.25s ease,
    transform 0.25s ease;
}

.xihan-back-top-enter-from,
.xihan-back-top-leave-to {
  opacity: 0;
  transform: translateY(12px) scale(0.85);
}
</style>
