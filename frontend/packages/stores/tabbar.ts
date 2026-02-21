import type { TabItem } from '~/types'
import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import { HOME_PATH, TABS_LIST_KEY } from '~/constants'
import { useAppStore } from '~/stores/app'
import { storage } from '~/utils'

export const useTabbarStore = defineStore('tabbar', () => {
  const appStore = useAppStore()
  const defaultTab: TabItem = {
    key: HOME_PATH,
    title: 'Dashboard',
    path: HOME_PATH,
    pinned: true,
    closable: false,
  }
  const tabs = ref<TabItem[]>(storage.get<TabItem[]>(TABS_LIST_KEY) ?? [defaultTab])

  const activeTab = ref(HOME_PATH)

  const tabKeys = computed(() => tabs.value.map(item => item.key))

  function ensureTab(tab: TabItem) {
    const exists = tabs.value.some(item => item.key === tab.key)
    if (!exists) {
      tabs.value.push(tab)
      if (appStore.tabbarPersist) {
        storage.set(TABS_LIST_KEY, tabs.value)
      }
    }
    activeTab.value = tab.key
  }

  function togglePin(key: string) {
    const tab = tabs.value.find(item => item.key === key)
    if (!tab || tab.path === HOME_PATH) {
      return
    }
    tab.pinned = !tab.pinned
    tab.closable = !tab.pinned
    if (appStore.tabbarPersist) {
      storage.set(TABS_LIST_KEY, tabs.value)
    }
  }

  function setActiveTab(key: string) {
    activeTab.value = key
  }

  function removeTab(key: string) {
    const index = tabs.value.findIndex(item => item.key === key)
    if (index < 0) {
      return
    }
    const current = tabs.value[index]
    if (!current.closable) {
      return
    }
    tabs.value.splice(index, 1)
    if (appStore.tabbarPersist) {
      storage.set(TABS_LIST_KEY, tabs.value)
    }
    if (activeTab.value === key) {
      activeTab.value = tabs.value[Math.max(0, index - 1)]?.key ?? HOME_PATH
    }
  }

  function closeOthers(key: string) {
    tabs.value = tabs.value.filter(tab => !tab.closable || tab.key === key)
    if (appStore.tabbarPersist) {
      storage.set(TABS_LIST_KEY, tabs.value)
    }
    activeTab.value = key
  }

  function closeLeft(key: string) {
    const currentIndex = tabs.value.findIndex(tab => tab.key === key)
    if (currentIndex < 0) {
      return
    }
    tabs.value = tabs.value.filter((tab, index) => !tab.closable || index >= currentIndex)
    if (appStore.tabbarPersist) {
      storage.set(TABS_LIST_KEY, tabs.value)
    }
  }

  function closeRight(key: string) {
    const currentIndex = tabs.value.findIndex(tab => tab.key === key)
    if (currentIndex < 0) {
      return
    }
    tabs.value = tabs.value.filter((tab, index) => !tab.closable || index <= currentIndex)
    if (appStore.tabbarPersist) {
      storage.set(TABS_LIST_KEY, tabs.value)
    }
  }

  function closeAll() {
    tabs.value = tabs.value.filter(tab => !tab.closable)
    activeTab.value = tabs.value[0]?.key ?? HOME_PATH
    if (appStore.tabbarPersist) {
      storage.set(TABS_LIST_KEY, tabs.value)
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
    togglePin,
  }
})
