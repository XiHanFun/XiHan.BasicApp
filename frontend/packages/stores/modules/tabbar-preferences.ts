import { computed } from 'vue'
import { useAppStore } from '~/stores/app'

export function useTabbarPreferences() {
  const appStore = useAppStore()

  const tabbarEnabled = computed({
    get: () => appStore.tabbarEnabled,
    set: (value) => appStore.setTabbarEnabled(value),
  })
  const tabbarPersist = computed({
    get: () => appStore.tabbarPersist,
    set: (value) => appStore.setTabbarPersist(value),
  })
  const tabbarVisitHistory = computed({
    get: () => appStore.tabbarVisitHistory,
    set: (value) => appStore.setTabbarVisitHistory(value),
  })
  const tabbarDraggable = computed({
    get: () => appStore.tabbarDraggable,
    set: (value) => appStore.setTabbarDraggable(value),
  })
  const tabbarShowMore = computed({
    get: () => appStore.tabbarShowMore,
    set: (value) => appStore.setTabbarShowMore(value),
  })
  const tabbarShowMaximize = computed({
    get: () => appStore.tabbarShowMaximize,
    set: (value) => appStore.setTabbarShowMaximize(value),
  })
  const tabbarMaxCount = computed({
    get: () => appStore.tabbarMaxCount,
    set: (value) => appStore.setTabbarMaxCount(value),
  })

  return {
    tabbarEnabled,
    tabbarPersist,
    tabbarVisitHistory,
    tabbarDraggable,
    tabbarShowMore,
    tabbarShowMaximize,
    tabbarMaxCount,
  }
}
