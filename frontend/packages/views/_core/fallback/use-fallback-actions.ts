/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import type { Router } from 'vue-router'
import { HOME_PATH } from '~/constants'
import { useAccessStore } from '~/stores'

/**
 * 错误页的「返回首页」
 * 首页是动态路由，没注册时 `router.replace('/')` 会被守卫打回 404
 * 故先解析目标，解析不出说明路由表没建起来，整页重载让守卫重新拉菜单
 */
export function goHome(router: Router) {
  const accessStore = useAccessStore()
  const target = accessStore.homePath || HOME_PATH
  const resolved = router.resolve(target)
  const isValid = resolved.matched.length > 0
    && resolved.name !== 'NotFound'
    && resolved.name !== 'NotFoundCatchAll'

  if (isValid) {
    void router.replace(target)
    return
  }

  // 整页重载是唯一出路：守卫里路由表建失败会把 isRoutesLoaded 留在未加载态，
  // 重载后守卫会重新拉菜单并注册路由
  // 必须显式 reload——hash 模式下只改 hash 不触发重载，守卫按普通导航跑一遍，
  // 目标路由依然没注册，又被打回 404，表现就是「点了没反应」
  const base = import.meta.env.VITE_BASE || '/'
  const normalizedBase = base.endsWith('/')
    ? base
    : `${base}/`
  const href = import.meta.env.VITE_ROUTER_HISTORY === 'history'
    ? target
    : `${normalizedBase}#${target}`
  window.location.replace(href)
  window.location.reload()
}
