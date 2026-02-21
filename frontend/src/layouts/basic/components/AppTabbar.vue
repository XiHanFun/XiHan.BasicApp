<script lang="ts" setup>
import type { DropdownOption } from 'naive-ui'
import type { TabItem } from '~/types'
import { Icon } from '@iconify/vue'
import { NButton, NDropdown, NIcon } from 'naive-ui'
import { computed, h, onBeforeUnmount, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import { useAppStore, useTabbarStore } from '~/stores'

defineOptions({ name: 'AppTabbar' })

const route = useRoute()
const router = useRouter()
const { t, te } = useI18n()
const appStore = useAppStore()
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

const tabThemeVars = computed(() => {
  const color = appStore.themeColor
  return {
    '--tab-active-color': color,
    '--tab-active-bg': `color-mix(in srgb, ${color} 18%, hsl(var(--background)))`,
  }
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
  return visibleTabs.value.find((item) => item.path === path)
}

function getTabDisableState(path: string, closable: boolean) {
  const currentIndex = visibleTabs.value.findIndex((item) => item.path === path)
  const leftTabs = visibleTabs.value.slice(0, currentIndex)
  const rightTabs = visibleTabs.value.slice(currentIndex + 1)
  const currentTab = getTabByPath(path)

  const hasLeftClosable = leftTabs.some((item) => item.closable)
  const hasRightClosable = rightTabs.some((item) => item.closable)
  const hasOtherClosable = visibleTabs.value.some((item) => item.closable && item.path !== path)
  const hasAnyClosable = visibleTabs.value.some((item) => item.closable)

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

onMounted(() => {
  window.addEventListener(
    'xihan-content-maximized-change',
    syncContentMaximizeState as EventListener,
  )
  window.dispatchEvent(new CustomEvent('xihan-sync-content-maximize-state'))
})

onBeforeUnmount(() => {
  window.removeEventListener(
    'xihan-content-maximized-change',
    syncContentMaximizeState as EventListener,
  )
})
</script>

<template>
  <div
    v-if="appStore.tabbarEnabled"
    :style="tabThemeVars"
    class="tabbar-root flex items-center gap-1 overflow-x-auto bg-[var(--tabbar-bg)] px-3 py-1"
  >
    <div
      v-for="(item, index) in localizedTabs"
      :key="item.key"
      class="chrome-tab group relative flex h-8 shrink-0 cursor-pointer items-center"
      :class="{
        'is-active': route.fullPath === item.path,
        'is-last-tab': index === localizedTabs.length - 1,
      }"
      role="button"
      tabindex="0"
      @click="handleJump(item.path)"
      @contextmenu.prevent="openContextMenu($event, item)"
      @keydown.enter.prevent="handleJump(item.path)"
    >
      <span class="chrome-tab__title">{{ item.displayTitle }}</span>
      <button
        v-if="item.closable"
        class="chrome-tab__close flex h-4 w-4 items-center justify-center rounded-full"
        type="button"
        aria-label="关闭标签页"
        @click.stop="handleClose(item.path, $event)"
      >
        <NIcon size="12">
          <Icon icon="lucide:x" />
        </NIcon>
      </button>
      <button
        v-else-if="item.pinned"
        class="chrome-tab__pin flex h-4 w-4 items-center justify-center rounded-full"
        type="button"
        @click.stop="tabbarStore.togglePin(item.path)"
      >
        <Icon icon="lucide:pin" width="11" />
      </button>
    </div>

    <NDropdown
      trigger="manual"
      :show="contextMenuVisible"
      :x="contextMenuX"
      :y="contextMenuY"
      :options="getContextOptions(contextTabPath, contextTabClosable, contextTabPinned)"
      @clickoutside="contextMenuVisible = false"
      @select="(key) => handleDropdownSelect(String(key))"
    >
      <span class="sr-only" />
    </NDropdown>

    <div class="ml-auto flex items-center gap-1">
      <NButton
        v-if="appStore.widgetRefresh"
        quaternary
        circle
        size="tiny"
        @click="refreshCurrentTab"
      >
        <template #icon>
          <NIcon>
            <Icon icon="lucide:refresh-cw" width="14" />
          </NIcon>
        </template>
      </NButton>
      <NButton
        v-if="appStore.tabbarShowMaximize"
        quaternary
        circle
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
  </div>
</template>

<style scoped>
.tabbar-root {
  border-bottom-width: 1px;
  border-bottom-style: solid;
  border-bottom-color: var(--border-color);
}

.chrome-tab {
  min-width: 110px;
  max-width: 180px;
  border-radius: 6px 6px 0 0;
  border: 1px solid transparent;
  border-bottom-color: transparent;
  padding: 0 10px;
  color: hsl(var(--muted-foreground));
  font-size: 13px;
  transition: all 0.2s ease;
}

.chrome-tab::after {
  position: absolute;
  top: 7px;
  right: -1px;
  height: 14px;
  width: 1px;
  background: hsl(var(--border));
  content: '';
}

.chrome-tab.is-last-tab::after {
  display: none;
}

.chrome-tab.is-active::after,
.chrome-tab:hover::after {
  opacity: 0;
}

.chrome-tab:hover {
  background: hsl(var(--accent));
}

.chrome-tab.is-active {
  border-color: hsl(var(--border));
  background: var(--tab-active-bg);
  color: var(--tab-active-color);
  font-weight: 500;
}

.chrome-tab__title {
  flex: 1;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.chrome-tab__close {
  margin-left: 6px;
  border: 0;
  background: transparent;
  padding: 0;
  color: currentcolor;
  opacity: 0.65;
  transition: all 0.2s ease;
}

.chrome-tab:hover .chrome-tab__close,
.chrome-tab.is-active .chrome-tab__close {
  color: currentcolor;
  opacity: 1;
}

.chrome-tab__pin {
  margin-left: 6px;
  color: var(--tab-active-color);
  opacity: 0.85;
}

.chrome-tab__close:hover {
  background: hsl(var(--heavy));
  color: hsl(var(--accent-foreground));
}
</style>
