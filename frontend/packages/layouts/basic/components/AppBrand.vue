<script setup lang="ts">
defineOptions({ name: 'AppBrand' })

const props = withDefaults(defineProps<AppBrandProps>(), {
  collapsed: false,
  logoSize: 32,
})

const emit = defineEmits<{ click: [] }>()

interface AppBrandProps {
  appTitle: string
  appLogo: string
  collapsed?: boolean
  logoSize?: number
}
</script>

<template>
  <div
    class="app-brand flex h-full shrink-0 cursor-pointer items-center overflow-hidden px-3"
    @click="emit('click')"
  >
    <div class="flex min-w-0 items-center gap-2 overflow-hidden">
      <span class="app-brand-logo-wrap flex-shrink-0">
        <img
          :src="props.appLogo"
          :alt="props.appTitle"
          class="app-brand-logo"
          :style="{ width: `${props.logoSize}px`, height: `${props.logoSize}px` }"
        >
      </span>
      <span
        v-if="!props.collapsed"
        class="app-brand-title truncate"
      >
        {{ props.appTitle }}
      </span>
    </div>
  </div>
</template>

<style scoped>
.app-brand-logo-wrap {
  display: inline-flex;
  height: 32px;
  width: 32px;
  align-items: center;
  justify-content: center;
  border-radius: 8px;
  border: 1px solid hsl(var(--border) / 0.6);
  background: hsl(var(--card) / 0.9);
}

.app-brand-logo {
  object-fit: contain;
}

.app-brand-title {
  font-size: 15px;
  font-weight: 600;
  letter-spacing: 0.01em;
  line-height: 1.3;
  color: hsl(var(--foreground));
  max-width: 160px;
}
</style>
