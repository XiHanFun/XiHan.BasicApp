/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import type { TabItem } from '~/types'
import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import { HOME_PATH, TABS_LIST_KEY } from '~/constants'
import { useAppStore } from '~/stores/app'
import { SessionStorage } from '~/utils'

export const useTabbarStore = defineStore('tabbar', () => {
  const appStore = useAppStore()
  // 菜单固定的标签由 syncAffixTabs 在菜单装好后放进来
  const tabs = ref<TabItem[]>(SessionStorage.get<TabItem[]>(TABS_LIST_KEY) ?? [])

  const activeTab = ref(HOME_PATH)
  const refreshSeeds = ref<Record<string, number>>({})

  const tabKeys = computed(() => tabs.value.map(item => item.key))

  // KeepAlive 的 include：所有「已打开 且 开了缓存」标签的路由名（= 组件强制名，见 router/dynamic.ts）
  // 必须是全部缓存标签，而非仅当前路由——否则切走的标签会被踢出缓存，返回时重渲染
  const cachedTabNames = computed(() =>
    tabs.value.filter(item => item.keepAlive && item.name).map(item => item.name as string),
  )

  // 补齐缓存相关字段：sessionStorage 里旧的持久化标签可能缺 name、keepAlive
  function mergeTab(existing: TabItem, tab: TabItem) {
    existing.name = tab.name
    existing.keepAlive = tab.keepAlive
    if (tab.meta) {
      existing.meta = {
        ...existing.meta,
        ...tab.meta,
      }
      existing.title = tab.title
    }
    if (tab.affix) {
      existing.affix = true
      existing.pinned = true
      existing.closable = false
    }
  }

  function ensureTab(tab: TabItem) {
    const existing = tabs.value.find(item => item.key === tab.key)
    if (!existing) {
      tabs.value.push(tab)
    }
    else {
      mergeTab(existing, tab)
    }
    if (appStore.tabbarPersist) {
      SessionStorage.set(TABS_LIST_KEY, tabs.value)
    }
    activeTab.value = tab.key
  }

  /**
   * 菜单装好后放进菜单固定的标签：按菜单顺序排在最前，没访问过也在，不改当前标签
   * 上一份菜单固定、这一份不再固定的标签降成普通标签
   */
  function syncAffixTabs(affixTabs: TabItem[]) {
    const keys = new Set(affixTabs.map(tab => tab.key))
    for (const tab of tabs.value) {
      if (tab.affix && !keys.has(tab.key)) {
        tab.affix = false
        tab.pinned = false
        tab.closable = true
      }
    }
    const fixed = affixTabs.map((tab) => {
      const existing = tabs.value.find(item => item.key === tab.key)
      if (!existing) {
        return tab
      }
      mergeTab(existing, tab)
      return existing
    })
    tabs.value = [
      ...fixed,
      ...tabs.value.filter(tab => !keys.has(tab.key)),
    ]
    if (appStore.tabbarPersist) {
      SessionStorage.set(TABS_LIST_KEY, tabs.value)
    }
  }

  function togglePin(key: string) {
    const tab = tabs.value.find(item => item.key === key)
    if (!tab || tab.affix) {
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
    const index = tabs.value.findIndex(item => item.key === key)
    if (index < 0) {
      return
    }
    const current = tabs.value[index]
    if (!current) {
      return
    }
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
    tabs.value = tabs.value.filter(tab => !tab.closable || tab.key === key)
    if (appStore.tabbarPersist) {
      SessionStorage.set(TABS_LIST_KEY, tabs.value)
    }
    activeTab.value = key
  }

  /**
   * 激活标签被批量关掉时收敛到本次操作的目标标签，目标也不在则回落首页
   * 与 removeTab / closeOthers / closeAll 的兜底同一口径，免得没有高亮项而内容区仍停在已关闭页
   */
  function ensureActiveTabAlive(fallbackKey: string) {
    if (tabs.value.some(tab => tab.key === activeTab.value)) {
      return
    }
    activeTab.value = tabs.value.some(tab => tab.key === fallbackKey)
      ? fallbackKey
      : HOME_PATH
  }

  function closeLeft(key: string) {
    const currentIndex = tabs.value.findIndex(tab => tab.key === key)
    if (currentIndex < 0) {
      return
    }
    tabs.value = tabs.value.filter((tab, index) => !tab.closable || index >= currentIndex)
    ensureActiveTabAlive(key)
    if (appStore.tabbarPersist) {
      SessionStorage.set(TABS_LIST_KEY, tabs.value)
    }
  }

  function closeRight(key: string) {
    const currentIndex = tabs.value.findIndex(tab => tab.key === key)
    if (currentIndex < 0) {
      return
    }
    tabs.value = tabs.value.filter((tab, index) => !tab.closable || index <= currentIndex)
    ensureActiveTabAlive(key)
    if (appStore.tabbarPersist) {
      SessionStorage.set(TABS_LIST_KEY, tabs.value)
    }
  }

  function closeAll() {
    tabs.value = tabs.value.filter(tab => !tab.closable)
    activeTab.value = tabs.value[0]?.key ?? HOME_PATH
    if (appStore.tabbarPersist) {
      SessionStorage.set(TABS_LIST_KEY, tabs.value)
    }
  }

  /**
   * 回到初始状态（退出登录）：一个标签都不留，也不留持久化的那份
   * 不用 closeAll：它留着固定的标签，那些页面下一个登录的人未必有
   */
  function $reset() {
    tabs.value = []
    activeTab.value = HOME_PATH
    refreshSeeds.value = {}
    SessionStorage.remove(TABS_LIST_KEY)
  }

  function moveTab(sourcePath: string, targetPath: string) {
    if (!sourcePath || !targetPath || sourcePath === targetPath) {
      return
    }
    const sourceIndex = tabs.value.findIndex(tab => tab.path === sourcePath)
    const targetIndex = tabs.value.findIndex(tab => tab.path === targetPath)
    if (sourceIndex < 0 || targetIndex < 0) {
      return
    }
    const [source] = tabs.value.splice(sourceIndex, 1)
    if (!source) {
      return
    }
    tabs.value.splice(targetIndex, 0, source)
    if (appStore.tabbarPersist) {
      SessionStorage.set(TABS_LIST_KEY, tabs.value)
    }
  }

  return {
    tabs,
    activeTab,
    tabKeys,
    cachedTabNames,
    ensureTab,
    syncAffixTabs,
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
    $reset,
  }
})
