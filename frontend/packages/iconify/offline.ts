import { icons as carbon } from '@iconify-json/carbon'
import { icons as ep } from '@iconify-json/ep'
import { icons as heroicons } from '@iconify-json/heroicons'
import { icons as logos } from '@iconify-json/logos'
import { icons as lucide } from '@iconify-json/lucide'
import { icons as mdi } from '@iconify-json/mdi'
import { icons as tabler } from '@iconify-json/tabler'
/**
 * Iconify 离线图标初始化
 * 预加载所需图标集，实现完全离线使用
 */
import { addCollection } from '@iconify/vue/offline'

const ICON_SETS = [
  { prefix: 'carbon', icons: carbon },
  { prefix: 'ep', icons: ep },
  { prefix: 'heroicons', icons: heroicons },
  { prefix: 'lucide', icons: lucide },
  { prefix: 'logos', icons: logos },
  { prefix: 'mdi', icons: mdi },
  { prefix: 'tabler', icons: tabler },
] as const

export function setupIconifyOffline() {
  for (const { icons } of ICON_SETS) {
    addCollection(icons)
  }
}

/** 图标集元数据，供 IconPicker 使用 */
export const ICON_SET_META = [
  { prefix: 'carbon', name: 'Carbon', package: 'carbon' },
  { prefix: 'ep', name: 'Element Plus', package: 'ep' },
  { prefix: 'heroicons', name: 'Heroicons', package: 'heroicons' },
  { prefix: 'lucide', name: 'Lucide', package: 'lucide' },
  { prefix: 'logos', name: 'Logos', package: 'logos' },
  { prefix: 'mdi', name: 'Material Design', package: 'mdi' },
  { prefix: 'tabler', name: 'Tabler', package: 'tabler' },
] as const

interface IconifyJSON {
  prefix?: string
  icons: Record<string, unknown>
}

const PACKAGE_LOADERS: Record<string, () => Promise<IconifyJSON>> = {
  carbon: () => import('@iconify-json/carbon').then(m => m.icons as IconifyJSON),
  ep: () => import('@iconify-json/ep').then(m => m.icons as IconifyJSON),
  heroicons: () => import('@iconify-json/heroicons').then(m => m.icons as IconifyJSON),
  lucide: () => import('@iconify-json/lucide').then(m => m.icons as IconifyJSON),
  logos: () => import('@iconify-json/logos').then(m => m.icons as IconifyJSON),
  mdi: () => import('@iconify-json/mdi').then(m => m.icons as IconifyJSON),
  tabler: () => import('@iconify-json/tabler').then(m => m.icons as IconifyJSON),
}

/** 按 prefix 懒加载图标名称列表 */
export async function loadIconNames(prefix: string): Promise<string[]> {
  const meta = ICON_SET_META.find(m => m.prefix === prefix)
  if (!meta || !PACKAGE_LOADERS[meta.package])
    return []
  const data = await PACKAGE_LOADERS[meta.package]().catch(() => ({ icons: {} }))
  return Object.keys(data.icons || {}).sort()
}
