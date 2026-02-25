import type { RouteRecordRaw } from 'vue-router'
import type { MenuRoute } from '~/types'

const viewModules = import.meta.glob('@/views/**/*.vue')

function resolveView(component?: string) {
  if (!component) return null
  const normalized = component
    .replace(/^\/+/, '')
    .replace(/^views\//, '')
    .replace(/\.vue$/, '')
  const keys = [`/src/views/${normalized}.vue`, `/src/views/${normalized}/index.vue`]

  for (const key of keys) {
    const matched = viewModules[key]
    if (matched) return matched
  }

  return null
}

export function mapMenuToRoutes(menuRoutes: MenuRoute[]): RouteRecordRaw[] {
  return menuRoutes
    .filter((item) => !item.meta?.hidden && !!item.path)
    .map((item) => {
      const component = resolveView(item.component)
      const route: any = {
        path: item.path,
        name: item.name,
        meta: item.meta as unknown as Record<string, unknown>,
      }

      if (item.redirect) {
        route.redirect = item.redirect
      }

      if (component) {
        route.component = component
      }

      if (item.children?.length) {
        route.children = mapMenuToRoutes(item.children)
      }

      return route as RouteRecordRaw
    })
}
