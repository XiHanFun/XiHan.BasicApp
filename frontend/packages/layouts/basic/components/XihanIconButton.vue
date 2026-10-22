<script setup lang="ts">
/** 顶栏 32px 图标钮。皮肤是全局的 .xihan-icon-btn（packages/design/ui.css），本组件只负责接线 */
defineOptions({ name: 'XihanIconButton', inheritAttrs: false })

withDefaults(defineProps<{
  tooltip?: string
  /** 只给可及名、不出原生提示：悬停即展开菜单的触发钮用它，免得提示与菜单叠在一起 */
  ariaLabel?: string
  active?: boolean
  disabled?: boolean
}>(), {
  tooltip: '',
  ariaLabel: '',
  active: false,
  disabled: false,
})
</script>

<template>
  <button
    v-bind="$attrs"
    type="button"
    class="xihan-icon-btn"
    :class="{ 'xihan-icon-btn--active': active }"
    :disabled="disabled"
    :title="tooltip || undefined"
    :aria-label="tooltip || ariaLabel || undefined"
  >
    <slot />
  </button>
</template>
