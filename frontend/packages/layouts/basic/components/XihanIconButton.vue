<script setup lang="ts">
defineOptions({ name: 'XihanIconButton', inheritAttrs: false })

withDefaults(defineProps<{
  tooltip?: string
  active?: boolean
}>(), {
  tooltip: '',
  active: false,
})
</script>

<template>
  <button
    v-bind="$attrs"
    type="button"
    class="xihan-icon-btn"
    :class="{ 'xihan-icon-btn--active': active }"
    :title="tooltip"
  >
    <slot />
  </button>
</template>

<style scoped>
.xihan-icon-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 32px;
  height: 32px;
  padding: 0;
  border: none;
  border-radius: 6px;
  background: transparent;
  color: hsl(var(--foreground) / 65%);
  cursor: pointer;
  outline: none;
  flex-shrink: 0;
  transition:
    background 0.15s ease,
    color 0.15s ease;
}

.xihan-icon-btn:hover {
  background: hsl(var(--accent));
  color: hsl(var(--foreground));
}

.xihan-icon-btn:active {
  background: hsl(var(--accent) / 80%);
}

.xihan-icon-btn--active {
  background: hsl(var(--accent));
  color: hsl(var(--primary));
}
</style>
