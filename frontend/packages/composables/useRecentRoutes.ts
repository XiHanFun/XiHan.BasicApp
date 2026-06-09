import { ref } from 'vue'
import { RECENT_ROUTES_KEY } from '~/constants'
import { LocalStorage } from '~/utils'

/**
 * 最近访问路由（设备本地，命令面板 ⌘K 的「最近」分组用）。
 * 按访问倒序、按 path 去重、容量上限；仅本地持久化，不上行后端。
 */
export interface RecentRoute {
  /** 完整路径（含 query，点击可还原） */
  path: string
  /** 标题（可能是 i18n key，渲染时翻译） */
  title: string
  /** 图标（iconify 名，可缺省） */
  icon?: string
}

const CAP = 12
const recent = ref<RecentRoute[]>(LocalStorage.get<RecentRoute[]>(RECENT_ROUTES_KEY) ?? [])

function recordRecent(item: RecentRoute): void {
  if (!item.path || !item.title) {
    return
  }
  const next = [item, ...recent.value.filter(r => r.path !== item.path)].slice(0, CAP)
  recent.value = next
  LocalStorage.set(RECENT_ROUTES_KEY, next)
}

function clearRecent(): void {
  recent.value = []
  LocalStorage.set(RECENT_ROUTES_KEY, [])
}

export function useRecentRoutes() {
  return { recent, recordRecent, clearRecent }
}
