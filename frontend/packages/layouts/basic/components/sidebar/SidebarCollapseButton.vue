<script setup lang="ts">
import { Icon } from '@iconify/vue'

defineOptions({ name: 'SidebarCollapseButton' })

const props = defineProps<{ collapsed: boolean }>()
const emit = defineEmits<{ 'update:collapsed': [value: boolean] }>()

function toggle() {
  emit('update:collapsed', !props.collapsed)
}
</script>

<template>
  <div
    class="absolute bottom-2 left-3 z-10 flex cursor-pointer items-center justify-center rounded-sm bg-accent p-1 text-foreground/60 transition-all duration-200 hover:bg-accent-hover hover:text-foreground"
    @click.stop="toggle"
  >
    <Icon
      :icon="collapsed ? 'lucide:chevrons-right' : 'lucide:chevrons-left'"
      class="size-4"
    />
  </div>
</template>
