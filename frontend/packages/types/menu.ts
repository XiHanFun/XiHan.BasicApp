// ==================== 菜单路由类型 ====================

export interface MenuMeta {
  title: string
  icon?: string
  hidden?: boolean
  keepAlive?: boolean
  affixTab?: boolean
  roles?: string[]
  permissions?: string[]
  order?: number
  badge?: string | number
  badgeType?: string
  dot?: boolean
  link?: string
}

export interface MenuRoute {
  basicId?: string
  path: string
  name: string
  component?: string
  redirect?: string
  meta: MenuMeta
  children?: MenuRoute[]
}
