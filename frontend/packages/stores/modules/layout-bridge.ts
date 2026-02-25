import { defineStore } from 'pinia'
import { ref } from 'vue'

export const useLayoutBridgeStore = defineStore('layout-bridge', () => {
  const sidebarToggleVersion = ref(0)
  const preferenceDrawerVersion = ref(0)
  const globalSearchVersion = ref(0)
  const lockScreenVersion = ref(0)

  function requestSidebarToggle() {
    sidebarToggleVersion.value += 1
  }

  function requestOpenPreferenceDrawer() {
    preferenceDrawerVersion.value += 1
  }

  function requestOpenGlobalSearch() {
    globalSearchVersion.value += 1
  }

  function requestLockScreen() {
    lockScreenVersion.value += 1
  }

  return {
    sidebarToggleVersion,
    preferenceDrawerVersion,
    globalSearchVersion,
    lockScreenVersion,
    requestSidebarToggle,
    requestOpenPreferenceDrawer,
    requestOpenGlobalSearch,
    requestLockScreen,
  }
})
