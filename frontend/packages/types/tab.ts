// ==================== 标签页类型 ====================

export interface TabItem {
  key: string
  /** 路由名（= KeepAlive 缓存名，须与组件被强制设置的 name 一致，见 router/dynamic.ts） */
  name?: string
  title: string
  path: string
  pinned?: boolean
  closable: boolean
  /** 该标签是否参与 keep-alive 缓存（来自菜单 meta.keepAlive） */
  keepAlive?: boolean
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
