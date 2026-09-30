<script setup lang="ts">
import type { FloatButtonPlacement } from '@xihan-ui/headless'
import type { ComponentPublicInstance } from 'vue'
import { useFullscreen, useWindowSize } from '@vueuse/core'
import { FLOAT_BUTTON_DEFAULT_OFFSET } from '@xihan-ui/headless'
import { XhFloatButtonList, XhFloatButtonRoot, XhFloatButtonTrigger } from '@xihan-ui/vue'
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import LocaleSwitcher from '~/components/common/LocaleSwitcher.vue'
import TimezoneSwitcher from '~/components/common/TimezoneSwitcher.vue'
import { WIDGET_FLOAT_TOOLBAR_POSITION_KEY } from '~/constants'
import { useTheme } from '~/hooks'
import { Icon } from '~/iconify'
import { useLayoutBridgeStore } from '~/stores'
import { LocalStorage } from '~/utils'
import { useWidgetPlacement } from '../composables'

/**
 * 悬浮工具组：命令面板、语言、时区、主题、全屏与偏好设置里，位置落到「悬浮」的都收进这一组。
 * 落位与顶栏共用 useWidgetPlacement 判定，同一个工具不会两处同时出现；一个都没有时整组不渲染。
 *
 * 组件库的悬浮按钮钉在角上，这里补上拖动：按住触发器可拖到任意处，松手贴向近的那条边；
 * 默认贴右边、停在右下角四分之一处（不压分页、页脚与回到顶部），视口尺寸一变就回到默认位置。
 */
defineOptions({ name: 'AppFloatToolbar' })

type Side = 'left' | 'right'

interface FloatToolbarPosition {
  side: Side
  top: number
  viewportWidth: number
  viewportHeight: number
}

/** 默认落点：触发器中心在视口高度的四分之三处 */
const DEFAULT_CENTER_RATIO = 0.75
/** 贴边距离：与组件库悬浮按钮缺省的贴边距离一致 */
const EDGE_GAP = FLOAT_BUTTON_DEFAULT_OFFSET
/** 位移超过它才算拖动，否则是点击展开 */
const DRAG_THRESHOLD = 4

const { t } = useI18n()
const layoutBridgeStore = useLayoutBridgeStore()
const placement = useWidgetPlacement()
const { isDark, toggleThemeWithTransition } = useTheme()
const { isFullscreen, toggle: toggleFullscreen } = useFullscreen()
const { width: viewportWidth, height: viewportHeight } = useWindowSize({ includeScrollbar: false })

const open = ref(false)
const translations = computed(() => ({ trigger: t('header.toolbar.float_tools') }))

// ---- 位置与拖动 ----
const triggerRef = ref<ComponentPublicInstance | null>(null)
/** 触发器边长：随组件库尺寸档走，挂上后量一次 */
const triggerSize = ref(0)
const side = ref<Side>('right')
/** 触发器上沿离视口顶的距离 */
const top = ref(0)
/** 拖动中触发器左沿的横坐标；不在拖动时为 null，横向位置由贴边决定 */
const dragLeft = ref<number | null>(null)

function clampTop(value: number) {
  return Math.min(viewportHeight.value - triggerSize.value - EDGE_GAP, Math.max(EDGE_GAP, value))
}

function clampLeft(value: number) {
  return Math.min(viewportWidth.value - triggerSize.value - EDGE_GAP, Math.max(EDGE_GAP, value))
}

function placeDefault() {
  side.value = 'right'
  top.value = clampTop(viewportHeight.value * DEFAULT_CENTER_RATIO - triggerSize.value / 2)
}

/** 本机记着的位置只在视口尺寸没变时作数 */
function restorePosition() {
  const saved = LocalStorage.get<FloatToolbarPosition>(WIDGET_FLOAT_TOOLBAR_POSITION_KEY)
  if (
    saved
    && (saved.side === 'left' || saved.side === 'right')
    && Number.isFinite(saved.top)
    && saved.viewportWidth === viewportWidth.value
    && saved.viewportHeight === viewportHeight.value
  ) {
    side.value = saved.side
    top.value = clampTop(saved.top)
    return
  }
  placeDefault()
}

function savePosition() {
  const position: FloatToolbarPosition = {
    side: side.value,
    top: top.value,
    viewportWidth: viewportWidth.value,
    viewportHeight: viewportHeight.value,
  }
  LocalStorage.set(WIDGET_FLOAT_TOOLBAR_POSITION_KEY, position)
}

// 整组出现时量触发器、摆到位
watch(triggerRef, (instance) => {
  const el = instance?.$el as HTMLElement | undefined
  if (!el) {
    return
  }
  triggerSize.value = el.offsetWidth
  restorePosition()
})

// 视口一变（窗口缩放、旋转屏幕）就回到默认位置
watch([viewportWidth, viewportHeight], () => {
  LocalStorage.remove(WIDGET_FLOAT_TOOLBAR_POSITION_KEY)
  placeDefault()
})

const left = computed(() =>
  dragLeft.value ?? (side.value === 'left' ? EDGE_GAP : viewportWidth.value - triggerSize.value - EDGE_GAP))
/** 触发器在上半屏时整组往下展开，在下半屏时往上展开，展开的那一截不出屏 */
const inUpperHalf = computed(() => top.value + triggerSize.value / 2 < viewportHeight.value / 2)
const rootPlacement = computed<FloatButtonPlacement>(() =>
  `${inUpperHalf.value ? 'top' : 'bottom'}-${side.value === 'left' ? 'start' : 'end'}`)
/** 语言、时区菜单朝屏幕里面弹 */
const menuPlacement = computed(() =>
  `${side.value === 'left' ? 'right' : 'left'}-${inUpperHalf.value ? 'start' : 'end'}` as const)
// 组件库按落位贴角；这里四条边都写死，盖掉贴角的那两条，按触发器的坐标摆整组
const rootStyle = computed(() => ({
  left: `${left.value}px`,
  right: 'auto',
  top: inUpperHalf.value ? `${top.value}px` : 'auto',
  bottom: inUpperHalf.value ? 'auto' : `${viewportHeight.value - top.value - triggerSize.value}px`,
}))

let pointerId: number | null = null
let moved = false
let startX = 0
let startY = 0
let originLeft = 0
let originTop = 0

function onPointerDown(event: PointerEvent) {
  if (event.pointerType === 'mouse' && event.button !== 0) {
    return
  }
  pointerId = event.pointerId
  moved = false
  startX = event.clientX
  startY = event.clientY
  originLeft = left.value
  originTop = top.value
  ;(event.currentTarget as HTMLElement).setPointerCapture(event.pointerId)
}

function onPointerMove(event: PointerEvent) {
  if (event.pointerId !== pointerId) {
    return
  }
  const dx = event.clientX - startX
  const dy = event.clientY - startY
  if (!moved) {
    if (Math.hypot(dx, dy) <= DRAG_THRESHOLD) {
      return
    }
    moved = true
    open.value = false
  }
  event.preventDefault()
  dragLeft.value = clampLeft(originLeft + dx)
  top.value = clampTop(originTop + dy)
}

/** 松手：按触发器中心落在哪半屏贴向那条边，记下位置 */
function finishDrag(event: PointerEvent) {
  if (event.pointerId !== pointerId) {
    return
  }
  pointerId = null
  const el = event.currentTarget as HTMLElement
  if (el.hasPointerCapture(event.pointerId)) {
    el.releasePointerCapture(event.pointerId)
  }
  if (dragLeft.value === null) {
    return
  }
  side.value = dragLeft.value + triggerSize.value / 2 < viewportWidth.value / 2 ? 'left' : 'right'
  dragLeft.value = null
  savePosition()
}

function onPointerCancel(event: PointerEvent) {
  finishDrag(event)
  // 取消的指针后面没有 click，不留着拦下一次点击
  moved = false
}

/** 拖完浏览器补发的 click 不能再去展开整组：在捕获阶段拦在根上，触发器自己的点击处理收不到 */
function onClickCapture(event: MouseEvent) {
  if (!moved) {
    return
  }
  moved = false
  event.stopPropagation()
  event.preventDefault()
}

// ---- 动作 ----
/** 点了动作就收起整组；语言与时区是菜单，选完由菜单自己收起，组保持展开以便看到结果 */
function run(action: () => void) {
  open.value = false
  action()
}

function toggleTheme(event: MouseEvent) {
  open.value = false
  toggleThemeWithTransition(event)
}

/**
 * Iconify 的 svg 只认 1em，而 ui.css 按 Action Control 档位给按钮里的图标定的尺寸不是 floating 档；
 * 悬浮组里的图标一律接组件库下发的字形尺，与触发器兜底字形同一把尺
 */
const glyphStyle = { inlineSize: 'var(--xh-icon-size)', blockSize: 'var(--xh-icon-size)' }
</script>

<template>
  <XhFloatButtonRoot
    v-if="placement.hasFloating.value"
    v-model:open="open"
    class="float-toolbar"
    :class="{ 'is-dragging': dragLeft !== null }"
    :style="rootStyle"
    :placement="rootPlacement"
    :translations="translations"
    @click.capture="onClickCapture"
  >
    <XhFloatButtonTrigger
      ref="triggerRef"
      class="float-trigger"
      @pointerdown="onPointerDown"
      @pointermove="onPointerMove"
      @pointerup="finishDrag"
      @pointercancel="onPointerCancel"
    >
      <!-- 收起时是螺母（快捷工具），展开后换成关闭，读得出再按一下就收起 -->
      <Icon :icon="open ? 'lucide:x' : 'lucide:bolt'" :style="glyphStyle" />
    </XhFloatButtonTrigger>
    <XhFloatButtonList>
      <button
        v-if="placement.search.value === 'floating'"
        type="button"
        :aria-label="t('header.search.placeholder')"
        :title="t('header.search.placeholder')"
        @click="run(() => layoutBridgeStore.requestOpenGlobalSearch())"
      >
        <Icon icon="lucide:search" :style="glyphStyle" />
      </button>
      <LocaleSwitcher v-if="placement.language.value === 'floating'" variant="dropdown" apply :placement="menuPlacement">
        <button
          type="button"
          class="float-menu-item"
          data-xh-material="frosted"
          :aria-label="t('header.toolbar.switch_language')"
          :title="t('header.toolbar.switch_language')"
        >
          <Icon icon="lucide:languages" :style="glyphStyle" />
        </button>
      </LocaleSwitcher>
      <TimezoneSwitcher v-if="placement.timezone.value === 'floating'" variant="dropdown" apply :placement="menuPlacement">
        <button
          type="button"
          class="float-menu-item"
          data-xh-material="frosted"
          :aria-label="t('header.toolbar.switch_timezone')"
          :title="t('header.toolbar.switch_timezone')"
        >
          <Icon icon="lucide:clock-3" :style="glyphStyle" />
        </button>
      </TimezoneSwitcher>
      <button
        v-if="placement.theme.value === 'floating'"
        type="button"
        :aria-label="isDark ? t('header.toolbar.theme_to_light') : t('header.toolbar.theme_to_dark')"
        :title="isDark ? t('header.toolbar.theme_to_light') : t('header.toolbar.theme_to_dark')"
        @click="toggleTheme"
      >
        <Icon :icon="isDark ? 'lucide:sun' : 'lucide:moon'" :style="glyphStyle" />
      </button>
      <button
        v-if="placement.fullscreen.value === 'floating'"
        type="button"
        :aria-label="isFullscreen ? t('header.toolbar.fullscreen_exit') : t('header.toolbar.fullscreen_enter')"
        :title="isFullscreen ? t('header.toolbar.fullscreen_exit') : t('header.toolbar.fullscreen_enter')"
        @click="run(toggleFullscreen)"
      >
        <Icon :icon="isFullscreen ? 'lucide:minimize' : 'lucide:maximize'" :style="glyphStyle" />
      </button>
      <button
        v-if="placement.preference.value === 'floating'"
        type="button"
        :aria-label="t('header.toolbar.preferences')"
        :title="t('header.toolbar.preferences')"
        @click="run(() => layoutBridgeStore.requestOpenPreferenceDrawer())"
      >
        <!-- 与顶栏的偏好设置按钮同一个图标 -->
        <Icon icon="lucide:settings-2" :style="glyphStyle" />
      </button>
    </XhFloatButtonList>
  </XhFloatButtonRoot>
</template>

<style scoped>
/* 松手后滑向贴靠的那条边；拖动中跟手，不过渡 */
.float-toolbar {
  transition: left var(--xh-motion-duration-move) var(--xh-motion-ease-enter-strong);
}

.float-toolbar.is-dragging {
  transition: none;
}

/* 触发器兼作拖动把手：按住拖动时不滚页面、不选中文字 */
.float-trigger {
  touch-action: none;
  user-select: none;
  cursor: grab;
}

.float-toolbar.is-dragging .float-trigger {
  cursor: grabbing;
}

/* 语言与时区是菜单：菜单触发器带着自己的 data-scope，悬浮组皮肤只给不带 data-scope 的原生按钮画面，
   这两颗的面改从材质配方取（data-xh-material="frosted"，即缺省 outline 档同一份磨砂面），
   这里只补与原生动作项同一张表的交互阶梯 */
.float-menu-item {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  padding: 0;
  font: inherit;
  cursor: pointer;
  transition:
    background-color var(--xh-motion-duration-micro) var(--xh-motion-ease-enter),
    scale var(--xh-motion-duration-release) var(--xh-motion-ease-release);
}

.float-menu-item:focus-visible {
  outline: var(--xh-ring-width) solid var(--xh-ring-focus);
  outline-offset: var(--xh-ring-offset);
}

/* 菜单展开时停在悬停档，读得出是哪一颗弹的菜单 */
.float-menu-item[data-state='open'] {
  background-color: var(--xh-bg-subtle-opaque);
}

@media (hover: hover) {
  .float-menu-item:hover {
    background-color: var(--xh-bg-subtle-opaque);
  }
}

.float-menu-item:active {
  background-color: var(--xh-bg-subtle-hover-opaque);
  scale: var(--xh-motion-scale-press);
  transition-duration: var(--xh-motion-duration-press);
  transition-timing-function: var(--xh-motion-ease-press);
}
</style>
