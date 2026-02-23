<script lang="ts" setup>
import type { TabItem } from '~/types'
import { Icon } from '@iconify/vue'
import { NIcon } from 'naive-ui'
import { HOME_PATH } from '~/constants'

interface Props {
  item: TabItem & { displayTitle: string }
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

function onAuxClick(event: MouseEvent) {
  if (event.button === 1 && props.item.closable && props.middleCloseEnabled !== false) {
    emit('middleClose', props.item.path)
  }
}
</script>

<template>
  <!--
    单一根 <div>，内容通过 <template v-if> 区分 chrome 与 flat 风格。
    TransitionGroup 始终看到同一 DOM 节点类型，切换风格时只做原地 patch，
    不触发 leave/enter 动画，彻底消除切换闪烁。
  -->
  <div
    class="tab-item group relative flex shrink-0 select-none"
    :class="[
      styleType === 'chrome'
        ? [
            'chrome-tab -mr-3 h-8 items-center',
            {
              'is-active': active,
              'is-last-tab': isLast,
              'affix-tab': Boolean(item.pinned),
              draggable,
            },
          ]
        : [
            `flat-tab flat-tab--${styleType}`,
            {
              'is-active': active,
              'affix-tab': Boolean(item.pinned),
              draggable,
              'has-left-divider': index > 0 && styleType !== 'card',
              'is-last': isLast && styleType !== 'card',
            },
          ],
    ]"
    role="button"
    tabindex="0"
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
        <div class="chrome-tab__background absolute inset-0 z-[-1] px-[6px] py-0">
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
          class="chrome-tab__main relative z-[2] mx-[14px] flex h-full min-w-[96px] items-center gap-1 pr-1"
        >
          <NIcon v-if="showIcon && item.meta?.icon" size="13" class="flex-shrink-0 opacity-70">
            <Icon :icon="item.meta.icon as string" />
          </NIcon>
          <span class="chrome-tab__title">{{ item.displayTitle }}</span>
          <button
            v-if="item.closable && !item.pinned"
            class="chrome-tab__close flex h-4 w-4 items-center justify-center rounded-full"
            type="button"
            aria-label="关闭标签页"
            @click.stop="emit('close', item.path, $event)"
          >
            <NIcon size="12"><Icon icon="lucide:x" /></NIcon>
          </button>
          <button
            v-else-if="item.pinned && item.path !== HOME_PATH"
            class="chrome-tab__pin flex h-4 w-4 items-center justify-center rounded-full"
            type="button"
            aria-label="取消固定"
            @click.stop="emit('togglePin', item.path)"
          >
            <NIcon size="12"><Icon icon="lucide:pin" /></NIcon>
          </button>
        </div>
      </div>
    </template>

    <!-- ======== Plain / Card / Brisk 风格内容 ======== -->
    <template v-else>
      <div class="flat-tab__inner relative flex h-full items-center gap-1 px-4">
        <NIcon v-if="showIcon && item.meta?.icon" size="13" class="flex-shrink-0 opacity-70">
          <Icon :icon="item.meta.icon as string" />
        </NIcon>
        <span class="flat-tab__title">{{ item.displayTitle }}</span>
        <button
          v-if="item.closable && !item.pinned"
          class="flat-tab__close flex h-4 w-4 items-center justify-center rounded-full"
          type="button"
          aria-label="关闭标签页"
          @click.stop="emit('close', item.path, $event)"
        >
          <NIcon size="12"><Icon icon="lucide:x" /></NIcon>
        </button>
        <button
          v-else-if="item.pinned && item.path !== HOME_PATH"
          class="flat-tab__pin flex h-4 w-4 items-center justify-center rounded-full"
          type="button"
          aria-label="取消固定"
          @click.stop="emit('togglePin', item.path)"
        >
          <NIcon size="12"><Icon icon="lucide:pin" /></NIcon>
        </button>
      </div>
    </template>
  </div>
</template>

<style scoped>
/* ========================================================
   TransitionGroup FLIP 拖拽归位动画
   注意：不能在基础 .tab-item 上加 transition:transform，
   否则 SortableJS 的 FLIP 反转会导致拖拽动画异常。
   ======================================================== */
.tab-item.tabs-slide-move {
  transition: transform 0.38s cubic-bezier(0.22, 1, 0.36, 1);
}

/* ============ Chrome 风格 ============ */
.chrome-tab {
  font-size: 13px;
  transition: color 0.18s ease;
}

.chrome-tab:not(.chrome-tab--dragging) {
  cursor: pointer;
}

.chrome-tab.is-active {
  z-index: 2;
}

.chrome-tab__divider {
  background: hsl(var(--border));
  transition: opacity 0.15s ease;
}

.chrome-tab__background-content {
  background: transparent;
  transition: all 0.15s ease-in-out;
}

.chrome-tab__background-before,
.chrome-tab__background-after {
  fill: transparent;
  transition: fill 0.15s ease;
}

.chrome-tab__main {
  color: hsl(var(--muted-foreground));
  transition: color 0.15s ease-in-out;
}

.chrome-tab__title {
  flex: 1;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.chrome-tab__close {
  border: 0;
  background: transparent;
  padding: 0;
  color: currentcolor;
  opacity: 0.65;
  transition: all 0.2s ease;
}

.chrome-tab__close:hover {
  background: hsl(var(--accent));
  color: hsl(var(--accent-foreground));
}

.chrome-tab__pin {
  border: 0;
  background: transparent;
  padding: 0;
  color: var(--tab-active-color);
  opacity: 0.75;
  cursor: pointer;
  transition: all 0.2s ease;
}

.chrome-tab__pin:hover {
  opacity: 1;
  background: hsl(var(--accent));
  color: hsl(var(--accent-foreground));
}

.tab-item.chrome-tab:hover:not(.is-active) .chrome-tab__background-content {
  margin-left: 2px;
  margin-right: 2px;
  border-radius: 6px;
  background: hsl(var(--accent));
}

.tab-item.chrome-tab:hover:not(.is-active) .chrome-tab__divider {
  opacity: 0;
}

.tab-item.chrome-tab.is-active .chrome-tab__main {
  color: var(--tab-active-color);
}

.tab-item.chrome-tab.is-active .chrome-tab__background-content {
  background: var(--tab-active-bg);
}

.tab-item.chrome-tab.is-active .chrome-tab__background-before,
.tab-item.chrome-tab.is-active .chrome-tab__background-after {
  fill: var(--tab-active-bg);
}

.tab-item.chrome-tab.is-active + .tab-item.chrome-tab .chrome-tab__divider,
.tab-item.chrome-tab:hover + .tab-item.chrome-tab .chrome-tab__divider {
  opacity: 0;
}

/* ============ Flat 通用基础 ============ */
.flat-tab {
  font-size: 13px;
  cursor: pointer;
  color: hsl(var(--muted-foreground));
}

.flat-tab__title {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  max-width: 120px;
}

.flat-tab__close,
.flat-tab__pin {
  border: 0;
  background: transparent;
  padding: 0;
  color: currentcolor;
  opacity: 0.65;
  cursor: pointer;
  flex-shrink: 0;
  transition: all 0.2s ease;
}

.flat-tab__close:hover {
  background: hsl(var(--accent));
  color: hsl(var(--accent-foreground));
  opacity: 1;
}

.flat-tab__pin:hover {
  opacity: 1;
  background: hsl(var(--accent));
  color: hsl(var(--accent-foreground));
}

/* ---- Plain 风格 ---- */
.flat-tab--plain {
  height: 100%;
  transition:
    color 0.18s ease,
    background 0.18s ease;
}

.flat-tab--plain.has-left-divider {
  border-left: 1px solid hsl(var(--border));
}

.flat-tab--plain.is-last {
  border-right: 1px solid hsl(var(--border));
}

.flat-tab--plain:hover:not(.is-active) {
  background: hsl(var(--accent));
  color: hsl(var(--foreground));
}

.flat-tab--plain.is-active {
  background: var(--tab-active-bg);
  color: var(--tab-active-color);
}

/* ---- Card 风格 ---- */
.flat-tab--card {
  height: calc(100% - 8px);
  margin-top: 4px;
  margin-left: 6px;
  border-radius: 6px;
  border: 1px solid hsl(var(--border));
  transition:
    color 0.18s ease,
    background 0.18s ease,
    border-color 0.18s ease;
}

.flat-tab--card:hover:not(.is-active) {
  background: hsl(var(--accent));
  color: hsl(var(--foreground));
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
    color 0.18s ease,
    background 0.18s ease;
}

.flat-tab--brisk.has-left-divider {
  border-left: 1px solid hsl(var(--border));
}

.flat-tab--brisk.is-last {
  border-right: 1px solid hsl(var(--border));
}

.flat-tab--brisk::after {
  content: '';
  position: absolute;
  bottom: 0;
  left: 0;
  width: 100%;
  height: 2px;
  background: var(--tab-active-color);
  transform: scaleX(0);
  transform-origin: left;
  transition: transform 0.28s cubic-bezier(0.4, 0, 0.2, 1);
}

.flat-tab--brisk:hover::after,
.flat-tab--brisk.is-active::after {
  transform: scaleX(1);
}

.flat-tab--brisk:hover:not(.is-active) {
  background: hsl(var(--accent));
  color: hsl(var(--foreground));
}

.flat-tab--brisk.is-active {
  color: var(--tab-active-color);
}
</style>
