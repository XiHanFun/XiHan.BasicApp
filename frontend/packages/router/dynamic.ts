import type { RouteRecordRaw } from 'vue-router'
import type { MenuRoute } from '~/types'

const viewModules = import.meta.glob('@/views/**/*.vue')
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

// 无法通过 import.meta.glob 解析的组件，直接显式映射
const explicitComponentMap: Record<string, () => Promise<unknown>> = {
  // Workbench（仪表板在 packages/_core 中，不在 src/views 的 glob 范围内）
  'workbench/dashboard/index': () => import('~/views/_core/dashboard/index.vue'),
  'dashboard/index': () => import('~/views/_core/dashboard/index.vue'),
  'core/dashboard/index': () => import('~/views/_core/dashboard/index.vue'),
  '_core/dashboard/index': () => import('~/views/_core/dashboard/index.vue'),
  'workbench/inbox/index': () => import('@/views/workbench/inbox/index.vue'),
  'workbench/inbox': () => import('@/views/workbench/inbox/index.vue'),
  // About（在 packages/_core 中）
  'about/index': () => import('~/views/_core/about/index.vue'),
  'core/about/index': () => import('~/views/_core/about/index.vue'),
  '_core/about/index': () => import('~/views/_core/about/index.vue'),
  // 日志（无 /index 后缀的组件路径需要显式映射）
  'system/log/access': () => import('@/views/system/log/access/index.vue'),
  'system/log/operation': () => import('@/views/system/log/operation/index.vue'),
  'system/log/exception': () => import('@/views/system/log/exception/index.vue'),
  'system/log/audit': () => import('@/views/system/log/audit/index.vue'),
  'system/log/login': () => import('@/views/system/log/login/index.vue'),
  'system/log/task': () => import('@/views/system/log/task/index.vue'),
  'system/log/api': () => import('@/views/system/log/api/index.vue'),
  // PascalCase → kebab-case 的显式映射
  'system/monitor/index': () => import('@/views/system/server/index.vue'),
  'system/cache/index': () => import('@/views/system/cache/index.vue'),
  'system/message/index': () => import('@/views/system/message/index.vue'),
  'system/constraintrule/index': () => import('@/views/system/constraint-rule/index.vue'),
  'system/constraint-rule/index': () => import('@/views/system/constraint-rule/index.vue'),
  'system/oauthapp/index': () => import('@/views/system/oauth-app/index.vue'),
  'system/o-auth-app/index': () => import('@/views/system/oauth-app/index.vue'),
  'system/usersession/index': () => import('@/views/system/user-session/index.vue'),
  'system/user-session/index': () => import('@/views/system/user-session/index.vue'),
  'system/notification/index': () => import('@/views/system/notification/index.vue'),
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
  for (const key of [lowerPath, kebabPath, aliasPath]) {
    if (!key) {
      continue
    }
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

  for (const candidate of candidates) {
    if (!candidate) {
      continue
    }
    const explicit = explicitComponentMap[candidate]
    if (explicit) {
      return explicit
    }
  }

  const keys = Array.from(candidates).flatMap(path => [
    `/src/views/${path}.vue`,
    `/src/views/${path}/index.vue`,
  ])

  for (const key of keys) {
    const matched = viewModules[key]
    if (matched) {
      return matched
    }
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
