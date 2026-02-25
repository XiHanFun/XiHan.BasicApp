import type { TabItem } from '~/types'
import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import { HOME_PATH, TABS_LIST_KEY } from '~/constants'
import { useAppStore } from '~/stores/app'
import { SessionStorage } from '~/utils'

export const useTabbarStore = defineStore('tabbar', () => {
  const appStore = useAppStore()
  const defaultTab: TabItem = {
    key: HOME_PATH,
    title: 'menu.workspace',
    path: HOME_PATH,
    pinned: true,
    closable: false,
  }
  const tabs = ref<TabItem[]>(SessionStorage.get<TabItem[]>(TABS_LIST_KEY) ?? [defaultTab])

  const activeTab = ref(HOME_PATH)
  const refreshSeeds = ref<Record<string, number>>({})

  const tabKeys = computed(() => tabs.value.map((item) => item.key))

  function ensureTab(tab: TabItem) {
    const existing = tabs.value.find((item) => item.key === tab.key)
    if (!existing) {
      tabs.value.push(tab)
    } else if (tab.meta) {
      existing.meta = { ...existing.meta, ...tab.meta }
      existing.title = tab.title
    }
    if (appStore.tabbarPersist) {
      SessionStorage.set(TABS_LIST_KEY, tabs.value)
    }
    activeTab.value = tab.key
  }

  function togglePin(key: string) {
    const tab = tabs.value.find((item) => item.key === key)
    if (!tab || tab.path === HOME_PATH) {
      return
    }
    tab.pinned = !tab.pinned
    tab.closable = !tab.pinned
    if (appStore.tabbarPersist) {
      SessionStorage.set(TABS_LIST_KEY, tabs.value)
    }
  }

  function refreshTab(path: string) {
    refreshSeeds.value[path] = (refreshSeeds.value[path] || 0) + 1
  }

  function getRefreshSeed(path: string) {
    return refreshSeeds.value[path] || 0
  }

  function setActiveTab(key: string) {
    activeTab.value = key
  }

  function removeTab(key: string) {
    const index = tabs.value.findIndex((item) => item.key === key)
    if (index < 0) {
      return
    }
    const current = tabs.value[index]
    if (!current.closable) {
      return
    }
    tabs.value.splice(index, 1)
    if (appStore.tabbarPersist) {
      SessionStorage.set(TABS_LIST_KEY, tabs.value)
    }
    if (activeTab.value === key) {
      activeTab.value = tabs.value[Math.max(0, index - 1)]?.key ?? HOME_PATH
    }
  }

  function closeOthers(key: string) {
    tabs.value = tabs.value.filter((tab) => !tab.closable || tab.key === key)
    if (appStore.tabbarPersist) {
      SessionStorage.set(TABS_LIST_KEY, tabs.value)
    }
    activeTab.value = key
  }

  function closeLeft(key: string) {
    const currentIndex = tabs.value.findIndex((tab) => tab.key === key)
    if (currentIndex < 0) {
      return
    }
    tabs.value = tabs.value.filter((tab, index) => !tab.closable || index >= currentIndex)
    if (appStore.tabbarPersist) {
      SessionStorage.set(TABS_LIST_KEY, tabs.value)
    }
  }

  function closeRight(key: string) {
    const currentIndex = tabs.value.findIndex((tab) => tab.key === key)
    if (currentIndex < 0) {
      return
    }
    tabs.value = tabs.value.filter((tab, index) => !tab.closable || index <= currentIndex)
    if (appStore.tabbarPersist) {
      SessionStorage.set(TABS_LIST_KEY, tabs.value)
    }
  }

  function closeAll() {
    tabs.value = tabs.value.filter((tab) => !tab.closable)
    activeTab.value = tabs.value[0]?.key ?? HOME_PATH
    if (appStore.tabbarPersist) {
      SessionStorage.set(TABS_LIST_KEY, tabs.value)
    }
  }

  function moveTab(sourcePath: string, targetPath: string) {
    if (!sourcePath || !targetPath || sourcePath === targetPath) {
      return
    }
    const sourceIndex = tabs.value.findIndex((tab) => tab.path === sourcePath)
    const targetIndex = tabs.value.findIndex((tab) => tab.path === targetPath)
    if (sourceIndex < 0 || targetIndex < 0) {
      return
    }
    const [source] = tabs.value.splice(sourceIndex, 1)
    tabs.value.splice(targetIndex, 0, source)
    if (appStore.tabbarPersist) {
      SessionStorage.set(TABS_LIST_KEY, tabs.value)
    }
  }

  return {
    tabs,
    activeTab,
    tabKeys,
    ensureTab,
    setActiveTab,
    removeTab,
    closeOthers,
    closeLeft,
    closeRight,
    closeAll,
    moveTab,
    togglePin,
    refreshTab,
    getRefreshSeed,
  }
})
