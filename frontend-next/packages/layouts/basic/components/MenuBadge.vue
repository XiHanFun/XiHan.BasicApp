<script lang="ts">
import type { VNodeChild } from 'vue'
import { XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { h } from 'vue'
import { menuBadgeDotColor, resolveMenuBadgeTone } from '~/utils'

/** 菜单标签信息 */
export interface BadgeInfo {
  text?: string | number
  type?: string
  dot?: boolean
}

/** 圆点按标签类型着色，没配时按提醒红 */
function dotStyle(type?: string) {
  return { background: menuBadgeDotColor(resolveMenuBadgeTone(type, true)) }
}

/**
 * 侧边栏菜单 badge 渲染器
 * 用于 buildMenuOptionsFromRoutes 的 badgeLabelRenderer 参数
 */
export function renderSidebarBadgeLabel(
  text: string,
  badge: BadgeInfo,
): string | (() => VNodeChild) {
  if (badge.dot) {
    return () =>
      h('span', { class: 'menu-badge-wrapper' }, [
        h('span', { class: 'menu-badge-text' }, text),
        h('span', { class: 'menu-badge-dot', style: dotStyle(badge.type) }),
      ])
  }
  return () =>
    h('span', { class: 'menu-badge-wrapper' }, [
      h('span', { class: 'menu-badge-text' }, text),
      h(XhTagRoot, {
        size: 'sm',
        variant: 'subtle',
        tone: resolveMenuBadgeTone(badge.type),
        class: 'menu-badge-tag',
      }, () => h(XhTagLabel, () => String(badge.text))),
    ])
}

/**
 * 水平菜单 badge 渲染器
 * 用于 buildMenuOptionsFromRoutes 的 badgeLabelRenderer 参数
 */
export function renderHorizontalBadgeLabel(
  text: string,
  badge: BadgeInfo,
): string | (() => VNodeChild) {
  if (badge.dot) {
    return () =>
      h('span', { class: 'inline-flex items-center gap-1' }, [
        text,
        h('span', { class: 'menu-badge-dot menu-badge-dot--inline', style: dotStyle(badge.type) }),
      ])
  }
  return () =>
    h('span', { class: 'inline-flex items-center gap-1.5' }, [
      text,
      h(XhTagRoot, {
        size: 'sm',
        variant: 'subtle',
        tone: resolveMenuBadgeTone(badge.type),
      }, () => h(XhTagLabel, () => String(badge.text))),
    ])
}
</script>

<script lang="ts" setup>
/**
 * MenuBadge 组件 —— 菜单标签
 *
 * 提供两种用法：
 * 1. 模板用法：<MenuBadge :text="label" :badge="info" mode="sidebar" />
 * 2. 渲染函数：renderSidebarBadgeLabel / renderHorizontalBadgeLabel
 */

defineOptions({ name: 'MenuBadge' })

const props = withDefaults(defineProps<{
  text: string
  badge: BadgeInfo
  mode?: 'sidebar' | 'horizontal'
}>(), { mode: 'sidebar' })

const badgeTone = resolveMenuBadgeTone(props.badge.type)
</script>

<template>
  <!-- 侧边栏模式：文字 … | badge -->
  <span v-if="mode === 'sidebar'" class="menu-badge-wrapper">
    <span class="menu-badge-text">{{ text }}</span>
    <span v-if="badge.dot" class="menu-badge-dot" :style="dotStyle(badge.type)" />
    <XhTagRoot v-else size="sm" variant="subtle" :tone="badgeTone" class="menu-badge-tag"><XhTagLabel>
      {{ badge.text }}
    </XhTagLabel></XhTagRoot>
  </span>

  <!-- 水平模式：行内紧跟 -->
  <span v-else class="inline-flex items-center gap-1.5">
    {{ text }}
    <span v-if="badge.dot" class="menu-badge-dot menu-badge-dot--inline" :style="dotStyle(badge.type)" />
    <XhTagRoot v-else size="sm" variant="subtle" :tone="badgeTone"><XhTagLabel>
      {{ badge.text }}
    </XhTagLabel></XhTagRoot>
  </span>
</template>

<style>
/* ---- 展开状态：水平排列 ---- */
.menu-badge-wrapper {
  display: flex;
  align-items: center;
  justify-content: space-between;
  width: 100%;
}

.menu-badge-text {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  min-width: 0;
}

/* 填色按标签类型由渲染处内联给出 */
.menu-badge-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  flex-shrink: 0;
  margin-left: 6px;
}

/* 水平菜单里紧跟文字 */
.menu-badge-dot--inline {
  margin-left: 2px;
}

/* 标签就是组件库的 sm 档，这里只管它在条目里的位置 */
.menu-badge-tag {
  flex-shrink: 0;
  margin-left: 6px;
}

/* ---- 折叠 + 显示标题时：竖向堆叠（侧栏菜单在这一档挂 .sidebar-menu--collapsed-titled）---- */
.sidebar-menu--collapsed-titled .menu-badge-wrapper {
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 3px;
}

.sidebar-menu--collapsed-titled .menu-badge-text {
  white-space: normal;
  text-align: center;
  word-break: keep-all;
  overflow-wrap: break-word;
}

.sidebar-menu--collapsed-titled .menu-badge-dot {
  margin-left: 0;
}

.sidebar-menu--collapsed-titled .menu-badge-tag {
  margin-left: 0;
}
</style>
