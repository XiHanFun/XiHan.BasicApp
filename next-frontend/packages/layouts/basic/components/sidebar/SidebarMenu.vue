<script setup lang="ts">
import type { SideNavNode } from '@xihan-ui/headless'
import type { SidebarMenuPropsContract } from '../../contracts'
import type { AppMenuOption } from '~/types'
import { XhSideNavList, XhSideNavRoot } from '@xihan-ui/vue'
import { computed } from 'vue'
import SidebarMenuNodes from './SidebarMenuNodes.vue'

defineOptions({ name: 'SidebarMenu' })

const props = defineProps<SidebarMenuPropsContract>()

const emit = defineEmits<{ menuUpdate: [key: string] }>()

/**
 * 喂给组件库的 collection：条目身份、层级与禁用的事实源。
 * 标签在这里给纯文本兜底（读屏与连打检索取它），真正的显示由标记里的部件渲染——
 * 菜单标签可能带角标或外链图标，塞不进一个字符串。
 */
function toCollection(nodes: AppMenuOption[]): SideNavNode[] {
  return nodes.map<SideNavNode>(node => ({
    value: node.key,
    label: typeof node.label === 'string' ? node.label : node.key,
    ...(node.disabled ? { disabled: true } : {}),
    ...(node.children?.length ? { children: toCollection(node.children) } : {}),
  }))
}

const collection = computed(() => toCollection(props.menuOptions))
</script>

<template>
  <div class="app-sidebar-menu flex-1 min-h-0">
    <XhSideNavRoot
      :value="props.activeKey"
      :collection="collection"
      :collapsed="props.collapsed"
      :accordion="props.accordion ?? true"
      collapsed-popout
      class="sidebar-menu"
      :class="[
        props.navigationStyle === 'rounded' ? 'sidebar-menu--rounded' : 'sidebar-menu--plain',
        props.collapsed && props.sidebarCollapsedShowTitle ? 'sidebar-menu--collapsed-titled' : '',
        props.collapsed && !props.sidebarCollapsedShowTitle ? 'sidebar-menu--collapsed-icon' : '',
        props.noTopPadding ? 'sidebar-menu--no-top-padding' : '',
      ]"
      :style="{ '--sidebar-collapsed-w': `${props.collapsedWidth ?? 60}px` }"
      @update:value="(key: string | null) => key && emit('menuUpdate', key)"
    >
      <XhSideNavList>
        <SidebarMenuNodes :nodes="props.menuOptions" />
      </XhSideNavList>
    </XhSideNavRoot>
  </div>
</template>

<style scoped>
.sidebar-menu {
  background: transparent;
  font-size: 14px;
}

.sidebar-menu--no-top-padding {
  padding-block-start: 0;
}

/* 一行的骨架：图标 + 标签 + 箭头。行高与配色对齐旧版侧栏 */
.sidebar-menu :deep(.sidebar-menu__row) {
  display: flex;
  gap: 8px;
  align-items: center;
  min-block-size: 38px;
  margin-block: 2px;
  padding-inline: 12px;
  color: hsl(var(--foreground) / 80%);
  font-size: 14px;
  font-weight: 500;
}

.sidebar-menu :deep(.sidebar-menu__row:hover) {
  background: hsl(var(--accent));
  color: hsl(var(--foreground));
}

/* 只有当前叶子高亮，父级分支不着色 */
.sidebar-menu :deep([data-part='link'][data-selected].sidebar-menu__row) {
  background: hsl(var(--primary) / 15%);
  color: hsl(var(--primary));
}

.sidebar-menu :deep(.sidebar-menu__icon) {
  display: inline-flex;
  flex: none;
  align-items: center;
  color: hsl(var(--foreground) / 72%);
  transition: transform 0.25s ease;
}

.sidebar-menu :deep(.sidebar-menu__row:hover .sidebar-menu__icon) {
  transform: scale(1.2);
}

.sidebar-menu :deep([data-selected].sidebar-menu__row .sidebar-menu__icon) {
  color: hsl(var(--primary));
}

.sidebar-menu :deep(.sidebar-menu__label) {
  flex: 1;
  min-inline-size: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.sidebar-menu :deep(.sidebar-menu__arrow) {
  flex: none;
  color: hsl(var(--foreground) / 55%);
}

/* —— 圆角档 / 直角档 —— */
.sidebar-menu--rounded :deep(.sidebar-menu__row) {
  margin-inline: 8px;
  border-radius: 8px;
}

.sidebar-menu--plain :deep(.sidebar-menu__row) {
  margin-inline: 0;
  border-radius: 0;
}

/* —— 折叠：只留图标 —— */
.sidebar-menu--collapsed-icon :deep(.sidebar-menu__row) {
  justify-content: center;
  padding-block: 12px;
  padding-inline: 0;
  margin-inline: 6px;
}

.sidebar-menu--collapsed-icon :deep(.sidebar-menu__label),
.sidebar-menu--collapsed-icon :deep(.sidebar-menu__arrow) {
  display: none;
}

/* —— 折叠：图标在上、小字标题在下 —— */
.sidebar-menu--collapsed-titled :deep(.sidebar-menu__row) {
  flex-direction: column;
  gap: 4px;
  justify-content: center;
  padding-block: 8px;
  padding-inline: 0;
  margin-inline: 6px;
  line-height: normal;
}

.sidebar-menu--collapsed-titled :deep(.sidebar-menu__icon) {
  font-size: 20px;
}

.sidebar-menu--collapsed-titled :deep(.sidebar-menu__label) {
  flex: none;
  inline-size: 100%;
  text-align: center;
  font-size: 11px;
  font-weight: 400;
  line-height: 1.4;
  white-space: normal;
  word-break: keep-all;
  overflow-wrap: break-word;
}

.sidebar-menu--collapsed-titled :deep([data-selected].sidebar-menu__row .sidebar-menu__label) {
  font-weight: 600;
}

.sidebar-menu--collapsed-titled :deep(.sidebar-menu__arrow) {
  display: none;
}
</style>
