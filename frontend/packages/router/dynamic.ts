import type { RouteRecordRaw } from 'vue-router'
import type { MenuRoute } from '~/types'
import { useAppContext } from '~/stores/app-context'

const fallbackView = () => import('~/views/_core/fallback/not-found.vue')

// 后端 Component 路径（PascalCase）→ 前端实际文件路径（kebab-case）的别名映射
const componentAliasMap: Record<string, string> = {
  // _core 特殊映射（packages/views/_core 不在 src/views 目录下）
  '_core/dashboard/index': '_core/dashboard/index',
  '_core/about/index': '_core/about/index',
  '_core/profile/index': '_core/profile/index',
}

// packages 自身的 _core 视图映射（使用 ~/ 引用，无需从 src 注入）
const coreComponentMap: Record<string, () => Promise<unknown>> = {
  '_core/dashboard/index': () => import('~/views/_core/dashboard/index.vue'),
  '_core/about/index': () => import('~/views/_core/about/index.vue'),
  '_core/profile/index': () => import('~/views/_core/profile/index.vue'),
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

function findFirstChildPath(children?: RouteRecordRaw[]): string | undefined {
  for (const child of children ?? []) {
    if (child.path) {
      return child.path
    }

    const descendant = findFirstChildPath(child.children)
    if (descendant) {
      return descendant
    }
  }

  return undefined
}

function hasChildPath(children: RouteRecordRaw[] | undefined, path: string): boolean {
  return (children ?? []).some(child =>
    child.path === path || hasChildPath(child.children, path),
  )
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

      if (route.children?.length) {
        const firstChildPath = findFirstChildPath(route.children)
        const redirect = item.redirect && item.redirect !== item.path
          && hasChildPath(route.children, item.redirect)
          ? item.redirect
          : firstChildPath
        if (redirect && redirect !== item.path) {
          route.redirect = redirect
        }
      }
      else if (item.redirect && item.redirect !== item.path) {
        route.redirect = item.redirect
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
