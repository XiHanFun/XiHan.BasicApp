<script lang="ts" setup>
import type { DropdownOption } from 'naive-ui'
import type { TabItem } from '~/types'
import { Icon } from '@iconify/vue'
import { useDebounceFn } from '@vueuse/core'
import { NButton, NDropdown, NIcon } from 'naive-ui'
import Sortable from 'sortablejs'
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import { useContentMaximize, useRefresh } from '~/hooks'
import { useAppStore, useTabbarPreferences, useTabbarStore } from '~/stores'
import {
  buildTabContextOptions,
  createDropdownIcon,
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
const tabsContainerRef = ref<HTMLElement | null>(null)
const scrollViewportRef = ref<HTMLElement | null>(null)
const sortableInstance = ref<null | Sortable>(null)

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
  }
}

function openContextMenu(e: MouseEvent, tab: TabItem) {
  e.preventDefault()
  contextTabPath.value = tab.path
  contextTabClosable.value = tab.closable
  contextTabPinned.value = Boolean(tab.pinned)
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

function destroySortable() {
  sortableInstance.value?.destroy()
  sortableInstance.value = null
}

function findTabElement(element: HTMLElement | null) {
  if (!element) {
    return null
  }
  if (element.classList.contains('group') || element.classList.contains('flat-tab')) {
    return element
  }
  return (element.closest('.group') || element.closest('.flat-tab')) as HTMLElement | null
}

async function initSortable() {
  destroySortable()
  await nextTick()

  const el = tabsContainerRef.value
  if (!el) {
    return
  }

  function resetElState() {
    el!.style.cursor = ''
    el!.querySelector('.draggable')?.classList.remove('dragging')
  }

  const isChromeStyle = appStore.tabbarStyle === 'chrome'

  sortableInstance.value = Sortable.create(el, {
    animation: 380,
    easing: 'cubic-bezier(0.22, 1, 0.36, 1)',
    ghostClass: isChromeStyle ? 'chrome-tab--ghost' : 'flat-tab--ghost',
    chosenClass: isChromeStyle ? 'chrome-tab--chosen' : 'flat-tab--chosen',
    dragClass: isChromeStyle ? 'chrome-tab--dragging' : 'flat-tab--dragging',
    draggable: isChromeStyle ? '.chrome-tab.draggable' : '.flat-tab.draggable',
    onMove: (evt) => {
      if (!tabbarPreferences.tabbarDraggable.value) {
        return false
      }
      const dragged = findTabElement(evt.dragged as HTMLElement)
      const related = findTabElement(evt.related as HTMLElement)
      if (!dragged || !related) {
        return false
      }
      // 固定标签和非固定标签不允许跨区交换
      const draggedPinned = dragged.classList.contains('affix-tab')
      const relatedPinned = related.classList.contains('affix-tab')
      return draggedPinned === relatedPinned
    },
    onStart: (evt) => {
      el.style.cursor = 'grabbing'
      ;(evt.item as HTMLElement).classList.add('dragging')
    },
    onEnd: (evt) => {
      const { newIndex, oldIndex, item } = evt
      resetElState()

      if (!tabbarPreferences.tabbarDraggable.value) {
        return
      }

      const srcParent = findTabElement(item as HTMLElement)
      if (!srcParent || !srcParent.classList.contains('draggable')) {
        return
      }

      if (
        oldIndex !== undefined
        && newIndex !== undefined
        && !Number.isNaN(oldIndex)
        && !Number.isNaN(newIndex)
        && oldIndex !== newIndex
      ) {
        const from = localizedTabs.value[oldIndex]
        const to = localizedTabs.value[newIndex]
        if (from && to && from.path !== to.path) {
          tabbarStore.moveTab(from.path, to.path)
        }
      }
    },
  })
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
  initSortable()
  await nextTick()
  initScrollObservers()
  scrollViewportRef.value?.addEventListener('scroll', debouncedEdge, { passive: true })
  scrollViewportRef.value?.addEventListener('wheel', handleTabsWheel, { passive: false })
})

onBeforeUnmount(() => {
  destroySortable()
  resizeObserver?.disconnect()
  mutationObserver?.disconnect()
  scrollViewportRef.value?.removeEventListener('scroll', debouncedEdge)
  scrollViewportRef.value?.removeEventListener('wheel', handleTabsWheel)
})

watch(() => tabbarPreferences.tabbarDraggable.value, () => {
  initSortable()
})

watch(() => appStore.tabbarStyle, () => {
  nextTick(() => {
    initSortable()
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
    class="tabbar-root flex bg-[var(--tabbar-bg)] px-2"
    :class="appStore.tabbarStyle === 'chrome' ? 'h-10 items-end pt-[4px] pb-0' : 'h-[38px] items-center py-0'"
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
            :icon="isContentMaximized ? 'lucide:minimize-2' : 'lucide:maximize-2'"
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

:deep(.chrome-tab--chosen),
:deep(.flat-tab--chosen) {
  cursor: grabbing;
}

:deep(.chrome-tab--ghost),
:deep(.flat-tab--ghost) {
  opacity: 0.4;
}

:deep(.chrome-tab--dragging),
:deep(.flat-tab--dragging) {
  transform: scale(0.98);
}
</style>
