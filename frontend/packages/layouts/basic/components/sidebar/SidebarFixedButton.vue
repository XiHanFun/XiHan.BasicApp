<script setup lang="ts">
import { Icon } from '@iconify/vue'

defineOptions({ name: 'SidebarFixedButton' })

const expandOnHover = defineModel<boolean>('expandOnHover', { default: true })

function toggle() {
  expandOnHover.value = !expandOnHover.value
}
</script>

<template>
  <div
    class="absolute bottom-2 right-3 z-10 flex cursor-pointer items-center justify-center rounded-sm bg-accent p-[5px] text-foreground/60 transition-all duration-300 hover:bg-accent-hover hover:text-foreground"
    @click.stop="toggle"
  >
    <Icon
      :icon="expandOnHover ? 'lucide:pin' : 'lucide:pin-off'"
      class="size-3.5"
    />
  </div>
</template>
