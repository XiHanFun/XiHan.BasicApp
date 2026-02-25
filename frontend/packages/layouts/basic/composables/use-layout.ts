import type { Ref } from 'vue'
import type { LayoutMode } from '../contracts'
import { computed, toRef } from 'vue'

export function useLayout(layoutModeRef: Ref<string> | (() => string)) {
  const currentLayout =
    typeof layoutModeRef === 'function' ? computed(layoutModeRef) : toRef(layoutModeRef)

  const isSideNav = computed(() => currentLayout.value === 'side')
  const isSideMixedNav = computed(() => currentLayout.value === 'side-mixed')
  const isHeaderNav = computed(() => currentLayout.value === 'top')
  const isMixedNav = computed(() => currentLayout.value === 'mix')
  const isHeaderMixedNav = computed(() => currentLayout.value === 'header-mix')
  const isHeaderSidebarNav = computed(() => currentLayout.value === 'header-sidebar')
  const isFullContent = computed(() => currentLayout.value === 'full')

  const showHeaderNav = computed(
    () => isHeaderNav.value || isMixedNav.value || isHeaderMixedNav.value,
  )

  const isSideMode = computed(
    () =>
      isMixedNav.value ||
      isSideMixedNav.value ||
      isSideNav.value ||
      isHeaderMixedNav.value ||
      isHeaderSidebarNav.value,
  )

  const isDualColumnMode = computed(() => isSideMixedNav.value || isHeaderMixedNav.value)

  return {
    currentLayout: currentLayout as Ref<LayoutMode>,
    isSideNav,
    isSideMixedNav,
    isHeaderNav,
    isMixedNav,
    isHeaderMixedNav,
    isHeaderSidebarNav,
    isFullContent,
    showHeaderNav,
    isSideMode,
    isDualColumnMode,
  }
}
