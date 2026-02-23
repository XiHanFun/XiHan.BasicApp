<script setup lang="ts">
import { Icon } from '@iconify/vue'

defineOptions({ name: 'SidebarActions' })

const props = defineProps<SidebarActionsProps>()

const emit = defineEmits<{
  toggleCollapse: []
  togglePin: []
}>()

interface SidebarActionsProps {
  collapsed: boolean
  sidebarCollapseButton: boolean
  sidebarFixedButton: boolean
  floatingMode: boolean
  floatingExpand: boolean
  sidebarPinned: boolean
  sidebarCurrentWidth: number
}
</script>

<template>
  <!-- 折叠/展开按钮 -->
  <button
    v-if="props.sidebarCollapseButton"
    type="button"
    class="fixed bottom-2 left-3 z-40 flex h-7 w-7 items-center justify-center rounded-md bg-muted p-1 text-muted-foreground outline-none transition-all duration-200 hover:bg-accent hover:text-accent-foreground focus:outline-none"
    :title="props.collapsed ? '展开侧边栏' : '收起侧边栏'"
    @click.stop="emit('toggleCollapse')"
  >
    <Icon
      :icon="props.collapsed ? 'lucide:chevrons-right' : 'lucide:chevrons-left'"
      width="15"
      height="15"
    />
  </button>

  <!-- Pin / 取消固定按钮 -->
  <Transition
    enter-active-class="transition-all duration-300 delay-100"
    enter-from-class="-translate-x-2 opacity-0"
    enter-to-class="translate-x-0 opacity-100"
    leave-active-class="transition-all duration-200"
    leave-from-class="translate-x-0 opacity-100"
    leave-to-class="-translate-x-1 opacity-0"
  >
    <button
      v-if="props.sidebarFixedButton && !props.collapsed && (!props.floatingMode || props.floatingExpand)"
      type="button"
      :style="{ left: `${Math.max(12, props.sidebarCurrentWidth - 40)}px` }"
      class="fixed bottom-2 z-40 flex h-7 w-7 items-center justify-center rounded-md bg-muted p-[5px] text-muted-foreground outline-none transition-all duration-200 hover:bg-accent hover:text-accent-foreground focus:outline-none"
      :title="props.sidebarPinned ? '取消固定（悬停展开）' : '固定侧边栏'"
      @click.stop="emit('togglePin')"
    >
      <Icon
        :icon="props.sidebarPinned ? 'lucide:pin-off' : 'lucide:pin'"
        width="14"
        height="14"
      />
    </button>
  </Transition>
</template>
