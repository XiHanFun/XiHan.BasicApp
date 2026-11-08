<!--
  Copyright (c) 2021-Present XiHanFun and contributors.
  Licensed under the MIT License. See LICENSE in the project root for license information.
-->

<script setup lang="ts">
import type { AppMenuOption } from '~/types'
import { VNodeRender } from '~/components'

/**
 * 顶栏菜单浮层里的一层条目
 *
 * 组件库的 menu / menubar 都只有两级，而路由菜单可以更深；导航菜单的 panel 插槽是自由内容，
 * 于是把整棵子树自己铺在面板里——本组件按 name 自引用逐层往下
 * 条目一律渲染成按钮而非链接：导航走路由，不能让浏览器整页跳走
 */
defineOptions({ name: 'HeaderTopMenuPanel' })

defineProps<{
  nodes: AppMenuOption[]
  /** 当前选中项（整条路径的末端 key） */
  activeKey?: string
}>()

const emit = defineEmits<{ select: [key: string] }>()
</script>

<template>
  <ul class="top-menu-panel">
    <li v-for="node in nodes" :key="node.key">
      <button
        type="button"
        class="top-menu-panel__item"
        :class="{ 'top-menu-panel__item--active': node.key === activeKey }"
        :disabled="node.disabled"
        @click="emit('select', node.key)"
      >
        <span v-if="node.icon" class="top-menu-panel__icon" aria-hidden="true">
          <VNodeRender :content="node.icon()" />
        </span>
        <span class="top-menu-panel__label">
          <VNodeRender v-if="typeof node.label === 'function'" :content="node.label()" />
          <template v-else>{{ node.label }}</template>
        </span>
      </button>
      <!-- 更深的层级往里缩进一档继续铺 -->
      <HeaderTopMenuPanel
        v-if="node.children?.length"
        class="top-menu-panel--nested"
        :nodes="node.children"
        :active-key="activeKey"
        @select="(key: string) => emit('select', key)"
      />
    </li>
  </ul>
</template>

<style scoped>
.top-menu-panel {
  display: flex;
  flex-direction: column;
  margin: 0;
  padding: 0;
  list-style: none;
}

.top-menu-panel--nested {
  padding-inline-start: var(--xh-space-3);
}

.top-menu-panel__item {
  display: flex;
  gap: var(--xh-space-2);
  align-items: center;
  inline-size: 100%;
  min-block-size: 32px;
  padding-inline: var(--xh-space-2_5);
  border: 0;
  border-radius: var(--xh-shape-control);
  background: transparent;
  color: color-mix(in srgb, var(--xh-fg-default) 85%, transparent);
  font: inherit;
  font-size: var(--xh-font-size-sm);
  text-align: start;
  cursor: pointer;
  white-space: nowrap;
  transition:
    background-color var(--xh-motion-duration-micro) var(--xh-motion-ease-enter),
    color var(--xh-motion-duration-micro) var(--xh-motion-ease-enter);
}

.top-menu-panel__item:hover:not(:disabled) {
  background: var(--xh-bg-subtle-hover);
  color: var(--xh-fg-default);
}

.top-menu-panel__item--active {
  background: color-mix(in srgb, var(--xh-fg-brand) 15%, transparent);
  color: var(--xh-fg-brand);
}

.top-menu-panel__item:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.top-menu-panel__icon {
  display: inline-flex;
  flex: none;
  align-items: center;
}

.top-menu-panel__label {
  min-inline-size: 0;
  overflow: hidden;
  text-overflow: ellipsis;
}
</style>
