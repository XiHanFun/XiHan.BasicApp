<script lang="ts" setup>
import type { DragEndEvent } from '@dnd-kit/vue'
import { DragDropProvider } from '@dnd-kit/vue'
import { computed, nextTick, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import { resolveSortMove } from '~/components/common/sortable'
import SortableItem from '~/components/common/SortableItem.vue'
import { ensurePinyin, getPinyinIndex, usePinyinReady } from '~/composables/usePinyin'
import { Icon } from '~/iconify'
import { useLayoutBridgeStore, useSplitViewStore, useTabbarStore } from '~/stores'

defineOptions({ name: 'AppTabOverview' })

const route = useRoute()
const router = useRouter()
const { t, te } = useI18n()
const tabbarStore = useTabbarStore()
const splitViewStore = useSplitViewStore()
const layoutBridgeStore = useLayoutBridgeStore()
const pinyinReady = usePinyinReady()

const visible = ref(false)
const keyword = ref('')
const activeIndex = ref(0)
const inputRef = ref<HTMLInputElement | null>(null)
const gridRef = ref<HTMLElement | null>(null)

function tr(title: string): string {
  return te(title) ? t(title) : title
}

function resolveIcon(icon?: string): string {
  if (!icon) {
    return 'lucide:file'
  }
  return icon.includes(':') ? icon : `lucide:${icon}`
}

interface OverviewCard {
  key: string
  path: string
  title: string
  icon: string
  pinned: boolean
  closable: boolean
  active: boolean
  /** 分屏锚定标签的副标题（合并展示） */
  splitWith?: string
}

/** 卡片源：隐藏被合并的分屏副标签；锚定标签合并展示双标题 */
const cards = computed<OverviewCard[]>(() =>
  tabbarStore.tabs
    .filter(tab => !splitViewStore.isMergedTab(tab.path))
    .map((tab) => {
      let splitWith: string | undefined
      if (splitViewStore.isSplitTab(tab.path)) {
        const right = tabbarStore.tabs.find(item => item.path === splitViewStore.rightPath)
        splitWith = right ? tr(right.title) : undefined
      }
      return {
        key: tab.path,
        path: tab.path,
        title: tr(tab.title),
        icon: resolveIcon(tab.meta?.icon as string | undefined),
        pinned: Boolean(tab.pinned),
        closable: tab.closable,
        active: route.fullPath === tab.path,
        splitWith,
      }
    }),
)

/** 搜索过滤：标题 / 路径 / 拼音全拼 / 首字母 */
const filteredCards = computed<OverviewCard[]>(() => {
  void pinyinReady.value
  const q = keyword.value.trim().toLowerCase()
  if (!q) {
    return cards.value
  }
  return cards.value.filter((card) => {
    if (card.title.toLowerCase().includes(q) || card.path.toLowerCase().includes(q)) {
      return true
    }
    const idx = getPinyinIndex(card.title)
    return !!idx && (idx.full.includes(q) || idx.initials.includes(q))
  })
})

watch(keyword, () => {
  activeIndex.value = 0
})

// ── 打开 / 关闭 ──────────────────────────────────────────────────
function open(): void {
  visible.value = true
  keyword.value = ''
  // 默认选中当前激活标签
  activeIndex.value = Math.max(0, cards.value.findIndex(card => card.active))
  void ensurePinyin()
  void nextTick(() => inputRef.value?.focus())
}

function close(): void {
  visible.value = false
}

watch(
  () => layoutBridgeStore.tabOverviewVersion,
  () => {
    if (visible.value) {
      close()
    }
    else {
      open()
    }
  },
)

// ── 交互 ─────────────────────────────────────────────────────────
function jumpTo(card: OverviewCard): void {
  close()
  tabbarStore.setActiveTab(card.path)
  if (route.fullPath !== card.path) {
    void router.push(card.path)
  }
}

function closeTab(card: OverviewCard, e?: Event): void {
  e?.stopPropagation()
  if (!card.closable) {
    return
  }
  if (splitViewStore.isSplitTab(card.path)) {
    splitViewStore.close()
  }
  tabbarStore.removeTab(card.path)
  if (route.fullPath === card.path) {
    void router.push(tabbarStore.activeTab)
  }
  activeIndex.value = Math.min(activeIndex.value, Math.max(0, filteredCards.value.length - 1))
}

/** 拖拽排序（仅未过滤时启用，避免子集排序歧义）；固定/非固定不互换 */
const dragEnabled = computed(() => keyword.value.trim() === '')
function onDragEnd(event: DragEndEvent): void {
  const list = filteredCards.value
  const move = resolveSortMove(event, list.map(card => card.path))
  if (!move) {
    return
  }
  if (list[move.from]?.pinned !== list[move.to]?.pinned) {
    return
  }
  const from = list[move.from]
  const to = list[move.to]
  if (from && to && from.path !== to.path) {
    tabbarStore.moveTab(from.path, to.path)
  }
}

// ── 键盘导航：↑↓ 按列步进、←→ 线性、Enter 跳转、Esc 关闭 ──────────
function columnsCount(): number {
  const grid = gridRef.value
  if (!grid) {
    return 1
  }
  const items = [...grid.querySelectorAll<HTMLElement>('[data-overview-card]')]
  if (items.length <= 1) {
    return 1
  }
  const firstTop = items[0]!.offsetTop
  let count = 0
  for (const item of items) {
    if (item.offsetTop !== firstTop) {
      break
    }
    count++
  }
  return Math.max(1, count)
}

function moveActive(delta: number): void {
  const max = filteredCards.value.length - 1
  if (max < 0) {
    return
  }
  activeIndex.value = Math.min(max, Math.max(0, activeIndex.value + delta))
  void nextTick(() => {
    gridRef.value
      ?.querySelectorAll<HTMLElement>('[data-overview-card]')[activeIndex.value]
      ?.scrollIntoView({ block: 'nearest' })
  })
}

function onKeydown(e: KeyboardEvent): void {
  if (e.key === 'Escape') {
    e.preventDefault()
    close()
    return
  }
  if (e.key === 'Enter') {
    e.preventDefault()
    const card = filteredCards.value[activeIndex.value]
    if (card) {
      jumpTo(card)
    }
    return
  }
  if (e.key === 'ArrowDown') {
    e.preventDefault()
    moveActive(columnsCount())
  }
  else if (e.key === 'ArrowUp') {
    e.preventDefault()
    moveActive(-columnsCount())
  }
  else if (e.key === 'ArrowRight' && !keyword.value) {
    e.preventDefault()
    moveActive(1)
  }
  else if (e.key === 'ArrowLeft' && !keyword.value) {
    e.preventDefault()
    moveActive(-1)
  }
}
</script>

<template>
  <Teleport to="body">
    <Transition name="tab-ov">
      <div v-if="visible" class="tab-ov" @click.self="close" @keydown="onKeydown">
        <div class="tab-ov__panel">
          <!-- 头部：标题 + 搜索 + 关闭 -->
          <div class="tab-ov__head">
            <span class="tab-ov__title">
              {{ t('tabbar.overview') }}
              <span class="tab-ov__count">{{ filteredCards.length }}</span>
            </span>
            <div class="tab-ov__search">
              <Icon icon="lucide:search" width="14" height="14" class="opacity-50" />
              <input
                ref="inputRef"
                v-model="keyword"
                class="tab-ov__input"
                :placeholder="t('tabbar.overview_search')"
                spellcheck="false"
                @keydown="onKeydown"
              >
            </div>
            <button type="button" class="tab-ov__close-btn" :aria-label="t('tabbar.close')" @click="close">
              <Icon icon="lucide:x" width="17" height="17" />
            </button>
          </div>

          <!-- 卡片网格 -->
          <DragDropProvider @drag-end="onDragEnd">
            <div ref="gridRef" class="tab-ov__grid">
              <SortableItem
                v-for="(card, index) in filteredCards"
                :id="card.path"
                :key="card.path"
                :index="index"
                :disabled="!dragEnabled"
                data-overview-card="true"
                class="tab-ov-card"
                :class="{ 'is-active': card.active, 'is-focused': index === activeIndex }"
                :style="{ animationDelay: `${Math.min(index * 18, 240)}ms` }"
                role="button"
                tabindex="0"
                @click="jumpTo(card)"
                @mousemove="activeIndex = index"
                @keydown.enter.prevent="jumpTo(card)"
              >
                <span class="tab-ov-card__icon">
                  <Icon :icon="card.splitWith ? 'lucide:columns-2' : card.icon" width="22" height="22" />
                </span>
                <span class="tab-ov-card__title" :title="card.splitWith ? `${card.title} | ${card.splitWith}` : card.title">
                  {{ card.title }}<template v-if="card.splitWith"> | {{ card.splitWith }}</template>
                </span>
                <span class="tab-ov-card__path">{{ card.path }}</span>
                <span v-if="card.pinned" class="tab-ov-card__pin">
                  <Icon icon="lucide:pin" width="11" height="11" />
                </span>
                <button
                  v-if="card.closable"
                  type="button"
                  class="tab-ov-card__close"
                  :aria-label="t('tabbar.close')"
                  @click="(e) => closeTab(card, e)"
                >
                  <Icon icon="lucide:x" width="13" height="13" />
                </button>
              </SortableItem>

              <div v-if="!filteredCards.length" class="tab-ov__empty">
                {{ t('tabbar.overview_empty') }}
              </div>
            </div>
          </DragDropProvider>

          <!-- 底部提示 -->
          <div class="tab-ov__footer">
            <span><kbd>↑↓←→</kbd> {{ t('header.search.footer.select') }}</span>
            <span><kbd>↵</kbd> {{ t('header.search.footer.open') }}</span>
            <span><kbd>esc</kbd> {{ t('header.search.footer.close') }}</span>
          </div>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<style scoped>
.tab-ov {
  position: fixed;
  inset: 0;
  z-index: 2450;
  display: flex;
  justify-content: center;
  align-items: flex-start;
  padding: 8vh 24px 24px;
  /* 自包含暗色遮罩：--overlay 变量自带 alpha，叠加二次 alpha 会成为非法值导致背景失效 */
  background: rgb(0 0 0 / 50%);
  backdrop-filter: blur(8px);
  overflow-y: auto;
}

.tab-ov__panel {
  width: min(96vw, 1080px);
  display: flex;
  flex-direction: column;
  gap: 18px;
}

.tab-ov__head {
  display: flex;
  align-items: center;
  gap: 14px;
}

.tab-ov__title {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  font-size: 17px;
  font-weight: 600;
  color: #fff;
  text-shadow: 0 1px 8px rgb(0 0 0 / 40%);
  white-space: nowrap;
}

.tab-ov__count {
  min-width: 22px;
  padding: 1px 7px;
  border-radius: 9999px;
  background: rgb(255 255 255 / 18%);
  font-size: 12px;
  font-weight: 600;
  text-align: center;
}

.tab-ov__search {
  display: flex;
  align-items: center;
  gap: 8px;
  flex: 1;
  max-width: 360px;
  height: 36px;
  padding: 0 12px;
  border-radius: 9999px;
  background: hsl(var(--background) / 0.92);
  border: 1px solid hsl(var(--border));
}

.tab-ov__input {
  flex: 1;
  min-width: 0;
  height: 100%;
  border: none;
  outline: none;
  background: transparent;
  color: hsl(var(--foreground));
  font-size: 14px;
}

.tab-ov__close-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 34px;
  height: 34px;
  margin-left: auto;
  border: none;
  border-radius: 9999px;
  background: rgb(255 255 255 / 14%);
  color: #fff;
  cursor: pointer;
  transition: background 0.15s ease;
}

.tab-ov__close-btn:hover {
  background: rgb(255 255 255 / 26%);
}

.tab-ov__grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(180px, 1fr));
  gap: 14px;
}

/* 卡片 */
.tab-ov-card {
  position: relative;
  display: flex;
  flex-direction: column;
  gap: 6px;
  padding: 16px 14px 12px;
  border-radius: 14px;
  background: hsl(var(--card));
  border: 1.5px solid hsl(var(--border));
  cursor: pointer;
  user-select: none;
  transition:
    transform 0.16s ease,
    border-color 0.16s ease,
    box-shadow 0.16s ease;
  animation: tab-ov-card-in 0.32s cubic-bezier(0.22, 1, 0.36, 1) both;
}

.tab-ov-card:hover,
.tab-ov-card.is-focused {
  transform: translateY(-3px);
  border-color: hsl(var(--primary) / 55%);
  box-shadow: 0 10px 28px hsl(var(--foreground) / 14%);
}

.tab-ov-card.is-active {
  border-color: hsl(var(--primary));
  box-shadow: 0 0 0 2px hsl(var(--primary) / 25%);
}

.tab-ov-card__icon {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 42px;
  height: 42px;
  border-radius: 11px;
  background: hsl(var(--primary) / 0.1);
  color: hsl(var(--primary));
}

.tab-ov-card__title {
  font-size: 14px;
  font-weight: 600;
  color: hsl(var(--foreground));
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.tab-ov-card__path {
  font-size: 11px;
  color: hsl(var(--muted-foreground));
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.tab-ov-card__pin {
  position: absolute;
  top: 10px;
  right: 12px;
  color: hsl(var(--muted-foreground));
}

.tab-ov-card__close {
  position: absolute;
  top: 8px;
  right: 8px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 22px;
  height: 22px;
  border: none;
  border-radius: 9999px;
  background: transparent;
  color: hsl(var(--muted-foreground));
  cursor: pointer;
  opacity: 0;
  transition:
    opacity 0.15s ease,
    background 0.15s ease,
    color 0.15s ease;
}

.tab-ov-card:hover .tab-ov-card__close,
.tab-ov-card.is-focused .tab-ov-card__close {
  opacity: 1;
}

.tab-ov-card__close:hover {
  background: hsl(var(--destructive) / 12%);
  color: hsl(var(--destructive));
}

/* 拖拽中 */
.tab-ov-card[data-dragging] {
  z-index: 2;
  opacity: 0.6;
  cursor: grabbing;
}

.tab-ov__empty {
  grid-column: 1 / -1;
  padding: 48px 0;
  text-align: center;
  font-size: 13px;
  color: rgb(255 255 255 / 70%);
}

.tab-ov__footer {
  display: flex;
  gap: 18px;
  justify-content: center;
  font-size: 12px;
  color: rgb(255 255 255 / 65%);
}

.tab-ov__footer kbd {
  padding: 1px 5px;
  margin-right: 3px;
  border-radius: 4px;
  background: rgb(255 255 255 / 16%);
  font-family: ui-monospace, monospace;
  font-size: 11px;
}

/* 进出场 */
.tab-ov-enter-active {
  transition: opacity 0.2s ease;
}

.tab-ov-leave-active {
  transition: opacity 0.16s ease;
}

.tab-ov-enter-from,
.tab-ov-leave-to {
  opacity: 0;
}

@keyframes tab-ov-card-in {
  from {
    opacity: 0;
    transform: translateY(14px) scale(0.94);
  }
}
</style>
