<script setup lang="ts">
import { NIcon } from 'naive-ui'
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { STORAGE_PREFIX } from '~/constants'
import { Icon } from '~/iconify'
import { LocalStorage } from '~/utils'

defineOptions({ name: 'PreferenceFab' })
defineProps<{ show: boolean }>()
const emit = defineEmits<{ click: [] }>()

const { t } = useI18n()

/** 悬浮按钮位置（贴边方向 + 垂直位置），本地持久化（按设备/视口，无需上行后端） */
const FAB_POS_KEY = `${STORAGE_PREFIX}widget_preference_fab_pos`
const SIZE = 40
/** 吸附后与边缘的间距 */
const EDGE_GAP = 12
/** 位移超过该阈值才视为「拖动」，否则当作「点击」打开偏好设置 */
const DRAG_THRESHOLD = 4

interface FabPos {
  side: 'left' | 'right'
  top: number
}

const side = ref<'left' | 'right'>('right')
const left = ref(0)
const top = ref(0)
const dragging = ref(false)

let moved = false
let startX = 0
let startY = 0
let originLeft = 0
let originTop = 0

function viewportWidth() {
  return typeof window === 'undefined' ? 1200 : window.innerWidth
}

function viewportHeight() {
  return typeof window === 'undefined' ? 800 : window.innerHeight
}

/** 贴边后的水平像素位置 */
function snappedLeft(s: 'left' | 'right') {
  return s === 'left' ? EDGE_GAP : viewportWidth() - SIZE - EDGE_GAP
}

function clampTop(value: number) {
  return Math.min(viewportHeight() - SIZE - EDGE_GAP, Math.max(EDGE_GAP, value))
}

function clampLeft(value: number) {
  return Math.min(viewportWidth() - SIZE - EDGE_GAP, Math.max(EDGE_GAP, value))
}

/** 应用贴边态：水平吸附到指定边，垂直夹取到可视区内 */
function applySnapped(s: 'left' | 'right', verticalTop: number) {
  side.value = s
  top.value = clampTop(verticalTop)
  left.value = snappedLeft(s)
}

function loadPos() {
  const saved = LocalStorage.get<FabPos>(FAB_POS_KEY)
  if (saved && (saved.side === 'left' || saved.side === 'right') && typeof saved.top === 'number') {
    applySnapped(saved.side, saved.top)
  }
  else {
    // 默认：右下角，贴近原固定位置（right:12 / bottom:88）
    applySnapped('right', viewportHeight() - SIZE - 88)
  }
}

function persist() {
  LocalStorage.set(FAB_POS_KEY, { side: side.value, top: top.value })
}

function onPointerDown(e: PointerEvent) {
  // 仅左键 / 触摸 / 笔触发
  if (e.pointerType === 'mouse' && e.button !== 0) {
    return
  }
  dragging.value = true
  moved = false
  startX = e.clientX
  startY = e.clientY
  originLeft = left.value
  originTop = top.value
  ;(e.currentTarget as HTMLElement).setPointerCapture?.(e.pointerId)
}

function onPointerMove(e: PointerEvent) {
  if (!dragging.value) {
    return
  }
  const dx = e.clientX - startX
  const dy = e.clientY - startY
  if (!moved && Math.hypot(dx, dy) > DRAG_THRESHOLD) {
    moved = true
  }
  if (moved) {
    e.preventDefault()
    left.value = clampLeft(originLeft + dx)
    top.value = clampTop(originTop + dy)
  }
}

function onPointerUp(e: PointerEvent) {
  if (!dragging.value) {
    return
  }
  dragging.value = false
  try {
    ;(e.currentTarget as HTMLElement).releasePointerCapture?.(e.pointerId)
  }
  catch {
    // 指针可能已被隐式释放，忽略
  }
  if (moved) {
    // 边缘吸附：按中心点落在左/右半屏决定贴向哪条边
    const center = left.value + SIZE / 2
    applySnapped(center < viewportWidth() / 2 ? 'left' : 'right', top.value)
    persist()
  }
}

/** 原生点击打开偏好设置；拖动后浏览器补发的 click 在此被吞掉，避免「拖完即打开」 */
function onClick() {
  if (moved) {
    moved = false
    return
  }
  emit('click')
}

function onResize() {
  applySnapped(side.value, top.value)
}

// 客户端环境下同步计算初始位置，避免首帧出现在 (0,0) 再跳位
if (typeof window !== 'undefined') {
  loadPos()
}

onMounted(() => {
  loadPos()
  window.addEventListener('resize', onResize)
})

onBeforeUnmount(() => {
  window.removeEventListener('resize', onResize)
})

const fabStyle = computed(() => ({ left: `${left.value}px`, top: `${top.value}px` }))
</script>

<template>
  <button
    v-if="show"
    class="xh-preference-fab"
    :class="{ 'is-dragging': dragging }"
    type="button"
    :style="fabStyle"
    :title="t('preference.drawer.title')"
    :aria-label="t('preference.drawer.title')"
    @click="onClick"
    @pointerdown="onPointerDown"
    @pointermove="onPointerMove"
    @pointerup="onPointerUp"
    @pointercancel="onPointerUp"
  >
    <NIcon size="20">
      <Icon icon="lucide:settings-2" />
    </NIcon>
  </button>
</template>

<style scoped>
/* 可拖动的偏好设置悬浮按钮：固定定位、贴边吸附，z-index 提升避免被表格固定列等遮挡 */
.xh-preference-fab {
  position: fixed;
  z-index: 1000;
  display: flex;
  align-items: center;
  justify-content: center;
  width: 40px;
  height: 40px;
  padding: 0;
  border: none;
  border-radius: 50%;
  color: #fff;
  background: hsl(var(--primary));
  box-shadow: 0 6px 16px rgb(0 0 0 / 18%);
  cursor: grab;
  /* 拖动时不触发页面滚动 / 文本选中 */
  touch-action: none;
  user-select: none;
  /* 松手后水平滑向吸附边的过渡（拖动中禁用，见 is-dragging） */
  transition:
    left 0.25s cubic-bezier(0.22, 1, 0.36, 1),
    box-shadow 0.2s ease,
    transform 0.2s ease;
}

.xh-preference-fab:hover {
  box-shadow: 0 8px 22px rgb(0 0 0 / 24%);
}

.xh-preference-fab.is-dragging {
  cursor: grabbing;
  /* 跟手：拖动中去掉过渡 */
  transition: none;
  transform: scale(1.06);
}
</style>
