<script setup lang="ts">
import { Icon } from '@iconify/vue'
import { NIcon } from 'naive-ui'

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
  <button
    v-if="props.sidebarCollapseButton"
    type="button"
    class="fixed bottom-2 left-3 z-40 flex h-7 w-7 items-center justify-center rounded-sm border-0 bg-[hsl(var(--muted))] p-1 text-[hsl(var(--muted-foreground))] outline-none transition-all duration-300 hover:bg-[hsl(var(--accent))] hover:text-[hsl(var(--accent-foreground))] focus:outline-none"
    :title="props.collapsed ? '展开侧边栏' : '收起侧边栏'"
    @click.stop="emit('toggleCollapse')"
  >
    <NIcon size="15">
      <Icon :icon="props.collapsed ? 'lucide:chevrons-right' : 'lucide:chevrons-left'" />
    </NIcon>
  </button>

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
      class="fixed bottom-2 z-40 flex h-7 w-7 items-center justify-center rounded-sm border-0 bg-[hsl(var(--muted))] p-[5px] text-[hsl(var(--muted-foreground))] outline-none transition-all duration-300 hover:bg-[hsl(var(--accent))] hover:text-[hsl(var(--accent-foreground))] focus:outline-none"
      :title="props.sidebarPinned ? '主体不跟随侧边栏' : '主体跟随侧边栏'"
      @click.stop="emit('togglePin')"
    >
      <NIcon size="14">
        <Icon :icon="props.sidebarPinned ? 'lucide:pin-off' : 'lucide:pin'" />
      </NIcon>
    </button>
  </Transition>
</template>
