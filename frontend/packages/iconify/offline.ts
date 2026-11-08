/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

/**
 * Iconify 离线图标集
 * 启动时预载常用的四个图标集，其余图标集在首次渲染到它的图标时（或图标选择器切到它时）再装
 */
import { addCollection } from '@iconify/vue/offline'
import { reactive } from 'vue'

type IconifyJSON = Parameters<typeof addCollection>[0]

/** 图标集元数据，供 IconPicker 使用 */
export const ICON_SET_META = [
  { prefix: 'carbon', name: 'Carbon', package: 'carbon' },
  { prefix: 'ep', name: 'Element Plus', package: 'ep' },
  { prefix: 'heroicons', name: 'Heroicons', package: 'heroicons' },
  { prefix: 'lucide', name: 'Lucide', package: 'lucide' },
  { prefix: 'mdi', name: 'Material Design Icons', package: 'mdi' },
  { prefix: 'simple-icons', name: 'Simple Icons', package: 'simple-icons' },
  { prefix: 'tabler', name: 'Tabler', package: 'tabler' },
] as const

type IconPackageName = typeof ICON_SET_META[number]['package']

const PACKAGE_LOADERS: Record<IconPackageName, () => Promise<IconifyJSON>> = {
  'carbon': () => import('@iconify-json/carbon').then(m => m.icons as IconifyJSON),
  'ep': () => import('@iconify-json/ep').then(m => m.icons as IconifyJSON),
  'heroicons': () => import('@iconify-json/heroicons').then(m => m.icons as IconifyJSON),
  'lucide': () => import('@iconify-json/lucide').then(m => m.icons as IconifyJSON),
  'mdi': () => import('@iconify-json/mdi').then(m => m.icons as IconifyJSON),
  'simple-icons': () => import('@iconify-json/simple-icons').then(m => m.icons as IconifyJSON),
  'tabler': () => import('@iconify-json/tabler').then(m => m.icons as IconifyJSON),
}

/**
 * 启动预载：lucide（通用）、tabler（部分页面）、mdi（品牌等图标）、simple-icons（登录页与绑定页的第三方登录 logo）
 * 这几个先于挂载装好，首屏图标不必等加载后再重挂
 */
const PRELOAD_ICON_PACKAGES: IconPackageName[] = [
  'lucide',
  'tabler',
  'mdi',
  'simple-icons',
]

/** 已装好的图标集；Icon 读它，图标集装好后对应的图标重新挂载 */
const loadedSets = reactive(new Set<string>())
const loadingSets = new Map<string, Promise<IconifyJSON | null>>()

function findPackage(prefix: string): IconPackageName | undefined {
  return ICON_SET_META.find(meta => meta.prefix === prefix)?.package
}

/** 是否是本地收录的图标集 */
export function hasIconSet(prefix: string): boolean {
  return findPackage(prefix) !== undefined
}

/** 图标集是否已装好 */
export function isIconSetLoaded(prefix: string): boolean {
  return loadedSets.has(prefix)
}

/**
 * 按 prefix 装入图标集，同一图标集只装一次
 * 未收录的 prefix 返回 null；加载失败也返回 null，且不记住失败，下次调用再试
 */
export function loadIconSet(prefix: string): Promise<IconifyJSON | null> {
  const packageName = findPackage(prefix)
  if (!packageName)
    return Promise.resolve(null)
  let task = loadingSets.get(prefix)
  if (!task) {
    task = PACKAGE_LOADERS[packageName]()
      .then((data) => {
        addCollection(data)
        loadedSets.add(prefix)
        return data
      })
      .catch(() => {
        loadingSets.delete(prefix)
        return null
      })
    loadingSets.set(prefix, task)
  }
  return task
}

export async function setupIconifyOffline() {
  await Promise.all(PRELOAD_ICON_PACKAGES.map(loadIconSet))
}

/** 按 prefix 取图标名称列表（顺带装入该图标集） */
export async function loadIconNames(prefix: string): Promise<string[]> {
  const data = await loadIconSet(prefix)
  if (!data)
    return []
  return Object.keys(data.icons || {}).sort()
}
