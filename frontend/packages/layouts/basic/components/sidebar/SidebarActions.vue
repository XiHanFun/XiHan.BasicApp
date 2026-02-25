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
}
</script>

<template>
  <div class="relative shrink-0">
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
        class="absolute bottom-[48px] right-2 z-10 flex h-7 w-7 items-center justify-center rounded-md border border-border/70 bg-background/85 p-[5px] text-muted-foreground outline-none transition-all duration-200 hover:bg-accent hover:text-accent-foreground focus:outline-none"
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

    <button
      v-if="props.sidebarCollapseButton"
      type="button"
      class="flex h-[42px] w-full items-center justify-center border-t border-border bg-sidebar text-muted-foreground outline-none transition-all duration-200 hover:bg-accent hover:text-accent-foreground focus:outline-none"
      :title="props.collapsed ? '展开侧边栏' : '收起侧边栏'"
      @click.stop="emit('toggleCollapse')"
    >
      <Icon
        :icon="props.collapsed ? 'lucide:panel-right-open' : 'lucide:panel-right-close'"
        width="16"
        height="16"
      />
    </button>
  </div>
</template>
