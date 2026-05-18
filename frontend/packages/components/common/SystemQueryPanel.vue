<script setup lang="ts">
const props = withDefaults(defineProps<{
  padding?: string
}>(), {
  padding: '10px 16px',
})
</script>

<template>
  <div class="xh-query-panel mb-2" :style="{ padding: props.padding }">
    <slot />
  </div>
</template>

<style scoped>
.xh-query-panel {
  background: var(--n-card-color);
  border-radius: var(--n-border-radius);
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.04);
}
</style>
