<script lang="ts" setup>
import type { DropdownOption } from 'naive-ui'
import type { TabItem } from '~/types'
import { Icon } from '@iconify/vue'
import { NButton, NDropdown, NIcon } from 'naive-ui'
import Sortable from 'sortablejs'
import { computed, h, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
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
const contextMenuVisible = ref(false)
const contextMenuX = ref(0)
const contextMenuY = ref(0)
const contextTabPath = ref('')
const contextTabClosable = ref(false)
const contextTabPinned = ref(false)
const isContentMaximized = ref(false)
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
    { key: 'reload', label: '重新加载', icon: getDropdownIcon('lucide:refresh-cw') },
    { key: 'open', label: '新窗口打开', icon: getDropdownIcon('lucide:external-link') },
    {
      key: 'pin',
      label: pinned ? '取消固定' : '固定',
      disabled: pinDisabled,
      icon: getDropdownIcon(pinned ? 'lucide:pin-off' : 'lucide:pin'),
    },
    {
      key: 'maximize',
      label: isContentMaximized.value ? '退出最大化' : '最大化',
      icon: getDropdownIcon(isContentMaximized.value ? 'lucide:minimize-2' : 'lucide:maximize-2'),
    },
    { key: 'divider-1', type: 'divider' },
    {
      key: 'close',
      label: '关闭',
      disabled: closeCurrentDisabled,
      icon: getDropdownIcon('lucide:x'),
    },
    { key: 'divider-2', type: 'divider' },
    {
      key: 'closeLeft',
      label: '关闭左侧标签页',
      disabled: closeLeftDisabled,
      icon: getDropdownIcon('lucide:panel-left-close'),
    },
    {
      key: 'closeRight',
      label: '关闭右侧标签页',
      disabled: closeRightDisabled,
      icon: getDropdownIcon('lucide:panel-right-close'),
    },
    {
      key: 'closeOthers',
      label: '关闭其他标签页',
      disabled: closeOthersDisabled,
      icon: getDropdownIcon('lucide:circle-off'),
    },
    {
      key: 'closeAll',
      label: '关闭全部标签页',
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
    animation: 300,
    delay: 400,
    delayOnTouchOnly: true,
    ghostClass: 'chrome-tab--ghost',
    chosenClass: 'chrome-tab--chosen',
    // filter 而非 draggable 选择器：所有子元素均参与 FLIP 动画和索引计数
    // filter 返回 true 表示阻止从该元素发起拖拽（相当于 affix 标签不可拖动）
    filter: (_evt, target: HTMLElement) => {
      const parent = findTabElement(target)
      const isDraggable = parent?.classList.contains('draggable')
      return !isDraggable || !tabbarPreferences.tabbarDraggable.value
    },
    onMove: (evt) => {
      const parent = findTabElement(evt.related as HTMLElement)
      if (parent?.classList.contains('draggable') && tabbarPreferences.tabbarDraggable.value) {
        const isCurrentAffix = (evt.dragged as HTMLElement).classList.contains('affix-tab')
        const isRelatedAffix = (evt.related as HTMLElement).classList.contains('affix-tab')
        return isCurrentAffix === isRelatedAffix
      }
      return false
    },
    onStart: () => {
      el.style.cursor = 'grabbing'
      el.querySelector('.draggable')?.classList.add('dragging')
    },
    onEnd: (evt) => {
      const { newIndex, oldIndex } = evt
      // eslint-disable-next-line ts/no-explicit-any
      const srcElement = ((evt as any).originalEvent as MouseEvent | undefined)?.target as HTMLElement | undefined

      if (!srcElement) {
        resetElState()
        return
      }

      const srcParent = findTabElement(srcElement)
      if (!srcParent) {
        resetElState()
        return
      }

      if (!srcParent.classList.contains('draggable')) {
        resetElState()
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
        if (!from || !to || from.path === to.path) {
          resetElState()
          return
        }
        tabbarStore.moveTab(from.path, to.path)
      }
      resetElState()
    },
  })
}

function syncContentMaximizeState(e: Event) {
  const customEvent = e as CustomEvent<boolean>
  isContentMaximized.value = Boolean(customEvent.detail)
}

function toggleMaximize() {
  window.dispatchEvent(new CustomEvent('xihan-toggle-content-maximize'))
}

function refreshCurrentTab() {
  tabbarStore.refreshTab(route.fullPath)
}

function handleMoreTabSelect(path: string) {
  handleJump(path)
}

onMounted(() => {
  initSortable()
  window.addEventListener(
    'xihan-content-maximized-change',
    syncContentMaximizeState as EventListener,
  )
  window.addEventListener('xihan-refresh-current-tab', refreshCurrentTab as EventListener)
  window.dispatchEvent(new CustomEvent('xihan-sync-content-maximize-state'))
})

onBeforeUnmount(() => {
  destroySortable()
  window.removeEventListener(
    'xihan-content-maximized-change',
    syncContentMaximizeState as EventListener,
  )
  window.removeEventListener('xihan-refresh-current-tab', refreshCurrentTab as EventListener)
})

watch([() => tabbarPreferences.tabbarDraggable.value, () => localizedTabs.value.length], () => {
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
        <TransitionGroup name="tabs-slide">
          <TabbarTabItem
            v-for="(item, index) in localizedTabs"
            :key="item.key"
            :item="item"
            :index="index"
            :active="route.fullPath === item.path"
            :is-last="index === localizedTabs.length - 1"
            :draggable="tabbarPreferences.tabbarDraggable.value && !item.pinned"
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
