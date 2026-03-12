<script lang="ts" setup>
import type { TabItem } from '~/types'
import { Icon } from '~/iconify'
import { NIcon } from 'naive-ui'
import { computed } from 'vue'
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

function resolveIcon(icon: string) {
  if (!icon) {
    return icon
  }
  return icon.includes(':') ? icon : `lucide:${icon}`
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
</script>

<template>
  <!--
    单一根 <div>，内容通过 <template v-if> 区分 chrome 与 flat 风格。
    TransitionGroup 始终看到同一 DOM 节点类型，切换风格时只做原地 patch，
    不触发 leave/enter 动画，彻底消除切换闪烁。
  -->
  <div
    class="tab-item group relative flex shrink-0 select-none"
    :class="tabClass"
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
          <NIcon v-if="showIcon && item.meta?.icon" size="13" class="flex-shrink-0 opacity-70">
            <Icon :icon="resolveIcon(item.meta.icon as string)" />
          </NIcon>
          <span class="chrome-tab__title">{{ item.displayTitle }}</span>
          <button
            v-if="item.closable && !item.pinned"
            class="chrome-tab__close chrome-tab__action flex h-5 w-5 items-center justify-center rounded-full"
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
            class="chrome-tab__pin chrome-tab__action flex h-5 w-5 items-center justify-center rounded-full"
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
    </template>

    <!-- ======== Plain / Card / Brisk 风格内容 ======== -->
    <template v-else>
      <div class="flat-tab__inner relative flex h-full items-center gap-1 px-4">
        <NIcon v-if="showIcon && item.meta?.icon" size="13" class="flex-shrink-0 opacity-70">
          <Icon :icon="resolveIcon(item.meta.icon as string)" />
        </NIcon>
        <span class="flat-tab__title">{{ item.displayTitle }}</span>
        <button
          v-if="item.closable && !item.pinned"
          class="flat-tab__close flat-tab__action flex h-5 w-5 items-center justify-center rounded-full"
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
          class="flat-tab__pin flat-tab__action flex h-5 w-5 items-center justify-center rounded-full"
          type="button"
          aria-label="取消固定"
          @click.stop="emit('togglePin', item.path)"
        >
          <NIcon size="12">
            <Icon icon="lucide:pin" />
          </NIcon>
        </button>
      </div>
    </template>
  </div>
</template>

<style scoped>
/* ============ Chrome 风格 ============ */
.chrome-tab {
  font-size: 13px;
  font-weight: 500;
  transition: color 0.18s ease;
}

.chrome-tab:not(.chrome-tab--dragging) {
  cursor: default;
}

.chrome-tab.is-active {
  z-index: 2;
  margin-bottom: -1px;
  padding-bottom: 1px;
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
  border-radius: 6px;
  transition:
    color 0.15s ease-in-out,
    background-color 0.15s ease-in-out,
    box-shadow 0.15s ease-in-out;
}

.chrome-tab__title {
  flex: 1;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.chrome-tab__action {
  margin-left: 8px;
}

.chrome-tab__close {
  border: 0;
  background: transparent;
  padding: 0;
  color: currentcolor;
  opacity: 0.72;
  transform: scale(1);
  cursor: pointer;
  transition: all 0.2s ease;
}

.chrome-tab__pin {
  border: 0;
  background: transparent;
  padding: 0;
  color: currentcolor;
  opacity: 0.72;
  transform: scale(1);
  cursor: default;
  transition: all 0.2s ease;
}

.tab-item.chrome-tab:hover .chrome-tab__close,
.tab-item.chrome-tab:hover .chrome-tab__pin,
.tab-item.chrome-tab.is-active .chrome-tab__close,
.tab-item.chrome-tab.is-active .chrome-tab__pin {
  opacity: 0.92;
  transform: scale(1);
}

.chrome-tab__close:hover,
.chrome-tab__pin:hover {
  opacity: 1 !important;
  background: color-mix(in srgb, hsl(var(--primary)) 20%, hsl(var(--background)));
  color: hsl(var(--foreground));
  box-shadow: inset 0 0 0 1px hsl(var(--primary) / 28%);
}

.tab-item.chrome-tab:hover:not(.is-active) {
  z-index: 2;
  margin-bottom: -1px;
  padding-bottom: 1px;
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
  background: color-mix(in srgb, hsl(var(--primary)) 8%, hsl(var(--accent)));
}

.tab-item.chrome-tab:hover:not(.is-active) .chrome-tab__background-before,
.tab-item.chrome-tab:hover:not(.is-active) .chrome-tab__background-after {
  fill: color-mix(in srgb, hsl(var(--primary)) 8%, hsl(var(--accent)));
}

.tab-item.chrome-tab.is-active + .tab-item.chrome-tab .chrome-tab__divider,
.tab-item.chrome-tab:hover + .tab-item.chrome-tab .chrome-tab__divider {
  opacity: 0;
}

/* ============ Flat 通用基础 ============ */
.flat-tab {
  font-size: 13px;
  font-weight: 500;
  cursor: default;
  color: hsl(var(--muted-foreground));
}

.flat-tab__title {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  max-width: 120px;
}

.flat-tab__action {
  margin-left: 8px;
}

.flat-tab__close,
.flat-tab__pin {
  border: 0;
  background: transparent;
  padding: 0;
  color: currentcolor;
  opacity: 0.72;
  transform: scale(1);
  cursor: default;
  flex-shrink: 0;
  transition: all 0.2s ease;
}

.flat-tab__close {
  cursor: pointer;
}

.tab-item.flat-tab:hover .flat-tab__close,
.tab-item.flat-tab:hover .flat-tab__pin,
.tab-item.flat-tab.is-active .flat-tab__close,
.tab-item.flat-tab.is-active .flat-tab__pin {
  opacity: 0.92;
  transform: scale(1);
}

.flat-tab__close:hover {
  background: color-mix(in srgb, hsl(var(--primary)) 20%, hsl(var(--background)));
  color: hsl(var(--foreground));
  box-shadow: inset 0 0 0 1px hsl(var(--primary) / 28%);
  opacity: 1 !important;
}

.flat-tab__pin:hover {
  opacity: 1 !important;
  background: color-mix(in srgb, hsl(var(--primary)) 20%, hsl(var(--background)));
  color: hsl(var(--foreground));
  box-shadow: inset 0 0 0 1px hsl(var(--primary) / 28%);
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
  background: color-mix(in srgb, hsl(var(--primary)) 14%, hsl(var(--accent)));
  color: hsl(var(--foreground));
}

.flat-tab--plain.is-active {
  background: var(--tab-active-bg);
  color: var(--tab-active-color);
}

/* ---- Card 风格 ---- */
.flat-tab--card {
  height: calc(100% - 4px);
  margin-top: 2px;
  margin-left: 6px;
  border-radius: 6px;
  border: 1px solid hsl(var(--border));
  transition:
    color 0.18s ease,
    background 0.18s ease,
    border-color 0.18s ease;
}

.flat-tab--card:hover:not(.is-active) {
  background: color-mix(in srgb, hsl(var(--primary)) 14%, hsl(var(--accent)));
  color: hsl(var(--foreground));
  border-color: hsl(var(--primary) / 30%);
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
  background: color-mix(in srgb, hsl(var(--primary)) 14%, hsl(var(--accent)));
  color: hsl(var(--foreground));
}

.flat-tab--brisk.is-active {
  color: var(--tab-active-color);
}
</style>
