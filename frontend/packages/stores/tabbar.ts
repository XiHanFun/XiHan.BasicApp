import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import type { TabItem } from '~/types'
import { HOME_PATH, TABS_LIST_KEY } from '~/constants'
import { storage } from '~/utils'

export const useTabbarStore = defineStore('tabbar', () => {
  const defaultTab: TabItem = {
    key: HOME_PATH,
    title: 'Dashboard',
    path: HOME_PATH,
    closable: false,
  }
  const tabs = ref<TabItem[]>(storage.get<TabItem[]>(TABS_LIST_KEY) ?? [defaultTab])

  const activeTab = ref(HOME_PATH)

  const tabKeys = computed(() => tabs.value.map((item) => item.key))

  function ensureTab(tab: TabItem) {
    const exists = tabs.value.some((item) => item.key === tab.key)
    if (!exists) {
      tabs.value.push(tab)
      storage.set(TABS_LIST_KEY, tabs.value)
    }
    activeTab.value = tab.key
  }

  function setActiveTab(key: string) {
    activeTab.value = key
  }

  function removeTab(key: string) {
    const index = tabs.value.findIndex((item) => item.key === key)
    if (index < 0) return
    const current = tabs.value[index]
    if (!current.closable) return
    tabs.value.splice(index, 1)
    storage.set(TABS_LIST_KEY, tabs.value)
    if (activeTab.value === key) {
      activeTab.value = tabs.value[Math.max(0, index - 1)]?.key ?? HOME_PATH
    }
  }

  function closeOthers(key: string) {
    tabs.value = tabs.value.filter((tab) => !tab.closable || tab.key === key)
    storage.set(TABS_LIST_KEY, tabs.value)
    activeTab.value = key
  }

  return {
    tabs,
    activeTab,
    tabKeys,
    ensureTab,
    setActiveTab,
    removeTab,
    closeOthers,
  }
})
