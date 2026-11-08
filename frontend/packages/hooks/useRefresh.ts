/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import { useRoute } from 'vue-router'
import { useTabbarStore } from '~/stores'

/**
 * 不整页刷新地重建当前页面组件
 * 递增 tabbarStore 里该路径的 refreshSeed，由 BasicLayout 监听重建
 */
export function useRefresh() {
  const route = useRoute()
  const tabbarStore = useTabbarStore()

  /** 刷新当前路由对应的页面 */
  function refresh() {
    tabbarStore.refreshTab(route.path)
  }

  return { refresh }
}
