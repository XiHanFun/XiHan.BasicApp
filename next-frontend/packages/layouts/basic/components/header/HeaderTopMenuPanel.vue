<script setup lang="ts">
import type { AppMenuOption } from '~/types'
import { VNodeRender } from '~/components'

/**
 * 顶栏菜单浮层里的一层条目。
 *
 * 组件库的 menu / menubar 都只有两级，而路由菜单可以更深；导航菜单的 panel 插槽是自由内容，
 * 于是把整棵子树自己铺在面板里——本组件按 name 自引用逐层往下。
 * 条目一律渲染成按钮而非链接：导航走路由，不能让浏览器整页跳走。
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
  padding-inline-start: 12px;
}

.top-menu-panel__item {
  display: flex;
  gap: 8px;
  align-items: center;
  inline-size: 100%;
  min-block-size: 32px;
  padding-inline: 10px;
  border: 0;
  border-radius: var(--xh-shape-control);
  background: transparent;
  color: hsl(var(--foreground) / 85%);
  font: inherit;
  font-size: 13px;
  text-align: start;
  cursor: pointer;
  white-space: nowrap;
}

.top-menu-panel__item:hover:not(:disabled) {
  background: hsl(var(--accent));
  color: hsl(var(--foreground));
}

.top-menu-panel__item--active {
  background: hsl(var(--primary) / 15%);
  color: hsl(var(--primary));
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
