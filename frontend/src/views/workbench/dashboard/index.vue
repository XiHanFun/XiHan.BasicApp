<script setup lang="ts">
import type { DragEndEvent } from '@dnd-kit/vue'
import type { BoardItem } from './components'
import { DragDropProvider } from '@dnd-kit/vue'
import { NButton, NDrawer, NDrawerContent, NEmpty, NPopover } from 'naive-ui'
import { computed, onMounted, onUnmounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { resolveSortMove } from '~/components/common/sortable'
import SortableItem from '~/components/common/SortableItem.vue'
import SyncStatusBadge from '~/components/common/SyncStatusBadge.vue'
import { useUserSettingSync } from '~/components/schema'
import { usePermission } from '~/hooks'
import { Icon } from '~/iconify'
import { useAppStore } from '~/stores'
import { DEFAULT_BOARD, WIDGET_MAP, WIDGETS } from './components'

defineOptions({ name: 'WorkbenchDashboardPage' })

const GRID_COLS = 12
const MIN_SPAN = 1
// 尺寸选项：12 等分全部列宽（1/12…1/1），边缘拖拽实时吸附到最近的一档（每一档都吸）
const SIZE_OPTIONS = Array.from({ length: GRID_COLS - MIN_SPAN + 1 }, (_, i) => MIN_SPAN + i)

function gcd(a: number, b: number): number {
  return b === 0 ? a : gcd(b, a % b)
}
// 用最简分数 x/x 表示宽度（占整行比例）：3/12→1/4、5/12→5/12、12/12→1/1
function spanLabel(span: number): string {
  const g = gcd(span, GRID_COLS)
  return `${span / g}/${GRID_COLS / g}`
}

const { t } = useI18n()
const { hasPermission } = usePermission()
const appStore = useAppStore()
// 看板布局随用户设置同步（本地即时 + 后端跨端同步，复用 SchemaPage 同一套机制）
// 同步开关在「偏好设置 → 同步 → 仪表盘看板」，本页只读 appStore.widgetsSyncEnabled 显示状态徽标
// 场景键保持 'workbench:widgets'（历史键），避免更名导致用户已保存布局丢失
const sync = useUserSettingSync('workbench:widgets')

// 小组件是否对当前用户可见：未声明权限码则人人可见，声明则按权限码门控
function canShow(key: string): boolean {
  const def = WIDGET_MAP[key]
  return !!def && (!def.permission || hasPermission(def.permission))
}
function clampSpan(span: number) {
  if (!Number.isFinite(span))
    return 6
  return Math.min(GRID_COLS, Math.max(MIN_SPAN, Math.round(span)))
}
function sanitize(items: BoardItem[]): BoardItem[] {
  return items.filter(item => canShow(item.key)).map(item => ({ key: item.key, span: clampSpan(item.span) }))
}

const LOCAL_BOARD_KEY = 'xh:workbench:widgets:board'

const board = ref<BoardItem[]>(sanitize(DEFAULT_BOARD))
const customizing = ref(false)
const showAdd = ref(false)
// 悬浮操作栏开合：折叠时右侧只露半隐藏按钮，点击展开为半透明毛玻璃操作栏
const panelOpen = ref(false)
function onPanelEsc(event: KeyboardEvent) {
  if (event.key === 'Escape' && panelOpen.value)
    panelOpen.value = false
}
window.addEventListener('keydown', onPanelEsc)
// 编辑期间是否有改动：只有点「完成」且确有改动时才落地 + 同步一次（编辑过程不写后端、不刷同步提示）
const dirty = ref(false)

// 工具栏可上下自由拖动（避免挡住小组件操作）：竖向偏移量，按本地记忆，跨刷新保留
const TOOLBAR_OFFSET_KEY = 'xh:workbench:widgets:toolbar-offset'
function clampOffset(value: number): number {
  return Math.min(Math.max(0, window.innerHeight - 160), Math.max(0, value))
}
const toolbarOffsetY = ref(clampOffset(Number(localStorage.getItem(TOOLBAR_OFFSET_KEY)) || 0))
const draggingToolbar = ref(false)
let toolbarDrag: { y: number, base: number } | null = null
function onToolbarDragMove(event: PointerEvent) {
  if (!toolbarDrag)
    return
  toolbarOffsetY.value = clampOffset(toolbarDrag.base + (event.clientY - toolbarDrag.y))
}
function onToolbarDragEnd() {
  window.removeEventListener('pointermove', onToolbarDragMove)
  toolbarDrag = null
  draggingToolbar.value = false
  try {
    localStorage.setItem(TOOLBAR_OFFSET_KEY, String(toolbarOffsetY.value))
  }
  catch {
    // 忽略本地存储异常
  }
}
function onToolbarDragStart(event: PointerEvent) {
  event.preventDefault()
  toolbarDrag = { y: event.clientY, base: toolbarOffsetY.value }
  draggingToolbar.value = true
  window.addEventListener('pointermove', onToolbarDragMove)
  window.addEventListener('pointerup', onToolbarDragEnd, { once: true })
}

function readLocalBoard(): BoardItem[] | null {
  try {
    const raw = localStorage.getItem(LOCAL_BOARD_KEY)
    const parsed = raw ? (JSON.parse(raw) as { items?: BoardItem[] }) : null
    return Array.isArray(parsed?.items) ? parsed.items : null
  }
  catch {
    return null
  }
}
function persist() {
  const payload = { items: board.value }
  // 本地始终留一份（关闭同步时即以此为准）；sync.save 内部按「仪表盘看板同步」开关决定是否上行后端
  try {
    localStorage.setItem(LOCAL_BOARD_KEY, JSON.stringify(payload))
  }
  catch {
    // 忽略本地存储异常（隐私模式等）
  }
  sync.save('board', payload)
}

onMounted(async () => {
  // 开启同步取后端值；关闭时 hydrate 返回 undefined，回退本地
  const saved = await sync.hydrate<{ items: BoardItem[] }>('board')
  if (saved?.items?.length) {
    board.value = sanitize(saved.items)
    return
  }
  const local = readLocalBoard()
  if (local?.length)
    board.value = sanitize(local)
})

// 其它设备推送（同步关闭的分区由框架层自动跳过，无需此处再判断）
const unsubscribe = sync.subscribeRemote('board', (value) => {
  const items = (value as { items?: BoardItem[] }).items
  if (Array.isArray(items) && items.length)
    board.value = sanitize(items)
})
onUnmounted(() => {
  unsubscribe()
  window.removeEventListener('keydown', onPanelEsc)
  window.removeEventListener('pointermove', onToolbarDragMove)
})

const activeKeys = computed(() => new Set(board.value.map(item => item.key)))
const available = computed(() => WIDGETS.filter(widget => !activeKeys.value.has(widget.key) && canShow(widget.key)))
const ids = computed(() => board.value.map(item => item.key))

function onDragEnd(event: DragEndEvent) {
  const move = resolveSortMove(event, ids.value)
  if (!move)
    return
  const next = board.value.slice()
  const [moved] = next.splice(move.from, 1)
  if (!moved)
    return
  next.splice(move.to, 0, moved)
  board.value = next
  dirty.value = true
}
function addWidget(key: string) {
  const def = WIDGET_MAP[key]
  if (!def)
    return
  board.value = [...board.value, { key, span: def.defaultSpan }]
  dirty.value = true
}
function removeWidget(key: string) {
  board.value = board.value.filter(item => item.key !== key)
  dirty.value = true
}
function setSpan(item: BoardItem, span: number) {
  item.span = span
  dirty.value = true
}
function resetBoard() {
  board.value = sanitize(DEFAULT_BOARD)
  dirty.value = true
}
// 编辑期间所有改动只在内存预览；点「完成」才落地 localStorage + 同步后端一次（无改动则跳过）
function finishCustomize() {
  customizing.value = false
  panelOpen.value = false
  if (!dirty.value)
    return
  dirty.value = false
  persist()
}

// 边缘拖拽调宽：把像素位移换算成栅格列数，实时跟随光标；靠近预设档位时磁吸（左把手方向取反）
const gridRef = ref<HTMLElement | null>(null)
let resize: { item: BoardItem, startX: number, startSpan: number, colUnit: number, sign: number } | null = null

// 实时列数 → 落地列数：吸附到最近的尺寸档位（全部列宽都吸）
function resolveSpan(cols: number): number {
  const clamped = Math.min(GRID_COLS, Math.max(MIN_SPAN, cols))
  return SIZE_OPTIONS.reduce((best, span) => (Math.abs(span - clamped) < Math.abs(best - clamped) ? span : best), SIZE_OPTIONS[0] ?? MIN_SPAN)
}
function onResizeMove(event: PointerEvent) {
  if (!resize)
    return
  const delta = (event.clientX - resize.startX) * resize.sign
  const next = resolveSpan(resize.startSpan + delta / resize.colUnit)
  if (next !== resize.item.span) {
    resize.item.span = next
    dirty.value = true
  }
}
function onResizeEnd() {
  window.removeEventListener('pointermove', onResizeMove)
  resize = null
}
function onResizeStart(event: PointerEvent, item: BoardItem, side: 'left' | 'right') {
  const width = gridRef.value?.offsetWidth ?? 0
  if (width <= 0)
    return
  event.preventDefault()
  event.stopPropagation()
  resize = { item, startX: event.clientX, startSpan: item.span, colUnit: width / 12, sign: side === 'left' ? -1 : 1 }
  window.addEventListener('pointermove', onResizeMove)
  window.addEventListener('pointerup', onResizeEnd, { once: true })
}

onUnmounted(() => window.removeEventListener('pointermove', onResizeMove))
</script>

<template>
  <div class="flex flex-col gap-4 p-4 sm:p-5">
    <!-- 悬浮控制：sticky 跟随滚动、零高度不占空间。折叠=右侧半隐藏按钮；展开=半透明毛玻璃操作栏 -->
    <div class="pointer-events-none sticky top-2 z-40 -mb-4 -mr-4 flex h-0 items-start justify-end overflow-x-clip sm:-mb-5 sm:-mr-5">
      <div
        class="flex justify-end"
        :style="{ transform: `translateY(${toolbarOffsetY}px)`, transition: draggingToolbar ? 'none' : 'transform 0.15s ease' }"
      >
        <!-- 折叠态：右侧半隐藏按钮，悬停滑出 -->
        <Transition
          enter-active-class="transition-opacity duration-200"
          enter-from-class="opacity-0"
          leave-active-class="transition-opacity duration-150"
          leave-to-class="opacity-0"
        >
          <button
            v-if="!panelOpen"
            type="button"
            :title="t('workbench.widgets.customize')"
            class="group pointer-events-auto mt-2 flex h-9 translate-x-[40%] items-center gap-1.5 rounded-l-xl border border-r-0 border-border/60 bg-background/55 pl-3 pr-2 text-muted-foreground shadow-sm backdrop-blur-md transition-all duration-200 hover:translate-x-0 hover:bg-background/90 hover:text-foreground"
            @click="panelOpen = true"
          >
            <Icon icon="lucide:settings-2" width="16" />
            <Icon icon="lucide:chevron-left" width="14" class="opacity-50 transition-opacity group-hover:opacity-90" />
          </button>
        </Transition>
        <!-- 展开态：半透明毛玻璃悬浮操作栏 -->
        <Transition
          enter-active-class="transition duration-200 ease-out"
          enter-from-class="translate-x-4 opacity-0"
          leave-active-class="transition duration-150 ease-in"
          leave-to-class="translate-x-4 opacity-0"
        >
          <div
            v-if="panelOpen"
            class="pointer-events-auto mr-4 mt-2 flex flex-wrap items-center gap-2 rounded-2xl border border-border/60 bg-background/80 p-1.5 pl-2 shadow-lg shadow-black/5 backdrop-blur-xl sm:mr-5"
          >
            <span
              class="flex h-7 w-4 shrink-0 cursor-grab touch-none items-center justify-center rounded text-muted-foreground/50 transition-colors hover:text-foreground active:cursor-grabbing"
              :title="t('workbench.widgets.drag_toolbar')"
              @pointerdown="onToolbarDragStart"
            >
              <Icon icon="lucide:grip-vertical" width="14" />
            </span>
            <div class="flex items-center gap-2">
              <span class="text-sm font-semibold text-foreground">{{ t('menu.workbench_dashboard') }}</span>
              <SyncStatusBadge :synced="appStore.widgetsSyncEnabled" />
            </div>
            <div class="mx-0.5 h-5 w-px bg-border/70" />
            <template v-if="customizing">
              <NButton size="small" @click="showAdd = true">
                <template #icon>
                  <Icon icon="lucide:plus" />
                </template>
                {{ t('workbench.widgets.add') }}
              </NButton>
              <NButton size="small" @click="resetBoard">
                <template #icon>
                  <Icon icon="lucide:rotate-ccw" />
                </template>
                {{ t('workbench.widgets.reset') }}
              </NButton>
              <NButton size="small" type="primary" @click="finishCustomize">
                <template #icon>
                  <Icon icon="lucide:check" />
                </template>
                {{ t('workbench.widgets.done') }}
              </NButton>
            </template>
            <template v-else>
              <NButton size="small" secondary @click="customizing = true">
                <template #icon>
                  <Icon icon="lucide:layout-grid" />
                </template>
                {{ t('workbench.widgets.customize') }}
              </NButton>
              <button
                type="button"
                :title="t('workbench.widgets.collapse')"
                class="flex h-7 w-7 items-center justify-center rounded-lg text-muted-foreground transition-colors hover:bg-muted hover:text-foreground"
                @click="panelOpen = false"
              >
                <Icon icon="lucide:chevron-right" width="16" />
              </button>
            </template>
          </div>
        </Transition>
      </div>
    </div>

    <div v-if="!board.length" class="py-20">
      <NEmpty :description="t('workbench.widgets.empty')">
        <template #extra>
          <NButton size="small" @click="customizing = true; showAdd = true">
            {{ t('workbench.widgets.add') }}
          </NButton>
        </template>
      </NEmpty>
    </div>

    <DragDropProvider v-else @drag-end="onDragEnd">
      <div ref="gridRef" class="grid grid-cols-1 gap-4 md:grid-cols-12">
        <SortableItem
          v-for="(item, index) in board"
          :id="item.key"
          :key="item.key"
          :index="index"
          handle=".widget-drag-handle"
          :disabled="!customizing"
          class="widget-cell min-w-0"
          :style="{ '--widget-span': item.span }"
        >
          <div class="relative h-full" :class="customizing ? 'rounded-xl ring-1 ring-dashed ring-[hsl(var(--primary)/0.45)]' : ''">
            <component :is="WIDGET_MAP[item.key]?.component" />
            <!-- 边缘拖拽调宽：左右各一把手，拖动按 12 栅格吸附到最近档位（Windows 窗口式） -->
            <template v-if="customizing">
              <div
                class="group/lh absolute left-0 top-0 z-10 flex h-full w-2.5 cursor-ew-resize touch-none items-center justify-start"
                :title="t('workbench.widgets.resize')"
                @pointerdown="onResizeStart($event, item, 'left')"
              >
                <span class="h-10 w-1 rounded-full bg-border transition-colors group-hover/lh:bg-[hsl(var(--primary))]" />
              </div>
              <div
                class="group/rh absolute right-0 top-0 z-10 flex h-full w-2.5 cursor-ew-resize touch-none items-center justify-end"
                :title="t('workbench.widgets.resize')"
                @pointerdown="onResizeStart($event, item, 'right')"
              >
                <span class="h-10 w-1 rounded-full bg-border transition-colors group-hover/rh:bg-[hsl(var(--primary))]" />
              </div>
            </template>
            <!-- 自定义工具条：始终在 DOM（v-show），仅手柄 span 可拖（非交互元素 + handle 选择器） -->
            <div
              v-show="customizing"
              class="absolute right-2 top-2 z-20 flex items-center gap-0.5 rounded-lg border border-border bg-card/95 px-1 py-0.5 shadow-sm backdrop-blur"
            >
              <span
                class="widget-drag-handle flex h-6 w-6 cursor-grab touch-none select-none items-center justify-center rounded text-muted-foreground hover:bg-muted active:cursor-grabbing"
                :title="t('workbench.widgets.drag')"
              >
                <Icon icon="lucide:grip-vertical" width="15" />
              </span>
              <NPopover trigger="click" placement="bottom-end" :show-arrow="false">
                <template #trigger>
                  <button type="button" class="flex h-6 min-w-[2.25rem] items-center justify-center rounded px-1 text-xs font-medium text-muted-foreground hover:bg-muted" :title="t('workbench.widgets.span')">
                    {{ spanLabel(item.span) }}
                  </button>
                </template>
                <div class="grid grid-cols-6 gap-1">
                  <button
                    v-for="s in SIZE_OPTIONS"
                    :key="s"
                    type="button"
                    class="flex h-7 min-w-[2.75rem] items-center justify-center rounded px-1 text-xs transition-colors"
                    :class="s === item.span ? 'bg-[hsl(var(--primary))] text-primary-foreground' : 'text-muted-foreground hover:bg-muted'"
                    @click="setSpan(item, s)"
                  >
                    {{ spanLabel(s) }}
                  </button>
                </div>
              </NPopover>
              <button type="button" class="flex h-6 w-6 items-center justify-center rounded text-muted-foreground hover:bg-muted hover:text-[hsl(var(--destructive))]" @click="removeWidget(item.key)">
                <Icon icon="lucide:x" width="15" />
              </button>
            </div>
          </div>
        </SortableItem>
      </div>
    </DragDropProvider>

    <NDrawer v-model:show="showAdd" :width="340" placement="right">
      <NDrawerContent :title="t('workbench.widgets.add_panel_title')" closable>
        <div v-if="!available.length" class="py-10">
          <NEmpty :description="t('workbench.widgets.all_added')" />
        </div>
        <div v-else class="flex flex-col gap-2">
          <div v-for="widget in available" :key="widget.key" class="flex items-center gap-3 rounded-lg border border-border bg-background p-3">
            <div class="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-muted text-[hsl(var(--primary))]">
              <Icon :icon="widget.icon" width="18" />
            </div>
            <div class="min-w-0 flex-1">
              <div class="text-sm font-medium text-foreground">
                {{ t(widget.titleKey) }}
              </div>
              <div class="truncate text-xs text-muted-foreground">
                {{ t(widget.descKey) }}
              </div>
            </div>
            <NButton size="small" type="primary" secondary @click="addWidget(widget.key)">
              <template #icon>
                <Icon icon="lucide:plus" />
              </template>
            </NButton>
          </div>
        </div>
      </NDrawerContent>
    </NDrawer>
  </div>
</template>

<style scoped>
/* 小屏（<md）忽略用户设定的小组件宽度，一律整行铺满；md 及以上按 12 栅格自定义宽度 */
.widget-cell {
  grid-column: 1 / -1;
}

@media (min-width: 768px) {
  .widget-cell {
    grid-column: span var(--widget-span);
  }
}
</style>
