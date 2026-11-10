<!--
  Copyright (c) 2021-Present XiHanFun and contributors.
  Licensed under the MIT License. See LICENSE in the project root for license information.
-->

<script setup lang="ts">
import { XhButton } from '@xihan-ui/vue'

/** 顶栏图标钮：组件库的 ghost 钮，高度取 md 档控件、随密度走；开着面板时的品牌色在全局 .app-icon-btn--active */
defineOptions({ name: 'AppIconButton', inheritAttrs: false })

withDefaults(defineProps<{
  tooltip?: string
  active?: boolean
  disabled?: boolean
  /** 带文字（上下文切换、用户菜单）：宽度随内容，不按正方盒排 */
  labeled?: boolean
}>(), {
  tooltip: '',
  active: false,
  disabled: false,
  labeled: false,
})
</script>

<template>
  <!-- 作者属性放最后：作浮层触发器（as-child）时传进来的 aria-label 等压过这里的缺省 -->
  <XhButton
    type="button"
    variant="ghost"
    tone="neutral"
    size="md"
    :icon-only="!labeled"
    class="app-icon-btn"
    :class="{ 'app-icon-btn--active': active }"
    :disabled="disabled"
    :title="tooltip || undefined"
    :aria-label="tooltip || undefined"
    v-bind="$attrs"
  >
    <slot />
  </XhButton>
</template>
