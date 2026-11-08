/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import { defineStore } from 'pinia'
import { ref } from 'vue'

export const useLayoutBridgeStore = defineStore('layout-bridge', () => {
  const sidebarToggleVersion = ref(0)
  const preferenceDrawerVersion = ref(0)
  const globalSearchVersion = ref(0)
  const lockScreenVersion = ref(0)
  const tabOverviewVersion = ref(0)

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

  return {
    sidebarToggleVersion,
    preferenceDrawerVersion,
    globalSearchVersion,
    lockScreenVersion,
    tabOverviewVersion,
    requestSidebarToggle,
    requestOpenPreferenceDrawer,
    requestOpenGlobalSearch,
    requestLockScreen,
    requestOpenTabOverview,
  }
})
