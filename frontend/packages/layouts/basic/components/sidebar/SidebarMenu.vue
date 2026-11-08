<!--
  Copyright (c) 2021-Present XiHanFun and contributors.
  Licensed under the MIT License. See LICENSE in the project root for license information.
-->

<script setup lang="ts">
import type { SideNavNode } from '@xihan-ui/headless'
import type { SidebarMenuPropsContract } from '../../contracts'
import type { MenuLinkResolver } from './menu-links'
import type { AppMenuOption } from '~/types'
import { XhSideNavList, XhSideNavRoot } from '@xihan-ui/vue'
import { computed, ref, watch } from 'vue'
import { menuAncestors, revealExpanded } from './menu-expansion'
import SidebarMenuNodes from './SidebarMenuNodes.vue'

defineOptions({ name: 'SidebarMenu' })

const props = defineProps<SidebarMenuPropsContract & {
  /** 叶子的去处：跳转由链接本身完成，menuUpdate 只报选中意图，不再负责导航 */
  linkOf: MenuLinkResolver
}>()

const emit = defineEmits<{ menuUpdate: [key: string] }>()

/**
 * 组件库的 collection：条目身份、层级与禁用的事实源
 * 标签只给纯文本供读屏与连打检索；显示由标记里的部件渲染，因为可能带角标或外链图标
 */
function toCollection(nodes: AppMenuOption[]): SideNavNode[] {
  return nodes.map<SideNavNode>(node => ({
    value: node.key,
    label: typeof node.label === 'string'
      ? node.label
      : node.key,
    ...(node.disabled
      ? { disabled: true }
      : {}),
    ...(node.children?.length
      ? {
          children: toCollection(node.children),
        }
      : {}),
  }))
}

const collection = computed(() => toCollection(props.menuOptions))

/**
 * 展开集合由这里持有：组件库把它完全交给应用，不会自己展开到当前项
 * 当前页换了、或菜单到了之后通往它的路径才解得出来时，把各级目录展开；
 * 之后用户手动收起的不再强行打开，直到当前页再换
 */
const expanded = ref<string[]>([])
const activePath = computed(() => menuAncestors(props.menuOptions, props.activeKey) ?? [])
watch(
  [
    () => props.activeKey,
    () => activePath.value.join('\n'),
  ],
  () => {
    expanded.value = revealExpanded(expanded.value, activePath.value, props.accordion ?? true)
  },
  { immediate: true },
)
</script>

<template>
  <div class="app-sidebar-menu flex-1 min-h-0">
    <XhSideNavRoot
      v-model:expanded-value="expanded"
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
        <SidebarMenuNodes :nodes="props.menuOptions" :link-of="props.linkOf" />
      </XhSideNavList>
    </XhSideNavRoot>
  </div>
</template>

<style scoped>
.sidebar-menu {
  /* 组件库的侧栏根自带 --xh-sider-w / --xh-sider-collapsed-w（15rem / 4rem）的固定宽度（它设计上自己就是侧栏）
     本应用的侧栏宽度由用户偏好控制，外层 aside 说了算，故让根跟着容器走，
     否则菜单比容器宽的那一截会被 overflow-x:hidden 裁掉，行尾的箭头首当其冲 */
  --xh-side-nav-w: 100%;
  --xh-side-nav-collapsed-w: 100%;

  /* 行的几何与各态配色走组件库的公开槽：悬停 / 按下 / 当前 / 展开路径的面由
     Collection Item 家族按状态给，这里只换数值，不再直接盖 background / color */
  --xh-side-nav-link-h: 38px;
  --xh-side-nav-link-px: 12px;
  --xh-side-nav-link-gap: 8px;
  --xh-side-nav-link-font-size: 14px;
  --xh-side-nav-row-fg: color-mix(in srgb, var(--xh-fg-default) 80%, transparent);
  --xh-side-nav-row-bg-hover: var(--xh-bg-subtle-hover);
  --xh-side-nav-row-bg-active: color-mix(in srgb, var(--xh-fg-brand) 15%, transparent);
  --xh-side-nav-row-fg-active: var(--xh-fg-brand);
  /* 只有当前叶子高亮，展开中的父级分支不着色 */
  --xh-side-nav-row-bg-in-path: transparent;

  background: transparent;
  font-size: var(--xh-font-size-md);
}

.sidebar-menu--no-top-padding {
  padding-block-start: 0;
}

/* 一行的骨架：图标 + 标签 + 箭头 */
/* 叶子的类名落在 li 上、可交互的是它内部的 link；行盒一律交给交互元素本身，
   hover 与选中才画在同一个盒子上 */
.sidebar-menu :deep(li.sidebar-menu__row) {
  min-block-size: 0;
  padding-inline: 0;
  margin-block: 0;
}

/* 行与行之间留一点气口；字重是版式不是状态，家族没有静息字重槽，直接给。取 medium：Windows 的中文字体只有常规与粗体两档，
   medium 落到常规、semibold 落到粗体，粗体太重（用户 2026-11-07 试过），有 medium 字形的字体上才看得出这一档
   组件库把行定为 inline-size: 100%，下面各档又加了左右外边距：不改回 auto 的话行宽是 100% 再加两侧外边距，
   右边越出侧栏的滚动区一截，行的右缘连同画在行盒内的聚焦环一起被裁掉、左右也不对称 */
.sidebar-menu :deep([data-scope='side-nav']:is([data-part='branch-trigger'], [data-part='link'])) {
  inline-size: auto;
  margin-block: 2px;
  font-weight: var(--xh-font-weight-medium);
}

.sidebar-menu :deep(.sidebar-menu__icon) {
  display: inline-flex;
  flex: none;
  align-items: center;
  color: color-mix(in srgb, var(--xh-fg-default) 72%, transparent);
  transition: transform var(--xh-motion-duration-move) var(--xh-motion-ease-enter);
}

.sidebar-menu
  :deep([data-scope='side-nav']:is([data-part='branch-trigger'], [data-part='link']):hover .sidebar-menu__icon) {
  transform: scale(1.2);
}

.sidebar-menu :deep([data-part='link'][data-current] .sidebar-menu__icon) {
  color: var(--xh-fg-brand);
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
  color: color-mix(in srgb, var(--xh-fg-default) 55%, transparent);
}

/* —— 圆角档 / 直角档：圆角走行的 radius 槽 —— */
.sidebar-menu--rounded {
  --xh-side-nav-link-radius: 8px;
}

.sidebar-menu--rounded :deep([data-scope='side-nav']:is([data-part='branch-trigger'], [data-part='link'])) {
  margin-inline: 8px;
}

.sidebar-menu--plain {
  --xh-side-nav-link-radius: 0;
}

.sidebar-menu--plain :deep([data-scope='side-nav']:is([data-part='branch-trigger'], [data-part='link'])) {
  margin-inline: 0;
}

/* —— 折叠：只留图标。行内衬归零走 px 槽 —— */
.sidebar-menu--collapsed-icon {
  --xh-side-nav-link-px: 0;
}

.sidebar-menu--collapsed-icon :deep([data-scope='side-nav']:is([data-part='branch-trigger'], [data-part='link'])) {
  justify-content: center;
  padding-block: var(--xh-space-3);
  margin-inline: 6px;
}

/* 标签不能 display:none：它是图标栏里链接与分支钮唯一的可及名
   裁法与皮肤折叠落定后的那条相同，只是折叠过程中也不占位，图标从头到尾都居中 */
.sidebar-menu--collapsed-icon :deep(.sidebar-menu__label) {
  position: absolute;
  inline-size: 1px;
  block-size: 1px;
  margin: -1px;
  overflow: hidden;
  clip-path: inset(50%);
  white-space: nowrap;
}

.sidebar-menu--collapsed-icon :deep(.sidebar-menu__arrow) {
  display: none;
}

/* —— 折叠：图标在上、小字标题在下 —— */
.sidebar-menu--collapsed-titled {
  --xh-side-nav-link-px: 0;
  --xh-side-nav-link-gap: 4px;
}

.sidebar-menu--collapsed-titled :deep([data-scope='side-nav']:is([data-part='branch-trigger'], [data-part='link'])) {
  flex-direction: column;
  justify-content: center;
  padding-block: var(--xh-space-2);
  margin-inline: 6px;
  line-height: normal;
}

.sidebar-menu--collapsed-titled :deep(.sidebar-menu__icon) {
  font-size: var(--xh-font-size-xl);
}

/* 皮肤在折叠态把行文字裁成读屏专用（absolute + 1px + clip-path inset(50%)，过程中还淡到 opacity 0），
   带标题的图标栏要它照常露出来，这几项逐一撤回 */
.sidebar-menu--collapsed-titled :deep(.sidebar-menu__label) {
  position: static;
  flex: none;
  inline-size: 100%;
  block-size: auto;
  margin: 0;
  clip-path: none;
  opacity: 1;
  text-align: center;
  font-size: var(--xh-font-size-xs);
  font-weight: 400;
  line-height: 1.4;
  white-space: normal;
  word-break: keep-all;
  overflow-wrap: break-word;
}

.sidebar-menu--collapsed-titled :deep([data-part='link'][data-current] .sidebar-menu__label) {
  font-weight: 600;
}

.sidebar-menu--collapsed-titled :deep(.sidebar-menu__arrow) {
  display: none;
}
</style>
