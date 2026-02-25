import { defineStore } from 'pinia'
import { computed } from 'vue'
import { useAppStore } from '~/stores/app'

export const useLayoutStateStore = defineStore('layout-state', () => {
  const appStore = useAppStore()

  const layoutMode = computed({
    get: () => appStore.layoutMode,
    set: (value) => appStore.setLayoutMode(value),
  })
  const sidebarCollapsed = computed({
    get: () => appStore.sidebarCollapsed,
    set: (value) => appStore.setSidebarCollapsed(value),
  })
  const sidebarWidth = computed({
    get: () => appStore.sidebarWidth,
    set: (value) => appStore.setSidebarWidth(value),
  })
  const sidebarShow = computed({
    get: () => appStore.sidebarShow,
    set: (value) => appStore.setSidebarShow(value),
  })
  const sidebarExpandOnHover = computed({
    get: () => appStore.sidebarExpandOnHover,
    set: (value) => appStore.setSidebarExpandOnHover(value),
  })
  const headerMode = computed({
    get: () => appStore.headerMode as 'fixed' | 'static' | 'auto' | 'auto-scroll',
    set: (value: 'fixed' | 'static' | 'auto' | 'auto-scroll') => appStore.setHeaderMode(value),
  })
  const headerMenuAlign = computed({
    get: () => appStore.headerMenuAlign,
    set: (value) => appStore.setHeaderMenuAlign(value),
  })
  const contentCompact = computed({
    get: () => appStore.contentCompact,
    set: (value) => appStore.setContentCompact(value),
  })
  const contentMaxWidth = computed({
    get: () => appStore.contentMaxWidth,
    set: (value) => appStore.setContentMaxWidth(value),
  })
  const navigationStyle = computed({
    get: () => appStore.navigationStyle,
    set: (value) => appStore.setNavigationStyle(value),
  })
  const navigationSplit = computed({
    get: () => appStore.navigationSplit,
    set: (value) => appStore.setNavigationSplit(value),
  })
  const navigationAccordion = computed({
    get: () => appStore.navigationAccordion,
    set: (value) => appStore.setNavigationAccordion(value),
  })

  function toggleSidebar() {
    appStore.toggleSidebar()
  }

  return {
    layoutMode,
    sidebarCollapsed,
    sidebarWidth,
    sidebarShow,
    sidebarExpandOnHover,
    headerMode,
    headerMenuAlign,
    contentCompact,
    contentMaxWidth,
    navigationStyle,
    navigationSplit,
    navigationAccordion,
    toggleSidebar,
  }
})
