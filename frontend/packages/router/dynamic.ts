import type { RouteRecordRaw } from 'vue-router'
import type { MenuRoute } from '~/types'
import { useAppContext } from '~/stores/app-context'

const fallbackView = () => import('~/views/_core/fallback/not-found.vue')

// 后端 Component 路径（PascalCase）→ 前端实际文件路径（kebab-case）的别名映射
const componentAliasMap: Record<string, string> = {
  // _core 特殊映射（packages/views/_core 不在 src/views 目录下）
  'dashboard/index': '_core/dashboard/index',
  'core/dashboard/index': '_core/dashboard/index',
  '_core/dashboard/index': '_core/dashboard/index',
  'workbench/dashboard/index': '_core/dashboard/index',
  'about/index': '_core/about/index',
  'core/about/index': '_core/about/index',
  '_core/about/index': '_core/about/index',
  // PascalCase → kebab-case 映射
  'system/monitor/index': 'system/server/index',
  'system/cache/index': 'system/cache/index',
  'system/message/index': 'system/message/index',
  'system/constraintrule/index': 'system/constraint-rule/index',
  'system/constraint-rule/index': 'system/constraint-rule/index',
  'system/oauthapp/index': 'system/oauth-app/index',
  'system/o-auth-app/index': 'system/oauth-app/index',
  'system/usersession/index': 'system/user-session/index',
  'system/user-session/index': 'system/user-session/index',
  'system/notification/index': 'system/notification/index',
}

// packages 自身的 _core 视图映射（使用 ~/ 引用，无需从 src 注入）
const coreComponentMap: Record<string, () => Promise<unknown>> = {
  'workbench/dashboard/index': () => import('~/views/_core/dashboard/index.vue'),
  'dashboard/index': () => import('~/views/_core/dashboard/index.vue'),
  'core/dashboard/index': () => import('~/views/_core/dashboard/index.vue'),
  '_core/dashboard/index': () => import('~/views/_core/dashboard/index.vue'),
  'about/index': () => import('~/views/_core/about/index.vue'),
  'core/about/index': () => import('~/views/_core/about/index.vue'),
  '_core/about/index': () => import('~/views/_core/about/index.vue'),
}

function toKebabCase(input: string) {
  return input
    .replace(/([A-Z]+)([A-Z][a-z])/g, '$1-$2')
    .replace(/([a-z0-9])([A-Z])/g, '$1-$2')
    .replace(/_/g, '-')
    .toLowerCase()
}

function resolveView(component?: string) {
  if (!component) {
    return null
  }

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

  const removeIndexSuffix = (path: string) => path.replace(/\/index$/i, '')
  const candidates = new Set([
    lowerPath,
    kebabPath,
    aliasPath,
    removeIndexSuffix(rawPath),
    removeIndexSuffix(lowerPath),
    removeIndexSuffix(kebabPath),
    removeIndexSuffix(aliasPath),
  ])

  // 优先匹配 packages 自身的 _core 视图
  for (const candidate of candidates) {
    if (!candidate)
      continue
    const core = coreComponentMap[candidate]
    if (core)
      return core
  }

  // 然后查 src 注册的显式映射（优先级高于 glob）
  const ctx = useAppContext()
  for (const candidate of candidates) {
    if (!candidate)
      continue
    const explicit = ctx.explicitComponentMap[candidate]
    if (explicit)
      return explicit
  }

  // 最后查 src 注册的 viewModules glob
  const keys = Array.from(candidates).flatMap(path => [
    `/src/views/${path}.vue`,
    `/src/views/${path}/index.vue`,
  ])

  for (const key of keys) {
    const matched = ctx.viewModules[key]
    if (matched)
      return matched
  }

  return null
}

export function mapMenuToRoutes(menuRoutes: MenuRoute[]): RouteRecordRaw[] {
  return menuRoutes
    .filter(item => !!item.path)
    .map((item) => {
      const component = resolveView(item.component)
      const route = {
        path: item.path,
        name: item.name,
        meta: item.meta as unknown as Record<string, unknown>,
      } as unknown as RouteRecordRaw

      if (item.children?.length) {
        route.children = mapMenuToRoutes(item.children)
      }

      // 防御：redirect 指向自身会导致无限循环，自动改为第一个子路由
      if (item.redirect && item.redirect !== item.path) {
        route.redirect = item.redirect
      }
      else if (item.redirect && item.redirect === item.path && route.children?.length) {
        route.redirect = route.children[0]!.path
      }

      if (component) {
        route.component = component
      }

      if (!route.component && (!route.children || route.children.length === 0) && fallbackView) {
        route.component = fallbackView
      }

      return route
    })
}
