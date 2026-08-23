<script setup lang="ts">
/** 顶栏 32px 图标钮。皮肤是全局的 .xihan-icon-btn（packages/design/ui.css），本组件只负责接线 */
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
    :title="tooltip || undefined"
    :aria-label="tooltip || undefined"
  >
    <slot />
  </button>
</template>
