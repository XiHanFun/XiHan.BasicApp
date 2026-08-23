import type { VNodeChild } from 'vue'

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

// ==================== 菜单/下拉的展示模型 ====================

/**
 * 菜单条目（侧栏、顶栏、混合布局共用）。
 *
 * 它是「路由 → 可渲染菜单」这一步的产物，由 buildMenuOptionsFromRoutes 生成。
 * label 与 icon 允许是渲染函数：菜单标签要带角标、外链图标这类附加物，纯字符串装不下。
 */
export interface AppMenuOption {
  /** 条目身份：按 keyBy 取路由名或全路径，同时是选中判据 */
  key: string
  label: string | (() => VNodeChild)
  icon?: () => VNodeChild
  disabled?: boolean
  children?: AppMenuOption[]
}

/** 下拉菜单条目（面包屑同级跳转、用户菜单、标签页右键菜单共用） */
export interface AppDropdownOption {
  key: string
  label?: string | (() => VNodeChild)
  icon?: () => VNodeChild
  disabled?: boolean
  /** 本条之前画一条分隔线 */
  divider?: boolean
  /** 独立分隔条目：整条只是一根分隔线，没有文案与去处 */
  type?: 'divider'
  /**
   * 二级子菜单条目：给了就渲染成子菜单触发器，本条自身不可选。
   * 只有摆部件的那条路认它（见 TabbarContextMenu.vue）；交 collection 代铺的菜单
   * （XDropdown 等）会丢掉子层，组件库的 MenuNode 没有 children。
   */
  children?: AppDropdownOption[]
}
