// ==================== 标签页类型 ====================

export interface TabItem {
  key: string
  title: string
  path: string
  pinned?: boolean
  closable: boolean
  meta?: {
    icon?: string
    [key: string]: unknown
  }
}

// ==================== 收藏夹类型 ====================

/**
 * 收藏夹项（收藏的常用菜单）。
 * 抽象、可序列化为 JSON，按 (用户) 维度通过 PagePreference 跨端同步。
 */
export interface FavoriteItem {
  /** 唯一键（等于 path） */
  key: string
  /** 标题（可能是 i18n key，如 'menu.workspace'，渲染时翻译） */
  title: string
  /** 路由路径（点击后导航） */
  path: string
  /** 图标（iconify 名，如 'lucide:gauge'，可缺省） */
  icon?: string
}
