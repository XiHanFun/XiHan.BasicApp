<script setup lang="ts">
defineOptions({ name: 'SidebarBrand' })

const props = defineProps<SidebarBrandProps>()

const emit = defineEmits<{ click: [] }>()

interface SidebarBrandProps {
  collapsed: boolean
  appTitle: string
  appLogo: string
  sidebarCollapsedShowTitle: boolean
}
</script>

<template>
  <div
    class="app-sidebar-brand flex h-14 shrink-0 cursor-pointer items-center overflow-hidden px-3 transition-colors hover:bg-[hsl(var(--accent))]"
    @click="emit('click')"
  >
    <div class="relative h-12 w-full overflow-hidden">
      <div
        class="absolute left-0 top-1/2 flex h-12 w-12 -translate-y-1/2 items-center justify-center rounded-xl bg-[hsl(var(--card)/0.92)] p-1.5 shadow-sm transition-transform duration-300"
        :class="props.collapsed ? 'scale-100' : 'scale-90'"
      >
        <img :src="props.appLogo" :alt="props.appTitle" class="h-10 w-10 object-contain" />
      </div>
      <span
        class="absolute left-[52px] top-1/2 block -translate-y-1/2 overflow-hidden text-ellipsis whitespace-nowrap text-xl font-semibold leading-none text-[hsl(var(--foreground))] transition-all duration-300"
        :class="
          props.collapsed && !props.sidebarCollapsedShowTitle
            ? 'max-w-0 opacity-0 delay-0'
            : 'max-w-[220px] opacity-100 delay-100'
        "
      >
        {{ props.appTitle }}
      </span>
    </div>
  </div>
</template>
