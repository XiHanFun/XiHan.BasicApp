<script lang="ts" setup>
import type { DropdownOption } from 'naive-ui'
import type { TabItem } from '~/types'
import { Icon } from '@iconify/vue'
import { NButton, NDropdown, NIcon } from 'naive-ui'
import Sortable from 'sortablejs'
import { computed, h, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import { useContentMaximize, useRefresh } from '~/hooks'
import { useAppStore, useTabbarPreferences, useTabbarStore } from '~/stores'
import TabbarActions from './tabbar/TabbarActions.vue'
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
const sortableInstance = ref<null | Sortable>(null)

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
    icon: getDropdownIcon(item.pinned ? 'lucide:pin' : 'lucide:file'),
  })),
)

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
      window.open(
        import.meta.env.VITE_ROUTER_HISTORY === 'history'
          ? `${window.location.origin}${tabPath}`
          : `${window.location.origin}${window.location.pathname}#${tabPath}`,
        '_blank',
      )
      break
  }
}

function getDropdownIcon(icon: string) {
  return () =>
    h(
      NIcon,
      {
        size: 16,
      },
      {
        default: () => h(Icon, { icon }),
      },
    )
}

function getTabByPath(path: string) {
  return visibleTabs.value.find(item => item.path === path)
}

function getTabDisableState(path: string, closable: boolean) {
  const currentIndex = visibleTabs.value.findIndex(item => item.path === path)
  const leftTabs = visibleTabs.value.slice(0, currentIndex)
  const rightTabs = visibleTabs.value.slice(currentIndex + 1)
  const currentTab = getTabByPath(path)

  const hasLeftClosable = leftTabs.some(item => item.closable)
  const hasRightClosable = rightTabs.some(item => item.closable)
  const hasOtherClosable = visibleTabs.value.some(item => item.closable && item.path !== path)
  const hasAnyClosable = visibleTabs.value.some(item => item.closable)

  return {
    closeAllDisabled: !hasAnyClosable,
    closeCurrentDisabled: !closable,
    closeLeftDisabled: !hasLeftClosable,
    closeOthersDisabled: !hasOtherClosable,
    closeRightDisabled: !hasRightClosable,
    pinDisabled: currentTab?.path === '/',
  }
}

function getContextOptions(path: string, closable: boolean, pinned: boolean): DropdownOption[] {
  const {
    closeAllDisabled,
    closeCurrentDisabled,
    closeLeftDisabled,
    closeOthersDisabled,
    closeRightDisabled,
    pinDisabled,
  } = getTabDisableState(path, closable)

  return [
    { key: 'reload', label: t('tabbar.reload'), icon: getDropdownIcon('lucide:refresh-cw') },
    { key: 'open', label: t('tabbar.open'), icon: getDropdownIcon('lucide:external-link') },
    {
      key: 'pin',
      label: pinned ? t('tabbar.unpin') : t('tabbar.pin'),
      disabled: pinDisabled,
      icon: getDropdownIcon(pinned ? 'lucide:pin-off' : 'lucide:pin'),
    },
    {
      key: 'maximize',
      label: isContentMaximized.value ? t('tabbar.unmaximize') : t('tabbar.maximize'),
      icon: getDropdownIcon(isContentMaximized.value ? 'lucide:minimize-2' : 'lucide:maximize-2'),
    },
    { key: 'divider-1', type: 'divider' },
    {
      key: 'close',
      label: t('tabbar.close'),
      disabled: closeCurrentDisabled,
      icon: getDropdownIcon('lucide:x'),
    },
    { key: 'divider-2', type: 'divider' },
    {
      key: 'closeLeft',
      label: t('tabbar.close_left'),
      disabled: closeLeftDisabled,
      icon: getDropdownIcon('lucide:panel-left-close'),
    },
    {
      key: 'closeRight',
      label: t('tabbar.close_right'),
      disabled: closeRightDisabled,
      icon: getDropdownIcon('lucide:panel-right-close'),
    },
    {
      key: 'closeOthers',
      label: t('tabbar.close_others'),
      disabled: closeOthersDisabled,
      icon: getDropdownIcon('lucide:circle-off'),
    },
    {
      key: 'closeAll',
      label: t('tabbar.close_all'),
      disabled: closeAllDisabled,
      icon: getDropdownIcon('lucide:rows-3'),
    },
  ]
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
  const target = getTabByPath(path)
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
  return element.classList.contains('group')
    ? element
    : (element.closest('.group') as HTMLElement | null)
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

  sortableInstance.value = Sortable.create(el, {
    animation: 380,
    easing: 'cubic-bezier(0.22, 1, 0.36, 1)',
    ghostClass: 'chrome-tab--ghost',
    chosenClass: 'chrome-tab--chosen',
    dragClass: 'chrome-tab--dragging',
    draggable: '.chrome-tab.draggable',
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

function handleTabsWheel(e: WheelEvent) {
  if (!appStore.tabbarScrollResponse)
    return
  const container = tabsContainerRef.value?.parentElement
  if (!container)
    return
  e.preventDefault()
  container.scrollLeft += e.deltaY !== 0 ? e.deltaY : e.deltaX
}

onMounted(() => {
  initSortable()
  tabsContainerRef.value?.parentElement?.addEventListener('wheel', handleTabsWheel, { passive: false })
})

onBeforeUnmount(() => {
  destroySortable()
  tabsContainerRef.value?.parentElement?.removeEventListener('wheel', handleTabsWheel)
})

watch(() => tabbarPreferences.tabbarDraggable.value, () => {
  initSortable()
})
</script>

<template>
  <div
    v-if="appStore.tabbarEnabled"
    :style="tabThemeVars"
    class="tabbar-root flex items-center bg-[var(--tabbar-bg)] px-3 py-1"
  >
    <div class="tabbar-list min-w-0 flex-1 overflow-x-auto">
      <div ref="tabsContainerRef" class="flex min-w-max items-center pr-4">
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
            @jump="handleJump"
            @contextmenu="openContextMenu"
            @close="handleClose"
            @toggle-pin="tabbarStore.togglePin"
            @middle-close="handleMiddleClose"
          />
        </TransitionGroup>
      </div>
    </div>

    <TabbarContextMenu
      :show="contextMenuVisible"
      :x="contextMenuX"
      :y="contextMenuY"
      :options="getContextOptions(contextTabPath, contextTabClosable, contextTabPinned)"
      @close="contextMenuVisible = false"
      @select="handleDropdownSelect"
    />
    <NDropdown
      v-if="tabbarPreferences.tabbarShowMore.value"
      :options="moreTabOptions"
      @select="(key) => handleMoreTabSelect(String(key))"
    >
      <NButton quaternary circle size="tiny">
        <template #icon>
          <NIcon>
            <Icon icon="lucide:ellipsis" width="14" />
          </NIcon>
        </template>
      </NButton>
    </NDropdown>
    <TabbarActions
      class="ml-2"
      :show-refresh="appStore.widgetRefresh"
      :show-maximize="tabbarPreferences.tabbarShowMaximize.value"
      :is-content-maximized="isContentMaximized"
      @refresh="refreshCurrentTab"
      @maximize="toggleMaximize"
    />
  </div>
</template>

<style scoped>
.tabbar-root {
  border-bottom-width: 1px;
  border-bottom-style: solid;
  border-bottom-color: var(--border-color);
}

.tabbar-list {
  scrollbar-width: thin;
}

:deep(.chrome-tab--chosen) {
  cursor: grabbing;
}

:deep(.chrome-tab--ghost) {
  opacity: 0.4;
}

:deep(.chrome-tab--dragging) {
  transform: scale(0.98);
}

/* 过渡类由 TransitionGroup 加到 TabbarTabItem 根元素上，
   实际 CSS 定义在 TabbarTabItem.vue 的 scoped 样式中 */
</style>
