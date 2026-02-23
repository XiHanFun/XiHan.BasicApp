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
}

const props = defineProps<Props>()

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
  <div
    class="chrome-tab group relative -mr-3 flex h-8 shrink-0 select-none items-center"
    :class="{
      'is-active': active,
      'is-last-tab': isLast,
      'affix-tab': Boolean(item.pinned),
      draggable,
    }"
    role="button"
    tabindex="0"
    @click="emit('jump', item.path)"
    @mousedown.middle.prevent
    @auxclick.prevent="onAuxClick"
    @contextmenu.prevent="emit('contextmenu', $event, item)"
    @keydown.enter.prevent="emit('jump', item.path)"
  >
    <div class="relative size-full px-1">
      <div
        v-if="props.index > 0 && !active"
        class="chrome-tab__divider absolute left-[7px] top-1/2 z-0 h-4 w-[1px] -translate-y-1/2"
      />

      <div class="chrome-tab__background absolute inset-0 z-[-1] px-[6px] py-0">
        <div class="chrome-tab__background-content h-full rounded-tl-[7px] rounded-tr-[7px]" />
        <svg class="chrome-tab__background-before absolute bottom-0 left-[-1px]" height="7" width="7">
          <path d="M 0 7 A 7 7 0 0 0 7 0 L 7 7 Z" />
        </svg>
        <svg class="chrome-tab__background-after absolute bottom-0 right-[-1px]" height="7" width="7">
          <path d="M 0 0 A 7 7 0 0 0 7 7 L 0 7 Z" />
        </svg>
      </div>

      <div class="chrome-tab__main relative z-[2] mx-[14px] flex h-full min-w-[96px] items-center gap-1 pr-1">
        <NIcon v-if="showIcon && item.meta?.icon" size="13" class="flex-shrink-0 opacity-70">
          <Icon :icon="(item.meta.icon as string)" />
        </NIcon>
        <span class="chrome-tab__title">{{ item.displayTitle }}</span>
        <button
          v-if="item.closable && !item.pinned"
          class="chrome-tab__close flex h-4 w-4 items-center justify-center rounded-full"
          type="button"
          aria-label="关闭标签页"
          @click.stop="emit('close', item.path, $event)"
        >
          <NIcon size="12">
            <Icon icon="lucide:x" />
          </NIcon>
        </button>
        <button
          v-else-if="item.pinned && item.path !== HOME_PATH"
          class="chrome-tab__pin flex h-4 w-4 items-center justify-center rounded-full"
          type="button"
          aria-label="取消固定"
          @click.stop="emit('togglePin', item.path)"
        >
          <NIcon size="12">
            <Icon icon="lucide:pin" />
          </NIcon>
        </button>
      </div>
    </div>
  </div>
</template>

<style scoped>
/* tabs-slide-move：Vue TransitionGroup FLIP 位移动画（拖拽结束后其余标签平滑归位）
   注意：不能在基础 .chrome-tab 上加 transition:transform，否则 SortableJS 的 FLIP 反转步骤
   会变成动画而非瞬间跳变，导致拖拽位移动画失效。此类只在 Vue 需要 move 时生效。 */
.chrome-tab.tabs-slide-move {
  transition: transform 0.38s cubic-bezier(0.22, 1, 0.36, 1);
}

.chrome-tab {
  font-size: 13px;
  /* 只保留颜色过渡，transform 交由 SortableJS（拖拽）和 JS hooks（入场/离场）各自管理 */
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

.chrome-tab:hover:not(.is-active) .chrome-tab__background-content {
  margin-left: 2px;
  margin-right: 2px;
  border-radius: 6px;
  background: hsl(var(--accent));
}

.chrome-tab:hover:not(.is-active) .chrome-tab__divider {
  opacity: 0;
}

.chrome-tab.is-active .chrome-tab__main {
  color: var(--tab-active-color);
}

.chrome-tab.is-active .chrome-tab__background-content {
  background: var(--tab-active-bg);
}

.chrome-tab.is-active .chrome-tab__background-before,
.chrome-tab.is-active .chrome-tab__background-after {
  fill: var(--tab-active-bg);
}

.chrome-tab.is-active + .chrome-tab .chrome-tab__divider,
.chrome-tab:hover + .chrome-tab .chrome-tab__divider {
  opacity: 0;
}
</style>
