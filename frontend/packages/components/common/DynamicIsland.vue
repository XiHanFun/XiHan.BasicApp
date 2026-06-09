<script lang="ts" setup>
import { computed } from 'vue'
import { useDynamicIsland } from '~/composables'
import { Icon } from '~/iconify'

defineOptions({ name: 'DynamicIsland' })

const { current, loadingCount } = useDynamicIsland()

const stateIcon = computed(() => {
  switch (current.value?.state) {
    case 'success':
      return 'lucide:check'
    case 'error':
      return 'lucide:triangle-alert'
    default:
      return 'lucide:loader-2'
  }
})
</script>

<template>
  <Teleport to="body">
    <Transition name="island">
      <div
        v-if="current"
        class="dynamic-island"
        :class="`is-${current.state}`"
        role="status"
        aria-live="polite"
      >
        <span class="di-indicator" :class="{ 'di-spin': current.state === 'loading' }">
          <Icon :icon="stateIcon" width="15" height="15" />
        </span>
        <Transition name="di-text" mode="out-in">
          <span :key="`${current.state}:${current.label}`" class="di-label">{{ current.label }}</span>
        </Transition>
        <span v-if="loadingCount > 1" class="di-count">{{ loadingCount }}</span>
      </div>
    </Transition>
  </Teleport>
</template>

<style scoped>
.dynamic-island {
  position: fixed;
  top: 10px;
  left: 50%;
  z-index: 3000;
  display: inline-flex;
  align-items: center;
  gap: 8px;
  max-width: min(80vw, 420px);
  height: 34px;
  padding: 0 14px;
  border-radius: 9999px;
  /* 标志性"灵动岛"恒为深色，与明暗主题解耦 */
  background: rgb(18 18 20 / 92%);
  color: rgb(255 255 255 / 95%);
  font-size: 13px;
  font-weight: 500;
  line-height: 1;
  white-space: nowrap;
  box-shadow:
    0 8px 28px rgb(0 0 0 / 28%),
    inset 0 0 0 1px rgb(255 255 255 / 8%);
  backdrop-filter: blur(12px);
  pointer-events: none;
  transform: translateX(-50%);
  transform-origin: top center;
}

.di-indicator {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  color: rgb(255 255 255 / 85%);
}

.is-success .di-indicator {
  color: #34d399;
}

.is-error .di-indicator {
  color: #f87171;
}

.di-spin {
  animation: di-spin 0.8s linear infinite;
}

@keyframes di-spin {
  to {
    transform: rotate(360deg);
  }
}

.di-label {
  overflow: hidden;
  text-overflow: ellipsis;
}

.di-count {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-width: 16px;
  height: 16px;
  padding: 0 4px;
  border-radius: 9999px;
  background: rgb(255 255 255 / 16%);
  font-size: 10px;
  font-weight: 600;
}

/* 整体进出场：从顶部"滴落"展开 + 回弹 */
.island-enter-active {
  transition:
    opacity 0.28s ease,
    transform 0.42s cubic-bezier(0.22, 1.4, 0.36, 1);
}

.island-leave-active {
  transition:
    opacity 0.22s ease,
    transform 0.26s cubic-bezier(0.4, 0, 1, 1);
}

.island-enter-from {
  opacity: 0;
  transform: translateX(-50%) translateY(-16px) scale(0.7);
}

.island-leave-to {
  opacity: 0;
  transform: translateX(-50%) translateY(-12px) scale(0.85);
}

/* 文案切换：快速淡入淡出，模拟内容形变 */
.di-text-enter-active,
.di-text-leave-active {
  transition:
    opacity 0.18s ease,
    transform 0.18s ease;
}

.di-text-enter-from {
  opacity: 0;
  transform: translateY(6px);
}

.di-text-leave-to {
  opacity: 0;
  transform: translateY(-6px);
}
</style>
