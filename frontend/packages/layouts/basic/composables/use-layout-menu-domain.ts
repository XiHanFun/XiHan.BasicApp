import type { MenuOption } from 'naive-ui'
import type { MenuRoute } from '~/types'
import {
  computed,
} from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAccessStore } from '~/stores'
import type { LayoutRouteMeta, LayoutRouteRecord } from '../contracts'

interface BuildMenuOptionsConfig {
  keyBy: 'name' | 'path'
  translate: (title: string, fallback: string) => string
  iconRenderer?: (icon: string) => MenuOption['icon']
}

type RouteRecordName = LayoutRouteRecord['name']

function toRouteNameKey(name: RouteRecordName) {
  return typeof name === 'string' || typeof name === 'number' ? String(name) : undefined
}

function toLayoutMeta(record: LayoutRouteRecord): LayoutRouteMeta {
  return (record.meta ?? {}) as LayoutRouteMeta
}

function resolveFullPath(path: string, parentPath = '') {
  if (!path) {
    return parentPath || '/'
  }
  if (path.startsWith('/')) {
    return path
  }
  return `${parentPath.replace(/\/$/, '')}/${path}`.replace(/\/{2,}/g, '/')
}

function normalizeMenuRoutes(menuRoutes: MenuRoute[]): LayoutRouteRecord[] {
  return menuRoutes.map((route) => {
    const normalized = {
      path: route.path,
      name: route.name,
      meta: route.meta as unknown as LayoutRouteRecord['meta'],
    } as LayoutRouteRecord
    if (route.redirect) {
      normalized.redirect = route.redirect
    }
    if (route.children?.length) {
      normalized.children = normalizeMenuRoutes(route.children)
    }
    return normalized
  })
}

function routeTreeContainsMatched(
  currentPath: string,
  node: LayoutRouteRecord,
  matchedNames: Set<string>,
  parentPath = '',
): boolean {
  const selfName = toRouteNameKey(node.name)
  if (selfName && matchedNames.has(selfName)) {
    return true
  }

  const fullPath = resolveFullPath(node.path, parentPath)
  if (fullPath && (currentPath === fullPath || currentPath.startsWith(`${fullPath}/`))) {
    return true
  }

  const children = node.children ?? []
  return children.some(child => routeTreeContainsMatched(currentPath, child, matchedNames, fullPath))
}

function buildMenuOptionsFromRoutes(
  routeList: LayoutRouteRecord[],
  config: BuildMenuOptionsConfig,
  parentPath = '',
): MenuOption[] {
  const options: MenuOption[] = []

  for (const item of routeList) {
    const meta = toLayoutMeta(item)
    const fullPath = resolveFullPath(item.path, parentPath)
    const children = item.children ?? []
    const childOptions = children.length
      ? buildMenuOptionsFromRoutes(children, config, fullPath)
      : undefined

    // vben 风格：隐藏父节点不直接渲染，提升其可见子节点，避免 mixed 模式二列空白。
    if (meta.hidden) {
      if (childOptions?.length) {
        options.push(...childOptions)
      }
      continue
    }

    const key = config.keyBy === 'path'
      ? fullPath
      : (
          toRouteNameKey(item.name)
          ?? (childOptions?.[0]?.key ? String(childOptions[0].key) : fullPath)
        )

    if (!key) {
      continue
    }

    const fallback = toRouteNameKey(item.name) ?? fullPath
    const label = meta.title ? config.translate(meta.title, fallback) : fallback
    const icon = meta.icon ? config.iconRenderer?.(meta.icon) : undefined

    options.push({
      key,
      label,
      icon,
      children: childOptions?.length ? childOptions : undefined,
    } as MenuOption)
  }

  return options
}

export function useLayoutMenuDomain() {
  const route = useRoute()
  const router = useRouter()
  const accessStore = useAccessStore()

  const staticRootChildren = computed<LayoutRouteRecord[]>(() => {
    return (router.options.routes.find(item => item.path === '/')?.children ?? []) as LayoutRouteRecord[]
  })

  const baseMenuSource = computed<LayoutRouteRecord[]>(() => {
    return accessStore.accessRoutes.length
      ? normalizeMenuRoutes(accessStore.accessRoutes)
      : staticRootChildren.value
  })

  const visibleRootRoutes = computed(() => {
    return baseMenuSource.value.filter(item => !toLayoutMeta(item).hidden)
  })

  function findMatchedRouteNameKey(candidates: LayoutRouteRecord[]) {
    for (const matchedRecord of route.matched) {
      const matchedName = toRouteNameKey(matchedRecord.name as RouteRecordName)
      if (matchedName && candidates.some(item => toRouteNameKey(item.name) === matchedName)) {
        return matchedName
      }
    }
    return undefined
  }

  function findMatchedRoutePath(candidates: LayoutRouteRecord[], parentPath = '') {
    const matchedNames = new Set(
      route.matched
        .map(item => toRouteNameKey(item.name as RouteRecordName))
        .filter((item): item is string => Boolean(item)),
    )

    for (const item of candidates) {
      if (routeTreeContainsMatched(route.path, item, matchedNames, parentPath)) {
        return resolveFullPath(item.path, parentPath)
      }
    }
    return undefined
  }

  const activeRootKey = computed<string>(() => {
    const matchedNames = new Set(
      route.matched
        .map(item => toRouteNameKey(item.name as RouteRecordName))
        .filter((item): item is string => Boolean(item)),
    )
    const nestedMatchedRoot = visibleRootRoutes.value.find(item =>
      routeTreeContainsMatched(route.path, item, matchedNames),
    )
    return findMatchedRouteNameKey(visibleRootRoutes.value)
      ?? toRouteNameKey(nestedMatchedRoot?.name)
      ?? toRouteNameKey(visibleRootRoutes.value[0]?.name)
      ?? ''
  })

  const activeRootRoute = computed(() => {
    return visibleRootRoutes.value.find(item => toRouteNameKey(item.name) === activeRootKey.value)
  })

  return {
    route,
    router,
    baseMenuSource,
    visibleRootRoutes,
    activeRootKey,
    activeRootRoute,
    toRouteNameKey,
    toLayoutMeta,
    resolveFullPath,
    normalizeMenuRoutes,
    buildMenuOptionsFromRoutes,
    findMatchedRouteNameKey,
    findMatchedRoutePath,
  }
}
