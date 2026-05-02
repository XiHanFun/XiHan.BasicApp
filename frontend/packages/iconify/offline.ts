/**
 * Iconify 离线图标初始化
 * 默认只预加载运行期高频图标集，其余图标集由 IconPicker 按需加载。
 */
import { addCollection } from '@iconify/vue/offline'

type IconifyJSON = Parameters<typeof addCollection>[0]

/** 图标集元数据，供 IconPicker 使用 */
export const ICON_SET_META = [
  { prefix: 'carbon', name: 'Carbon', package: 'carbon' },
  { prefix: 'ep', name: 'Element Plus', package: 'ep' },
  { prefix: 'heroicons', name: 'Heroicons', package: 'heroicons' },
  { prefix: 'lucide', name: 'Lucide', package: 'lucide' },
] as const

type IconPackageName = typeof ICON_SET_META[number]['package']

const PACKAGE_LOADERS: Record<IconPackageName, () => Promise<IconifyJSON>> = {
  carbon: () => import('@iconify-json/carbon').then(m => m.icons as IconifyJSON),
  ep: () => import('@iconify-json/ep').then(m => m.icons as IconifyJSON),
  heroicons: () => import('@iconify-json/heroicons').then(m => m.icons as IconifyJSON),
  lucide: () => import('@iconify-json/lucide').then(m => m.icons as IconifyJSON),
}

const PRELOAD_ICON_PACKAGES: IconPackageName[] = ['lucide']

export async function setupIconifyOffline() {
  await Promise.all(PRELOAD_ICON_PACKAGES.map(async (packageName) => {
    const icons = await PACKAGE_LOADERS[packageName]().catch(() => null)
    if (icons) {
      addCollection(icons)
    }
  }))
}

/** 按 prefix 懒加载图标名称列表 */
export async function loadIconNames(prefix: string): Promise<string[]> {
  const meta = ICON_SET_META.find(m => m.prefix === prefix)
  const loader = meta ? PACKAGE_LOADERS[meta.package] : undefined
  if (!loader)
    return []
  const data = await loader().catch(() => null)
  if (!data)
    return []
  addCollection(data)
  return Object.keys(data.icons || {}).sort()
}
