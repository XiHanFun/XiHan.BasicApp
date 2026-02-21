import { computed } from 'vue'
import { useAppStore } from '~/stores/app'

export function useLayoutPreferences() {
  const appStore = useAppStore()

  const layoutMode = computed({
    get: () => appStore.layoutMode,
    set: value => appStore.setLayoutMode(value),
  })
  const sidebarCollapsed = computed({
    get: () => appStore.sidebarCollapsed,
    set: value => appStore.setSidebarCollapsed(value),
  })
  const sidebarWidth = computed({
    get: () => appStore.sidebarWidth,
    set: value => appStore.setSidebarWidth(value),
  })
  const sidebarShow = computed({
    get: () => appStore.sidebarShow,
    set: value => appStore.setSidebarShow(value),
  })
  const sidebarExpandOnHover = computed({
    get: () => appStore.sidebarExpandOnHover,
    set: value => appStore.setSidebarExpandOnHover(value),
  })
  const headerMode = computed({
    get: () => appStore.headerMode,
    set: value => appStore.setHeaderMode(value),
  })
  const headerMenuAlign = computed({
    get: () => appStore.headerMenuAlign,
    set: value => appStore.setHeaderMenuAlign(value),
  })
  const contentCompact = computed({
    get: () => appStore.contentCompact,
    set: value => appStore.setContentCompact(value),
  })
  const contentMaxWidth = computed({
    get: () => appStore.contentMaxWidth,
    set: value => appStore.setContentMaxWidth(value),
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
