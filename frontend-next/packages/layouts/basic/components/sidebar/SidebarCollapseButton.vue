<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { Icon } from '~/iconify'

defineOptions({ name: 'SidebarCollapseButton' })

const props = defineProps<{ collapsed: boolean }>()
const emit = defineEmits<{ 'update:collapsed': [value: boolean] }>()

const { t } = useI18n()

// 名字说的是按下后的动作，随状态换；顶栏那颗管整栏显隐，两者同屏，所以不复用它的文案
const label = computed(() => props.collapsed ? t('sidebar.expand') : t('sidebar.collapse'))

function toggle() {
  emit('update:collapsed', !props.collapsed)
}
</script>

<template>
  <button
    type="button"
    class="sidebar-collapse-button absolute bottom-2 left-3 z-10 flex cursor-pointer items-center justify-center rounded-sm bg-accent p-1 text-foreground/60 hover:bg-accent-hover hover:text-foreground"
    :aria-label="label"
    :title="label"
    @click.stop="toggle"
  >
    <span class="icon-swap">
      <Transition name="icon-swap">
        <Icon
          :key="collapsed ? 'right' : 'left'"
          :icon="collapsed ? 'lucide:chevrons-right' : 'lucide:chevrons-left'"
          class="size-4"
        />
      </Transition>
    </span>
  </button>
</template>

<style scoped>
/* 换色与顶栏图标钮同一组令牌；按压缩放走组件库的 press / release，reduced motion 下令牌自己归零 */
.sidebar-collapse-button {
  transition:
    background-color var(--xh-motion-duration-micro) var(--xh-motion-ease-enter),
    color var(--xh-motion-duration-micro) var(--xh-motion-ease-enter),
    scale var(--xh-motion-duration-release) var(--xh-motion-ease-release);
}

.sidebar-collapse-button:focus-visible {
  outline: var(--xh-ring-width) solid var(--xh-ring-focus);
  outline-offset: var(--xh-ring-offset);
}

.sidebar-collapse-button:active {
  scale: var(--xh-motion-scale-press);
  transition-duration: var(--xh-motion-duration-press);
  transition-timing-function: var(--xh-motion-ease-press);
}
</style>
