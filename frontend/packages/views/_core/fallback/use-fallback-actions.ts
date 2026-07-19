import type { Router } from 'vue-router'
import { HOME_PATH } from '~/constants'
import { useAccessStore } from '~/stores'

/**
 * 错误页（404/403/500）的「返回首页」。
 *
 * 不能直接 `router.replace('/')`：守卫会把 `/` 重定向到首页路径，而首页是一条**动态路由**——
 * 菜单没拉到、或当前账号根本没有这个菜单的权限时它压根没注册，守卫解析不出来又把你打回 404，
 * 表现就是"点了返回首页还在 404"。
 *
 * 所以先解析一次目标：解析不出真实路由，说明动态路由表本身没建起来，
 * 此时整页重载才是唯一出路（重载后守卫会重新拉一次菜单并注册路由）。
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

  const base = import.meta.env.VITE_BASE || '/'
  const normalizedBase = base.endsWith('/') ? base : `${base}/`
  window.location.href = import.meta.env.VITE_ROUTER_HISTORY === 'history'
    ? target
    : `${normalizedBase}#${target}`
}
