<script setup lang="ts">
const props = withDefaults(defineProps<{
  padding?: string
}>(), {
  padding: '10px 16px',
})
</script>

<template>
  <vxe-card class="xh-query-panel mb-2" :style="{ padding: props.padding }">
    <slot />
  </vxe-card>
</template>
