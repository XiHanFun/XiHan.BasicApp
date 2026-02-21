// ==================== 路由路径常量 ====================

export const LOGIN_PATH = '/login'
export const HOME_PATH = '/dashboard'
export const NOT_FOUND_PATH = '/404'
export const FORBIDDEN_PATH = '/403'

// ==================== 存储 Key 常量 ====================

export const STORAGE_PREFIX = 'xihan_'

export const TOKEN_KEY = `${STORAGE_PREFIX}access_token`
export const REFRESH_TOKEN_KEY = `${STORAGE_PREFIX}refresh_token`
export const USER_INFO_KEY = `${STORAGE_PREFIX}user_info`
export const LOCALE_KEY = `${STORAGE_PREFIX}locale`
export const THEME_MODE_KEY = `${STORAGE_PREFIX}theme_mode`
export const SIDEBAR_COLLAPSED_KEY = `${STORAGE_PREFIX}sidebar_collapsed`
export const TAGS_BAR_KEY = `${STORAGE_PREFIX}tags_bar`
export const TABS_LIST_KEY = `${STORAGE_PREFIX}tabs_list`
export const LAYOUT_MODE_KEY = `${STORAGE_PREFIX}layout_mode`
export const THEME_COLOR_KEY = `${STORAGE_PREFIX}theme_color`
export const BREADCRUMB_ENABLED_KEY = `${STORAGE_PREFIX}breadcrumb_enabled`
export const SEARCH_ENABLED_KEY = `${STORAGE_PREFIX}search_enabled`
export const THEME_ANIMATION_ENABLED_KEY = `${STORAGE_PREFIX}theme_animation_enabled`

// ==================== 主题模式 ====================

export const THEME_DARK = 'dark'
export const THEME_LIGHT = 'light'
export const THEME_AUTO = 'auto'

// ==================== 默认配置 ====================

export const DEFAULT_LOCALE = 'zh-CN'
export const DEFAULT_THEME = THEME_LIGHT
export const DEFAULT_PAGE_SIZE = 20
export const TOKEN_EXPIRES_IN = 7 * 24 * 60 * 60 * 1000
export const DEFAULT_THEME_COLOR = '#18a058'
export const DEFAULT_LAYOUT_MODE = 'side'
export const LAYOUT_MODE_OPTIONS = [
  { label: '侧边布局', value: 'side' },
  { label: '混合布局', value: 'mix' },
  { label: '顶部布局', value: 'top' },
]

// ==================== HTTP 状态码 ====================

export const HTTP_STATUS = {
  OK: 200,
  CREATED: 201,
  NO_CONTENT: 204,
  BAD_REQUEST: 400,
  UNAUTHORIZED: 401,
  FORBIDDEN: 403,
  NOT_FOUND: 404,
  INTERNAL_SERVER_ERROR: 500,
} as const

// ==================== 业务状态码 ====================

export const BIZ_CODE = {
  SUCCESS: 200,
  UNAUTHORIZED: 401,
  FORBIDDEN: 403,
  TOKEN_EXPIRED: 4001,
  REFRESH_TOKEN_EXPIRED: 4002,
} as const

// ==================== 性别 ====================

export const GENDER_OPTIONS = [
  { label: '未知', value: 0 },
  { label: '男', value: 1 },
  { label: '女', value: 2 },
]

// ==================== 状态 ====================

export const STATUS_OPTIONS = [
  { label: '启用', value: 1 },
  { label: '禁用', value: 0 },
]

// ==================== 菜单类型 ====================

export const MENU_TYPE = {
  DIR: 0,
  MENU: 1,
  BUTTON: 2,
} as const
