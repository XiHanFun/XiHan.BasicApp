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
    class="app-sidebar-brand flex h-14 shrink-0 cursor-pointer items-center overflow-hidden px-3"
    @click="emit('click')"
  >
    <div
      class="site-brand-inline flex w-full min-w-0 items-center"
      :class="props.collapsed && !props.sidebarCollapsedShowTitle ? 'justify-center' : ''"
    >
      <span class="site-brand-logo-wrap">
        <img :src="props.appLogo" :alt="props.appTitle" class="site-brand-logo">
      </span>
      <span
        class="site-brand-title transition-all duration-300"
        :class="
          props.collapsed && !props.sidebarCollapsedShowTitle
            ? 'site-brand-title--hide ml-0 opacity-0'
            : 'site-brand-title--show ml-2 opacity-100'
        "
      >
        {{ props.appTitle }}
      </span>
    </div>
  </div>
</template>

<style scoped>
.site-brand-inline {
  --brand-shell-max-width: 220px;
  --brand-logo-wrap-size: 24px;
  --brand-logo-size: 16px;
  --brand-title-max-width: 180px;
  max-width: var(--brand-shell-max-width);
}

.site-brand-logo-wrap {
  display: inline-flex;
  height: var(--brand-logo-wrap-size);
  width: var(--brand-logo-wrap-size);
  flex-shrink: 0;
  align-items: center;
  justify-content: center;
  border-radius: 6px;
  border: 1px solid hsl(var(--border) / 0.7);
  background: hsl(var(--card) / 0.92);
}

.site-brand-logo {
  height: var(--brand-logo-size);
  width: var(--brand-logo-size);
  object-fit: contain;
}

.site-brand-title {
  max-width: var(--brand-title-max-width);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  font-size: 14px;
  font-weight: 600;
  letter-spacing: 0.01em;
  color: hsl(var(--foreground));
}

.site-brand-title--show {
  max-width: var(--brand-title-max-width);
}

.site-brand-title--hide {
  max-width: 0;
}
</style>
