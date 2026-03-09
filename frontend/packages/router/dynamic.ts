import type { RouteRecordRaw } from 'vue-router'
import type { MenuRoute } from '~/types'

const viewModules = import.meta.glob('@/views/**/*.vue')
const fallbackView = () => import('~/views/_core/fallback/not-found.vue')

const componentAliasMap: Record<string, string> = {
  // 后端种子菜单兼容映射
  'dashboard/index': 'dashboard/workspace/index',
  'system/log/access': 'system/logs/index',
  'system/log/operation': 'system/logs/index',
  'system/log/exception': 'system/logs/index',
  'system/log/audit': 'system/logs/index',
  'system/monitor/index': 'system/logs/index',
}

const explicitComponentMap: Record<string, () => Promise<unknown>> = {
  // Dashboard
  'dashboard/index': () => import('@/views/dashboard/workspace/index.vue'),
  'dashboard/workspace/index': () => import('@/views/dashboard/workspace/index.vue'),

  // Log-like pages
  'system/log/access': () => import('@/views/system/logs/index.vue'),
  'system/log/operation': () => import('@/views/system/logs/index.vue'),
  'system/log/exception': () => import('@/views/system/logs/index.vue'),
  'system/log/audit': () => import('@/views/system/logs/index.vue'),
  'system/monitor/index': () => import('@/views/system/logs/index.vue'),
}

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
  const aliasPath = componentAliasMap[lowerPath] ?? componentAliasMap[kebabPath] ?? ''
  for (const key of [lowerPath, kebabPath, aliasPath]) {
    if (!key)
      continue
    const explicit = explicitComponentMap[key]
    if (explicit) {
      return explicit
    }
  }

  const removeIndexSuffix = (path: string) => path.replace(/\/index$/i, '')
  const candidates = new Set([
    rawPath,
    lowerPath,
    kebabPath,
    aliasPath,
    removeIndexSuffix(rawPath),
    removeIndexSuffix(lowerPath),
    removeIndexSuffix(kebabPath),
    removeIndexSuffix(aliasPath),
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
    .filter(item => !!item.path)
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

      // 叶子菜单若未匹配到本地组件，使用兜底页面避免空白路由。
      if (!route.component && (!route.children || route.children.length === 0) && fallbackView) {
        route.component = fallbackView
      }

      return route as RouteRecordRaw
    })
}
