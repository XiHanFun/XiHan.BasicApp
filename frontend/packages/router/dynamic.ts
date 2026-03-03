import type { RouteRecordRaw } from 'vue-router'
import type { MenuRoute } from '~/types'

const viewModules = import.meta.glob('@/views/**/*.vue')

function toKebabCase(input: string) {
  return input
    .replace(/([a-z0-9])([A-Z])/g, '$1-$2')
    .replace(/_/g, '-')
    .toLowerCase()
}

function resolveView(component?: string) {
  if (!component) return null

  const normalized = component
    .replace(/^\/+/, '')
    .replace(/^views\//, '')
    .replace(/\.vue$/, '')

  const rawPath = normalized
  const lowerPath = normalized.toLowerCase()
  const kebabPath = normalized
    .split('/')
    .map(segment => toKebabCase(segment))
    .join('/')

  const removeIndexSuffix = (path: string) => path.replace(/\/index$/i, '')
  const candidates = new Set([
    rawPath,
    lowerPath,
    kebabPath,
    removeIndexSuffix(rawPath),
    removeIndexSuffix(lowerPath),
    removeIndexSuffix(kebabPath),
  ])

  const keys = Array.from(candidates).flatMap(path => [
    `/src/views/${path}.vue`,
    `/src/views/${path}/index.vue`,
  ])

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
