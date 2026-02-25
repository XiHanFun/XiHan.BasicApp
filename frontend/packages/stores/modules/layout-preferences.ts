import { computed } from 'vue'
import { useLayoutStateStore } from './layout-state'

export function useLayoutPreferences() {
  const layoutStateStore = useLayoutStateStore()

  const layoutMode = computed({
    get: () => layoutStateStore.layoutMode,
    set: (value) => (layoutStateStore.layoutMode = value),
  })
  const sidebarCollapsed = computed({
    get: () => layoutStateStore.sidebarCollapsed,
    set: (value) => (layoutStateStore.sidebarCollapsed = value),
  })
  const sidebarWidth = computed({
    get: () => layoutStateStore.sidebarWidth,
    set: (value) => (layoutStateStore.sidebarWidth = value),
  })
  const sidebarShow = computed({
    get: () => layoutStateStore.sidebarShow,
    set: (value) => (layoutStateStore.sidebarShow = value),
  })
  const sidebarExpandOnHover = computed({
    get: () => layoutStateStore.sidebarExpandOnHover,
    set: (value) => (layoutStateStore.sidebarExpandOnHover = value),
  })
  const headerMode = computed({
    get: () => layoutStateStore.headerMode,
    set: (value) => (layoutStateStore.headerMode = value),
  })
  const headerMenuAlign = computed({
    get: () => layoutStateStore.headerMenuAlign,
    set: (value) => (layoutStateStore.headerMenuAlign = value),
  })
  const contentCompact = computed({
    get: () => layoutStateStore.contentCompact,
    set: (value) => (layoutStateStore.contentCompact = value),
  })
  const contentMaxWidth = computed({
    get: () => layoutStateStore.contentMaxWidth,
    set: (value) => (layoutStateStore.contentMaxWidth = value),
  })

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
  }
}
