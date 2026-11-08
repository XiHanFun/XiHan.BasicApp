<!--
  Copyright (c) 2021-Present XiHanFun and contributors.
  Licensed under the MIT License. See LICENSE in the project root for license information.
-->

<script lang="ts" setup>
import type { TabItem } from '~/types'
import { XhSortableItem } from '@xihan-ui/vue'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { HOME_PATH } from '~/constants'
import { Icon } from '~/iconify'

interface Props {
  item: TabItem & {
    displayTitle: string
    /** 分屏合并标签的右侧信息（存在时渲染「左icon 左标题 | 右icon 右标题」结构） */
    splitRight?: { title: string, icon?: string }
  }
  index: number
  active: boolean
  isLast: boolean
  draggable: boolean
  showIcon?: boolean
  middleCloseEnabled?: boolean
  styleType?: string
}

const props = withDefaults(defineProps<Props>(), {
  styleType: 'chrome',
})

const emit = defineEmits<{
  jump: [path: string]
  close: [path: string, event: MouseEvent]
  contextmenu: [event: MouseEvent, tab: TabItem]
  togglePin: [path: string]
  middleClose: [path: string]
}>()

const { t } = useI18n()

function resolveIcon(icon: string) {
  if (!icon) {
    return icon
  }
  return icon.includes(':')
    ? icon
    : `lucide:${icon}`
}

const tabClass = computed(() => {
  if (props.styleType === 'chrome') {
    return [
      'chrome-tab -mr-3 h-9 items-center',
      {
        'is-active': props.active,
        'is-last-tab': props.isLast,
        'affix-tab': Boolean(props.item.pinned),
        'draggable': props.draggable,
      },
    ]
  }

  return [
    `flat-tab flat-tab--${props.styleType}`,
    {
      'is-active': props.active,
      'affix-tab': Boolean(props.item.pinned),
      'draggable': props.draggable,
      'has-left-divider': props.index > 0 && props.styleType !== 'card',
      'is-last': props.isLast && props.styleType !== 'card',
    },
  ]
})

function onAuxClick(event: MouseEvent) {
  if (event.button === 1 && props.item.closable && props.middleCloseEnabled !== false) {
    emit('middleClose', props.item.path)
  }
}

/**
 * 悬停提示：首行是标签名（标题会被 120px 截断，悬停第一眼要看得到全名），
 * 次行逐条列出这枚标签当前真正可用的鼠标手势
 */
const tabHint = computed(() => {
  const parts: string[] = []
  if (props.draggable) {
    parts.push(t('tabbar.tab_hint_drag'))
  }
  if (props.item.closable && props.middleCloseEnabled !== false) {
    parts.push(t('tabbar.tab_hint_middle'))
  }
  parts.push(t('tabbar.tab_hint_context'))
  return `${props.item.displayTitle}
${parts.join(t('tabbar.tab_hint_sep'))}`
})
</script>

<template>
  <!--
    单一根 <div>，内容通过 <template v-if> 区分 chrome 与 flat 风格
    TransitionGroup 始终看到同一 DOM 节点类型，切换风格时只做原地 patch，
    不触发 leave/enter 动画，彻底消除切换闪烁
    data-tab-item：标签栏按它数标签、找右键目标，钉在自己的根上，不靠调用方透传
  -->
  <XhSortableItem
    :item-id="item.path"
    :disabled="!draggable"
    data-tab-item="true"
    class="tab-item group relative flex shrink-0 select-none"
    :class="tabClass"
    role="button"
    tabindex="0"
    :title="tabHint"
    @click="emit('jump', item.path)"
    @mousedown.middle.prevent
    @auxclick.prevent="onAuxClick"
    @contextmenu.prevent="emit('contextmenu', $event, item)"
    @keydown.enter.prevent="emit('jump', item.path)"
  >
    <!-- ======== Chrome 风格内容 ======== -->
    <template v-if="styleType === 'chrome'">
      <div class="relative size-full px-1">
        <div
          v-if="index > 0 && !active"
          class="chrome-tab__divider absolute left-[7px] top-1/2 z-0 h-4 w-[1px] -translate-y-1/2"
        />
        <div class="chrome-tab__background absolute inset-0 z-0 px-[6px] py-0">
          <div class="chrome-tab__background-content h-full rounded-tl-[7px] rounded-tr-[7px]" />
          <svg
            class="chrome-tab__background-before absolute bottom-0 left-[-1px]"
            height="7"
            width="7"
          >
            <path d="M 0 7 A 7 7 0 0 0 7 0 L 7 7 Z" />
          </svg>
          <svg
            class="chrome-tab__background-after absolute bottom-0 right-[-1px]"
            height="7"
            width="7"
          >
            <path d="M 0 0 A 7 7 0 0 0 7 7 L 0 7 Z" />
          </svg>
        </div>
        <div
          class="chrome-tab__main relative z-[2] mx-[12px] flex h-full min-w-[60px] items-center gap-1 pr-1"
        >
          <span v-if="showIcon && item.meta?.icon" class="flex-shrink-0 opacity-70" style="display: inline-flex; font-size: var(--xh-glyph-size-text)">
            <Icon :icon="resolveIcon(item.meta.icon as string)" />
          </span>
          <span class="chrome-tab__title">{{ item.displayTitle }}</span>
          <template v-if="item.splitRight">
            <span class="split-tab-sep">|</span>
            <span v-if="showIcon && item.splitRight.icon" class="flex-shrink-0 opacity-70" style="display: inline-flex; font-size: var(--xh-glyph-size-text)">
              <Icon :icon="resolveIcon(item.splitRight.icon)" />
            </span>
            <span class="chrome-tab__title">{{ item.splitRight.title }}</span>
          </template>
          <button
            v-if="item.closable && !item.pinned"
            class="chrome-tab__close chrome-tab__action flex h-5 w-5 items-center justify-center rounded-full"
            type="button"
            :aria-label="t('tabbar.close_tab')"
            :title="t('tabbar.close_tab')"
            @click.stop="emit('close', item.path, $event)"
          >
            <Icon width="12" height="12" icon="lucide:x" />
          </button>
          <button
            v-else-if="item.pinned && item.path !== HOME_PATH"
            class="chrome-tab__pin chrome-tab__action flex h-5 w-5 items-center justify-center rounded-full"
            type="button"
            :aria-label="t('tabbar.unpin')"
            :title="t('tabbar.unpin')"
            @click.stop="emit('togglePin', item.path)"
          >
            <Icon width="12" height="12" icon="lucide:pin" />
          </button>
          <!-- 首页固定标签：只读锁图标（强制固定、不可取消，纯展示不触发 togglePin） -->
          <span
            v-else-if="item.pinned && item.path === HOME_PATH"
            class="chrome-tab__lock chrome-tab__action flex h-5 w-5 items-center justify-center rounded-full"
            :aria-label="t('tabbar.pinned')"
            :title="t('tabbar.pinned')"
          >
            <Icon width="12" height="12" icon="lucide:lock" />
          </span>
        </div>
      </div>
    </template>

    <!-- ======== Plain / Card / Brisk 风格内容 ======== -->
    <template v-else>
      <div class="flat-tab__inner relative flex h-full items-center gap-1 px-4">
        <span v-if="showIcon && item.meta?.icon" class="flex-shrink-0 opacity-70" style="display: inline-flex; font-size: var(--xh-glyph-size-text)">
          <Icon :icon="resolveIcon(item.meta.icon as string)" />
        </span>
        <span class="flat-tab__title">{{ item.displayTitle }}</span>
        <template v-if="item.splitRight">
          <span class="split-tab-sep">|</span>
          <span v-if="showIcon && item.splitRight.icon" class="flex-shrink-0 opacity-70" style="display: inline-flex; font-size: var(--xh-glyph-size-text)">
            <Icon :icon="resolveIcon(item.splitRight.icon)" />
          </span>
          <span class="flat-tab__title">{{ item.splitRight.title }}</span>
        </template>
        <button
          v-if="item.closable && !item.pinned"
          class="flat-tab__close flat-tab__action flex h-5 w-5 items-center justify-center rounded-full"
          type="button"
          :aria-label="t('tabbar.close_tab')"
          :title="t('tabbar.close_tab')"
          @click.stop="emit('close', item.path, $event)"
        >
          <Icon width="12" height="12" icon="lucide:x" />
        </button>
        <button
          v-else-if="item.pinned && item.path !== HOME_PATH"
          class="flat-tab__pin flat-tab__action flex h-5 w-5 items-center justify-center rounded-full"
          type="button"
          :aria-label="t('tabbar.unpin')"
          :title="t('tabbar.unpin')"
          @click.stop="emit('togglePin', item.path)"
        >
          <Icon width="12" height="12" icon="lucide:pin" />
        </button>
        <!-- 首页固定标签：只读锁图标（强制固定、不可取消，纯展示不触发 togglePin） -->
        <span
          v-else-if="item.pinned && item.path === HOME_PATH"
          class="flat-tab__lock flat-tab__action flex h-5 w-5 items-center justify-center rounded-full"
          :aria-label="t('tabbar.pinned')"
          :title="t('tabbar.pinned')"
        >
          <Icon width="12" height="12" icon="lucide:lock" />
        </span>
      </div>
    </template>
  </XhSortableItem>
</template>

<style scoped>
/* ============ Chrome 风格 ============ */
/* 标签文字与侧栏、面包屑同取 medium：Windows 的中文字体没有这一档会落到常规，semibold 又落到粗体、太重 */
.chrome-tab {
  font-size: var(--xh-font-size-sm);
  font-weight: var(--xh-font-weight-medium);
  transition: color var(--xh-motion-duration-enter) var(--xh-motion-ease-enter);
}

.chrome-tab:not(.chrome-tab--dragging) {
  cursor: default;
}

.chrome-tab.is-active {
  z-index: 2;
  margin-bottom: -1px;
  padding-bottom: var(--xh-space-0_5);
}

.chrome-tab__divider {
  background: var(--xh-border-default);
  transition: opacity var(--xh-motion-duration-micro) var(--xh-motion-ease-enter);
}

.chrome-tab__background-content {
  background: transparent;
  transition: background-color var(--xh-motion-duration-micro) var(--xh-motion-ease-sweep);
}

.chrome-tab__background-before,
.chrome-tab__background-after {
  fill: transparent;
  transition: fill var(--xh-motion-duration-micro) var(--xh-motion-ease-enter);
}

.chrome-tab__main {
  color: var(--xh-fg-muted);
  border-radius: var(--xh-shape-control);
  transition:
    color var(--xh-motion-duration-micro) var(--xh-motion-ease-sweep),
    background-color var(--xh-motion-duration-micro) var(--xh-motion-ease-sweep),
    box-shadow var(--xh-motion-duration-micro) var(--xh-motion-ease-sweep);
}

.chrome-tab__title {
  flex: 1;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.chrome-tab__action {
  margin-inline-start: 8px;
}

.chrome-tab__close {
  border: 0;
  background: transparent;
  padding: 0;
  color: currentcolor;
  opacity: 0.72;
  transform: scale(1);
  cursor: pointer;
  transition:
    background-color var(--xh-motion-duration-enter) var(--xh-motion-ease-enter),
    color var(--xh-motion-duration-enter) var(--xh-motion-ease-enter),
    opacity var(--xh-motion-duration-enter) var(--xh-motion-ease-enter),
    box-shadow var(--xh-motion-duration-enter) var(--xh-motion-ease-enter);
}

.chrome-tab__pin,
.chrome-tab__lock {
  border: 0;
  background: transparent;
  padding: 0;
  color: currentcolor;
  opacity: 0.72;
  transform: scale(1);
  cursor: default;
  transition:
    background-color var(--xh-motion-duration-enter) var(--xh-motion-ease-enter),
    color var(--xh-motion-duration-enter) var(--xh-motion-ease-enter),
    opacity var(--xh-motion-duration-enter) var(--xh-motion-ease-enter),
    transform var(--xh-motion-duration-enter) var(--xh-motion-ease-enter);
}

.tab-item.chrome-tab:hover .chrome-tab__close,
.tab-item.chrome-tab:hover .chrome-tab__pin,
.tab-item.chrome-tab:hover .chrome-tab__lock,
.tab-item.chrome-tab.is-active .chrome-tab__close,
.tab-item.chrome-tab.is-active .chrome-tab__pin,
.tab-item.chrome-tab.is-active .chrome-tab__lock {
  opacity: 0.92;
  transform: scale(1);
}

/* 键盘聚焦给与组件库同一道环：2px 画在钮盒内。不写的话落回浏览器缺省环，画在盒外、被页签条的 overflow 裁掉 */
.chrome-tab__close:focus-visible {
  outline: var(--xh-ring-width) solid var(--xh-ring-focus);
  outline-offset: var(--xh-ring-offset);
}

.chrome-tab__close:hover,
.chrome-tab__pin:hover {
  opacity: 1 !important;
  background: color-mix(in srgb, var(--xh-bg-brand) 20%, var(--xh-bg-surface));
  color: var(--xh-fg-default);
  box-shadow: inset 0 0 0 1px color-mix(in srgb, var(--xh-fg-brand) 28%, transparent);
}

.tab-item.chrome-tab:hover:not(.is-active) {
  z-index: 2;
  margin-bottom: -1px;
  padding-bottom: var(--xh-space-0_5);
}

.tab-item.chrome-tab:hover:not(.is-active) .chrome-tab__divider {
  opacity: 0;
}

.tab-item.chrome-tab.is-active .chrome-tab__main,
.tab-item.chrome-tab:hover:not(.is-active) .chrome-tab__main {
  color: var(--tab-active-color);
}

.tab-item.chrome-tab.is-active .chrome-tab__background-content {
  background: var(--tab-active-bg);
}

.tab-item.chrome-tab.is-active .chrome-tab__background-before,
.tab-item.chrome-tab.is-active .chrome-tab__background-after {
  fill: var(--tab-active-bg);
}

.tab-item.chrome-tab:hover:not(.is-active) .chrome-tab__background-content {
  background: color-mix(in srgb, var(--xh-bg-brand) 8%, var(--xh-bg-subtle-hover));
}

.tab-item.chrome-tab:hover:not(.is-active) .chrome-tab__background-before,
.tab-item.chrome-tab:hover:not(.is-active) .chrome-tab__background-after {
  fill: color-mix(in srgb, var(--xh-fg-brand) 8%, var(--xh-bg-subtle-hover));
}

.tab-item.chrome-tab.is-active + .tab-item.chrome-tab .chrome-tab__divider,
.tab-item.chrome-tab:hover + .tab-item.chrome-tab .chrome-tab__divider {
  opacity: 0;
}

/* ============ Flat 通用基础 ============ */
.flat-tab {
  font-size: var(--xh-font-size-sm);
  font-weight: var(--xh-font-weight-medium);
  cursor: default;
  color: var(--xh-fg-muted);
}

.flat-tab__title {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  max-width: 120px;
}

.flat-tab__action {
  margin-inline-start: 8px;
}

.flat-tab__close,
.flat-tab__pin,
.flat-tab__lock {
  border: 0;
  background: transparent;
  padding: 0;
  color: currentcolor;
  opacity: 0.72;
  transform: scale(1);
  cursor: default;
  flex-shrink: 0;
  transition:
    background-color var(--xh-motion-duration-enter) var(--xh-motion-ease-enter),
    color var(--xh-motion-duration-enter) var(--xh-motion-ease-enter),
    opacity var(--xh-motion-duration-enter) var(--xh-motion-ease-enter),
    transform var(--xh-motion-duration-enter) var(--xh-motion-ease-enter);
}

.flat-tab__close {
  cursor: pointer;
}

.tab-item.flat-tab:hover .flat-tab__close,
.tab-item.flat-tab:hover .flat-tab__pin,
.tab-item.flat-tab:hover .flat-tab__lock,
.tab-item.flat-tab.is-active .flat-tab__close,
.tab-item.flat-tab.is-active .flat-tab__pin,
.tab-item.flat-tab.is-active .flat-tab__lock {
  opacity: 0.92;
  transform: scale(1);
}

.flat-tab__close:hover {
  background: color-mix(in srgb, var(--xh-bg-brand) 20%, var(--xh-bg-surface));
  color: var(--xh-fg-default);
  box-shadow: inset 0 0 0 1px color-mix(in srgb, var(--xh-fg-brand) 28%, transparent);
  opacity: 1 !important;
}

.flat-tab__pin:hover {
  opacity: 1 !important;
  background: color-mix(in srgb, var(--xh-bg-brand) 20%, var(--xh-bg-surface));
  color: var(--xh-fg-default);
  box-shadow: inset 0 0 0 1px color-mix(in srgb, var(--xh-fg-brand) 28%, transparent);
}

/* ---- Plain 风格 ---- */
.flat-tab--plain {
  height: 100%;
  transition:
    color var(--xh-motion-duration-enter) var(--xh-motion-ease-enter),
    background var(--xh-motion-duration-enter) var(--xh-motion-ease-enter);
}

.flat-tab--plain.has-left-divider {
  border-left: 1px solid var(--xh-border-default);
}

.flat-tab--plain.is-last {
  border-right: 1px solid var(--xh-border-default);
}

.flat-tab--plain:hover:not(.is-active) {
  background: color-mix(in srgb, var(--xh-bg-brand) 14%, var(--xh-bg-subtle-hover));
  color: var(--xh-fg-default);
}

.flat-tab--plain.is-active {
  background: var(--tab-active-bg);
  color: var(--tab-active-color);
}

/* ---- Card 风格 ---- */
.flat-tab--card {
  height: calc(100% - 4px);
  margin-top: 2px;
  margin-inline-start: 6px;
  border-radius: var(--xh-shape-control);
  border: 1px solid var(--xh-border-default);
  transition:
    color var(--xh-motion-duration-enter) var(--xh-motion-ease-enter),
    background var(--xh-motion-duration-enter) var(--xh-motion-ease-enter),
    border-color var(--xh-motion-duration-enter) var(--xh-motion-ease-enter);
}

.flat-tab--card:hover:not(.is-active) {
  background: color-mix(in srgb, var(--xh-bg-brand) 14%, var(--xh-bg-subtle-hover));
  color: var(--xh-fg-default);
  border-color: color-mix(in srgb, var(--xh-fg-brand) 30%, transparent);
}

.flat-tab--card.is-active {
  background: var(--tab-active-bg);
  color: var(--tab-active-color);
  border-color: color-mix(in srgb, var(--tab-active-color) 50%, transparent);
}

/* ---- Brisk 风格 ---- */
.flat-tab--brisk {
  height: 100%;
  position: relative;
  transition:
    color var(--xh-motion-duration-enter) var(--xh-motion-ease-enter),
    background var(--xh-motion-duration-enter) var(--xh-motion-ease-enter);
}

.flat-tab--brisk.has-left-divider {
  border-left: 1px solid var(--xh-border-default);
}

.flat-tab--brisk.is-last {
  border-right: 1px solid var(--xh-border-default);
}

.flat-tab--brisk::after {
  content: '';
  position: absolute;
  bottom: 0;
  inset-inline-start: 0;
  width: 100%;
  height: 2px;
  background: var(--tab-active-color);
  transform: scaleX(0);
  transform-origin: left;
  transition: transform var(--xh-motion-duration-slide) var(--xh-motion-ease-sweep);
}

.flat-tab--brisk:hover::after,
.flat-tab--brisk.is-active::after {
  transform: scaleX(1);
}

.flat-tab--brisk:hover:not(.is-active) {
  background: color-mix(in srgb, var(--xh-bg-brand) 14%, var(--xh-bg-subtle-hover));
  color: var(--xh-fg-default);
}

.flat-tab--brisk.is-active {
  color: var(--tab-active-color);
}

/* 拖拽中的标签（sortable 在被拖那一项上写 data-dragging） */
.tab-item[data-dragging] {
  z-index: 3;
  cursor: grabbing;
  opacity: 0.6;
}

/* 分屏合并标签的左右分隔符 */
.split-tab-sep {
  flex-shrink: 0;
  margin: 0 2px;
  opacity: 0.4;
  font-weight: 400;
}
</style>
