<script lang="ts">
import type { VNodeChild } from 'vue'
import { NTag } from 'naive-ui'
import { h } from 'vue'

/** 菜单标签信息 */
export interface BadgeInfo {
  text?: string | number
  type?: string
  dot?: boolean
}

type TagType = 'default' | 'error' | 'info' | 'success' | 'warning'

const BADGE_TYPE_MAP: Record<string, TagType> = {
  default: 'default',
  success: 'success',
  warning: 'warning',
  error: 'error',
  info: 'info',
}

export function resolveBadgeType(type?: string): TagType {
  return BADGE_TYPE_MAP[type ?? ''] ?? 'default'
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
        h('span', { class: 'menu-badge-dot' }),
      ])
  }
  return () =>
    h('span', { class: 'menu-badge-wrapper' }, [
      h('span', { class: 'menu-badge-text' }, text),
      h(NTag, {
        size: 'tiny',
        type: resolveBadgeType(badge.type),
        round: true,
        bordered: false,
        class: 'menu-badge-tag',
      }, () => String(badge.text)),
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
        h('span', { class: 'menu-badge-dot', style: 'margin-left: 2px' }),
      ])
  }
  return () =>
    h('span', { class: 'inline-flex items-center gap-1.5' }, [
      text,
      h(NTag, {
        size: 'tiny',
        type: resolveBadgeType(badge.type),
        round: true,
        bordered: false,
      }, () => String(badge.text)),
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

const tagType = resolveBadgeType(props.badge.type)
</script>

<template>
  <!-- 侧边栏模式：文字 … | badge -->
  <span v-if="mode === 'sidebar'" class="menu-badge-wrapper">
    <span class="menu-badge-text">{{ text }}</span>
    <span v-if="badge.dot" class="menu-badge-dot" />
    <NTag v-else size="tiny" :type="tagType" round :bordered="false" class="menu-badge-tag">
      {{ badge.text }}
    </NTag>
  </span>

  <!-- 水平模式：行内紧跟 -->
  <span v-else class="inline-flex items-center gap-1.5">
    {{ text }}
    <span v-if="badge.dot" class="menu-badge-dot" style="margin-left: 2px" />
    <NTag v-else size="tiny" :type="tagType" round :bordered="false">
      {{ badge.text }}
    </NTag>
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

.menu-badge-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: hsl(var(--destructive));
  flex-shrink: 0;
  margin-left: 6px;
}

.menu-badge-tag {
  flex-shrink: 0;
  margin-left: 6px;
  font-size: 11px !important;
  padding: 0 6px !important;
  height: 18px !important;
  line-height: 18px !important;
}

/* ---- 折叠 + 显示标题时：竖向堆叠 ---- */
.mixed-primary-menu .sidebar-menu-collapsed-show-title.n-menu.n-menu--collapsed .menu-badge-wrapper,
.sidebar-menu-collapsed-show-title.n-menu.n-menu--collapsed .menu-badge-wrapper {
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 3px;
}

.mixed-primary-menu .sidebar-menu-collapsed-show-title.n-menu.n-menu--collapsed .menu-badge-text,
.sidebar-menu-collapsed-show-title.n-menu.n-menu--collapsed .menu-badge-text {
  white-space: normal;
  text-align: center;
  word-break: keep-all;
  overflow-wrap: break-word;
}

.mixed-primary-menu .sidebar-menu-collapsed-show-title.n-menu.n-menu--collapsed .menu-badge-dot,
.sidebar-menu-collapsed-show-title.n-menu.n-menu--collapsed .menu-badge-dot {
  margin-left: 0;
}

.mixed-primary-menu .sidebar-menu-collapsed-show-title.n-menu.n-menu--collapsed .menu-badge-tag,
.sidebar-menu-collapsed-show-title.n-menu.n-menu--collapsed .menu-badge-tag {
  margin-left: 0;
  font-size: 10px !important;
  padding: 0 4px !important;
  height: 16px !important;
  line-height: 16px !important;
}
</style>
