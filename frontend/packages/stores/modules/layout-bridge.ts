import { defineStore } from 'pinia'
import { ref } from 'vue'

export const useLayoutBridgeStore = defineStore('layout-bridge', () => {
  const sidebarToggleVersion = ref(0)
  const preferenceDrawerVersion = ref(0)
  const globalSearchVersion = ref(0)
  const lockScreenVersion = ref(0)
  const tabOverviewVersion = ref(0)
  const chatDrawerVersion = ref(0)

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

  function requestOpenTabOverview() {
    tabOverviewVersion.value += 1
  }

  function requestOpenChatDrawer() {
    chatDrawerVersion.value += 1
  }

  return {
    sidebarToggleVersion,
    preferenceDrawerVersion,
    globalSearchVersion,
    lockScreenVersion,
    tabOverviewVersion,
    chatDrawerVersion,
    requestSidebarToggle,
    requestOpenPreferenceDrawer,
    requestOpenGlobalSearch,
    requestLockScreen,
    requestOpenTabOverview,
    requestOpenChatDrawer,
  }
})
