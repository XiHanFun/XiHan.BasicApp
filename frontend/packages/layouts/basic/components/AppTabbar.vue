<script lang="ts" setup>
import type { DragEndEvent } from '@dnd-kit/vue'
import type { DropdownOption } from 'naive-ui'
import type { TabItem } from '~/types'
import { DragDropProvider } from '@dnd-kit/vue'
import { useDebounceFn } from '@vueuse/core'
import { NButton, NDropdown, NIcon } from 'naive-ui'
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import { resolveSortMove } from '~/components/common/sortable'
import { useContentMaximize, useRefresh } from '~/hooks'
import { Icon } from '~/iconify'
import { useAppStore, useFavoritesStore, useTabbarPreferences, useTabbarStore } from '~/stores'
import {
  buildTabContextOptions,
  createDropdownIcon,
  flyToFavorites,
  getTabByPath,
  openTabInNewWindow,
} from '../composables'
import TabbarContextMenu from './tabbar/TabbarContextMenu.vue'
import TabbarTabItem from './tabbar/TabbarTabItem.vue'

defineOptions({ name: 'AppTabbar' })

const route = useRoute()
const router = useRouter()
const { t, te } = useI18n()
const appStore = useAppStore()
const tabbarPreferences = useTabbarPreferences()
const tabbarStore = useTabbarStore()
const favoritesStore = useFavoritesStore()

const visibleTabs = computed(() => tabbarStore.tabs)
const localizedTabs = computed(() => {
  return visibleTabs.value.map((tab) => {
    const translated = te(tab.title) ? t(tab.title) : tab.title
    return {
      ...tab,
      displayTitle: translated,
    }
  })
})
const { contentIsMaximize: isContentMaximized, toggleMaximize } = useContentMaximize()
const { refresh: refreshCurrentTab } = useRefresh()
const contextMenuVisible = ref(false)
const contextMenuX = ref(0)
const contextMenuY = ref(0)
const contextTabPath = ref('')
const contextTabClosable = ref(false)
const contextTabPinned = ref(false)
// 收藏「飞入」动画的起点矩形（右键时捕获被点中标签的 DOM 位置）
const contextTabRect = ref<DOMRect | null>(null)
const tabsContainerRef = ref<HTMLElement | null>(null)
const scrollViewportRef = ref<HTMLElement | null>(null)

// ---- 溢出滚动按钮 ----
const showScrollBtn = ref(false)
const scrollAtLeft = ref(true)
const scrollAtRight = ref(false)
let resizeObserver: null | ResizeObserver = null
let mutationObserver: null | MutationObserver = null

const tabThemeVars = computed(() => {
  const color = appStore.themeColor
  return {
    '--tab-active-color': color,
    '--tab-active-bg': `color-mix(in srgb, ${color} 18%, hsl(var(--background)))`,
  }
})
const moreTabOptions = computed<DropdownOption[]>(() =>
  localizedTabs.value.map(item => ({
    key: item.path,
    label: item.displayTitle,
    icon: createDropdownIcon(item.pinned ? 'lucide:pin' : 'lucide:file'),
  })),
)
const contextMenuOptions = computed(() => {
  return buildTabContextOptions({
    path: contextTabPath.value,
    closable: contextTabClosable.value,
    pinned: contextTabPinned.value,
    favorited: favoritesStore.has(contextTabPath.value),
    favoritesEnabled: appStore.widgetFavorites,
    tabs: visibleTabs.value,
    isContentMaximized: isContentMaximized.value,
    t,
  })
})

function handleJump(path: string) {
  tabbarStore.setActiveTab(path)
  if (route.fullPath !== path) {
    router.push(path)
  }
}

function handleClose(path: string, e: MouseEvent) {
  e.stopPropagation()
  tabbarStore.removeTab(path)
  if (route.fullPath === path) {
    router.push(tabbarStore.activeTab)
  }
}

function handleCloseOthers(path: string) {
  tabbarStore.closeOthers(path)
  if (route.fullPath !== path) {
    router.push(path)
  }
}

function handleContextMenuSelect(key: string, tabPath: string) {
  switch (key) {
    case 'close':
      tabbarStore.removeTab(tabPath)
      if (route.fullPath === tabPath) {
        router.push(tabbarStore.activeTab)
      }
      break
    case 'closeLeft':
      tabbarStore.closeLeft(tabPath)
      break
    case 'closeRight':
      tabbarStore.closeRight(tabPath)
      break
    case 'closeOthers':
      handleCloseOthers(tabPath)
      break
    case 'closeAll':
      tabbarStore.closeAll()
      router.push(tabbarStore.activeTab)
      break
    case 'pin':
      tabbarStore.togglePin(tabPath)
      break
    case 'maximize':
      toggleMaximize()
      break
    case 'reload':
      tabbarStore.refreshTab(tabPath)
      if (route.fullPath !== tabPath) {
        router.push(tabPath)
      }
      break
    case 'open':
      openTabInNewWindow(tabPath)
      break
    case 'favorite': {
      const target = getTabByPath(visibleTabs.value, tabPath)
      if (!target) {
        break
      }
      const wasFavorited = favoritesStore.has(tabPath)
      favoritesStore.toggle({
        key: tabPath,
        path: tabPath,
        title: target.title,
        icon: target.meta?.icon as string | undefined,
      })
      // 仅「新增收藏」时播放飞入动画（取消收藏不播）
      if (!wasFavorited) {
        flyToFavorites(contextTabRect.value, {
          label: te(target.title) ? t(target.title) : target.title,
        })
      }
      break
    }
  }
}

function openContextMenu(e: MouseEvent, tab: TabItem) {
  e.preventDefault()
  contextTabPath.value = tab.path
  contextTabClosable.value = tab.closable
  contextTabPinned.value = Boolean(tab.pinned)
  // 捕获被右键的标签 DOM 矩形，作为「飞入收藏夹」动画起点
  const tabEl = (e.target as HTMLElement | null)?.closest('[data-tab-item]') as HTMLElement | null
  contextTabRect.value = tabEl?.getBoundingClientRect() ?? null
  contextMenuX.value = e.clientX
  contextMenuY.value = e.clientY
  contextMenuVisible.value = true
}

function handleDropdownSelect(key: string) {
  if (!contextTabPath.value) {
    return
  }
  handleContextMenuSelect(key, contextTabPath.value)
  contextMenuVisible.value = false
}

function handleMiddleClose(path: string) {
  const target = getTabByPath(visibleTabs.value, path)
  if (!target || !target.closable) {
    return
  }
  tabbarStore.removeTab(path)
  if (route.fullPath === path) {
    router.push(tabbarStore.activeTab)
  }
}

// ---- 标签入场 / 离场动画（JS hooks + 内联 transition，绕开 scoped CSS 跨组件边界问题） ----
// 流程：@before-enter 设初始态 → @enter 强制 reflow + 设 transition 动到终态
const TAB_ENTER_DURATION = 320
const TAB_LEAVE_DURATION = 260
const TAB_EASING = 'cubic-bezier(0.22, 1, 0.36, 1)'

function setTabStyle(el: HTMLElement, styles: Partial<CSSStyleDeclaration>) {
  Object.assign(el.style, styles)
}

function clearTabTransition(el: HTMLElement) {
  setTabStyle(el, { transition: '', opacity: '', transform: '' })
}

function onTabBeforeEnter(el: Element) {
  setTabStyle(el as HTMLElement, { opacity: '0', transform: 'translateX(-18px)' })
}

function onTabEnter(el: Element, done: () => void) {
  const htmlEl = el as HTMLElement
  void htmlEl.offsetHeight
  setTabStyle(htmlEl, {
    transition: `opacity ${TAB_ENTER_DURATION}ms ${TAB_EASING}, transform ${TAB_ENTER_DURATION}ms ${TAB_EASING}`,
    opacity: '1',
    transform: 'translateX(0px)',
  })
  const cleanup = () => {
    clearTabTransition(htmlEl)
    done()
  }
  const timer = setTimeout(cleanup, TAB_ENTER_DURATION + 40)
  htmlEl.addEventListener('transitionend', () => {
    clearTimeout(timer)
    cleanup()
  }, { once: true })
}

function onTabBeforeLeave(el: Element) {
  setTabStyle(el as HTMLElement, { opacity: '1', transform: 'translateX(0px)' })
}

function onTabLeave(el: Element, done: () => void) {
  const htmlEl = el as HTMLElement
  void htmlEl.offsetHeight
  setTabStyle(htmlEl, {
    transition: `opacity ${TAB_LEAVE_DURATION}ms ${TAB_EASING}, transform ${TAB_LEAVE_DURATION}ms ${TAB_EASING}`,
    opacity: '0',
    transform: 'translateX(-18px)',
  })
  const timer = setTimeout(done, TAB_LEAVE_DURATION + 40)
  htmlEl.addEventListener('transitionend', () => {
    clearTimeout(timer)
    done()
  }, { once: true })
}

function onTabEnterCancelled(el: Element) {
  clearTabTransition(el as HTMLElement)
}

function onTabLeaveCancelled(el: Element) {
  clearTabTransition(el as HTMLElement)
}

// 拖拽结束：按路径提交新顺序。固定标签（pinned）在 TabbarTabItem 中已被禁用拖拽，
// 此处再以数据校验「固定 / 非固定不互换」，保证最终顺序不破坏约束。
function onTabDragEnd(event: DragEndEvent) {
  const tabs = localizedTabs.value
  const move = resolveSortMove(event, tabs.map(tab => tab.path))
  if (!move) {
    return
  }
  if (Boolean(tabs[move.from]?.pinned) !== Boolean(tabs[move.to]?.pinned)) {
    return
  }
  const from = tabs[move.from]
  const to = tabs[move.to]
  if (from && to && from.path !== to.path) {
    tabbarStore.moveTab(from.path, to.path)
  }
}

function handleMoreTabSelect(path: string) {
  handleJump(path)
}

// ---- 滚动按钮逻辑 ----

function calcShowScrollBtn() {
  const vp = scrollViewportRef.value
  if (!vp)
    return
  showScrollBtn.value = vp.scrollWidth > vp.clientWidth
}

function updateScrollEdge() {
  const vp = scrollViewportRef.value
  if (!vp)
    return
  scrollAtLeft.value = vp.scrollLeft <= 0
  scrollAtRight.value = vp.scrollLeft + vp.clientWidth >= vp.scrollWidth - 1
}

const debouncedCalc = useDebounceFn(() => {
  calcShowScrollBtn()
  scrollToActive()
}, 80)

const debouncedEdge = useDebounceFn(updateScrollEdge, 80)

function scrollDirection(dir: 'left' | 'right', distance = 150) {
  const vp = scrollViewportRef.value
  if (!vp)
    return
  vp.scrollBy({
    behavior: 'smooth',
    left: dir === 'left' ? -(vp.clientWidth - distance) : +(vp.clientWidth - distance),
  })
}

async function scrollToActive() {
  const vp = scrollViewportRef.value
  if (!vp)
    return
  await nextTick()
  if (vp.clientWidth >= vp.scrollWidth)
    return
  requestAnimationFrame(() => {
    const activeEl = vp.querySelector('.is-active') as HTMLElement | null
    activeEl?.scrollIntoView({ behavior: 'smooth', inline: 'nearest' })
  })
}

function handleTabsWheel(e: WheelEvent) {
  const vp = scrollViewportRef.value
  if (!vp)
    return
  if (appStore.tabbarScrollResponse) {
    e.preventDefault()
    vp.scrollBy({ left: e.deltaY !== 0 ? e.deltaY * 3 : e.deltaX * 3 })
  }
}

function initScrollObservers() {
  const vp = scrollViewportRef.value
  if (!vp)
    return

  calcShowScrollBtn()
  updateScrollEdge()

  resizeObserver?.disconnect()
  resizeObserver = new ResizeObserver(debouncedCalc)
  resizeObserver.observe(vp)

  let prevCount = vp.querySelectorAll('[data-tab-item]').length
  mutationObserver?.disconnect()
  mutationObserver = new MutationObserver(() => {
    const count = vp.querySelectorAll('[data-tab-item]').length
    if (count > prevCount)
      scrollToActive()
    if (count !== prevCount)
      calcShowScrollBtn()
    prevCount = count
  })
  mutationObserver.observe(vp, { childList: true, subtree: true })
}

onMounted(async () => {
  await nextTick()
  initScrollObservers()
  scrollViewportRef.value?.addEventListener('scroll', debouncedEdge, { passive: true })
  scrollViewportRef.value?.addEventListener('wheel', handleTabsWheel, { passive: false })
})

onBeforeUnmount(() => {
  resizeObserver?.disconnect()
  mutationObserver?.disconnect()
  scrollViewportRef.value?.removeEventListener('scroll', debouncedEdge)
  scrollViewportRef.value?.removeEventListener('wheel', handleTabsWheel)
})

watch(() => appStore.tabbarStyle, () => {
  nextTick(() => {
    initScrollObservers()
  })
})

watch(() => route.fullPath, () => {
  nextTick(scrollToActive)
})
</script>

<template>
  <div
    v-if="appStore.tabbarEnabled"
    :style="tabThemeVars"
    class="tabbar-root flex items-center bg-[var(--tabbar-bg)] px-2"
    :class="appStore.tabbarStyle === 'chrome' ? 'h-10 pt-[4px] pb-0' : 'h-[38px] py-0'"
  >
    <!-- 左侧滚动箭头 -->
    <NButton
      v-show="showScrollBtn"
      quaternary
      size="tiny"
      :focusable="false"
      :disabled="scrollAtLeft"
      @click="scrollDirection('left')"
    >
      <template #icon>
        <NIcon>
          <Icon icon="lucide:chevrons-left" width="14" />
        </NIcon>
      </template>
    </NButton>
    <span v-show="showScrollBtn" class="tab-divider" />

    <!-- 标签列表（无滚动条，通过箭头控制） -->
    <div
      class="tabbar-list min-w-0 flex-1 overflow-hidden"
      :class="appStore.tabbarStyle !== 'chrome' ? 'h-9' : 'h-full'"
    >
      <div
        ref="scrollViewportRef"
        class="tabbar-viewport h-full overflow-x-auto"
      >
        <div
          ref="tabsContainerRef"
          class="pr-2"
          :class="appStore.tabbarStyle === 'chrome'
            ? 'flex h-full min-w-max items-end'
            : 'flex h-full min-w-max items-stretch'"
        >
          <DragDropProvider @drag-end="onTabDragEnd">
            <TransitionGroup
              name="tabs-slide"
              :css="false"
              @before-enter="onTabBeforeEnter"
              @enter="onTabEnter"
              @before-leave="onTabBeforeLeave"
              @leave="onTabLeave"
              @enter-cancelled="onTabEnterCancelled"
              @leave-cancelled="onTabLeaveCancelled"
            >
              <TabbarTabItem
                v-for="(item, index) in localizedTabs"
                :key="item.key"
                :item="item"
                :index="index"
                :active="route.fullPath === item.path"
                :is-last="index === localizedTabs.length - 1"
                :draggable="tabbarPreferences.tabbarDraggable.value && !item.pinned"
                :show-icon="appStore.tabbarShowIcon"
                :middle-close-enabled="appStore.tabbarMiddleClickClose"
                :style-type="appStore.tabbarStyle"
                data-tab-item="true"
                @jump="handleJump"
                @contextmenu="openContextMenu"
                @close="handleClose"
                @toggle-pin="tabbarStore.togglePin"
                @middle-close="handleMiddleClose"
              />
            </TransitionGroup>
          </DragDropProvider>
        </div>
      </div>
    </div>

    <!-- 右侧滚动箭头 -->
    <span v-show="showScrollBtn" class="tab-divider" />
    <NButton
      v-show="showScrollBtn"
      quaternary
      size="tiny"
      :focusable="false"
      :disabled="scrollAtRight"
      @click="scrollDirection('right')"
    >
      <template #icon>
        <NIcon>
          <Icon icon="lucide:chevrons-right" width="14" />
        </NIcon>
      </template>
    </NButton>

    <TabbarContextMenu
      :show="contextMenuVisible"
      :x="contextMenuX"
      :y="contextMenuY"
      :options="contextMenuOptions"
      @close="contextMenuVisible = false"
      @select="handleDropdownSelect"
    />
    <span class="tab-divider" />
    <NDropdown
      v-if="tabbarPreferences.tabbarShowMore.value"
      :options="moreTabOptions"
      @select="(key) => handleMoreTabSelect(String(key))"
    >
      <NButton quaternary size="tiny">
        <template #icon>
          <NIcon>
            <Icon icon="lucide:layout-grid" width="14" />
          </NIcon>
        </template>
      </NButton>
    </NDropdown>
    <span
      v-if="tabbarPreferences.tabbarShowMore.value && (appStore.widgetRefresh || tabbarPreferences.tabbarShowMaximize.value)"
      class="tab-divider"
    />
    <NButton
      v-if="appStore.widgetRefresh"
      quaternary
      size="tiny"
      @click="refreshCurrentTab"
    >
      <template #icon>
        <NIcon>
          <Icon icon="lucide:rotate-cw" width="14" />
        </NIcon>
      </template>
    </NButton>
    <span v-if="appStore.widgetRefresh && tabbarPreferences.tabbarShowMaximize.value" class="tab-divider" />
    <NButton
      v-if="tabbarPreferences.tabbarShowMaximize.value"
      quaternary
      size="tiny"
      @click="toggleMaximize"
    >
      <template #icon>
        <NIcon>
          <Icon
            :icon="isContentMaximized ? 'lucide:minimize' : 'lucide:maximize'"
            width="14"
          />
        </NIcon>
      </template>
    </NButton>
  </div>
</template>

<style scoped>
.tabbar-root {
  border-bottom: 1px solid hsl(var(--border));
}

/* 按钮间竖杠分隔线 */
.tab-divider {
  display: inline-block;
  flex-shrink: 0;
  width: 1px;
  height: 16px;
  margin: 0 2px;
  background: hsl(var(--border));
  vertical-align: middle;
}

/* 隐藏内部滚动条，由左右箭头代替 */
.tabbar-viewport {
  scrollbar-width: none;
}

.tabbar-viewport::-webkit-scrollbar {
  display: none;
}
</style>
