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
export const BRAND_TITLE_KEY = `${STORAGE_PREFIX}brand_title`
export const BRAND_LOGO_KEY = `${STORAGE_PREFIX}brand_logo`
export const BREADCRUMB_ENABLED_KEY = `${STORAGE_PREFIX}breadcrumb_enabled`
export const SEARCH_ENABLED_KEY = `${STORAGE_PREFIX}search_enabled`
export const THEME_ANIMATION_ENABLED_KEY = `${STORAGE_PREFIX}theme_animation_enabled`
export const GRAYSCALE_ENABLED_KEY = `${STORAGE_PREFIX}grayscale_enabled`
export const COLOR_WEAKNESS_ENABLED_KEY = `${STORAGE_PREFIX}color_weakness_enabled`
export const WATERMARK_ENABLED_KEY = `${STORAGE_PREFIX}watermark_enabled`
export const WATERMARK_TEXT_KEY = `${STORAGE_PREFIX}watermark_text`
export const UI_RADIUS_KEY = `${STORAGE_PREFIX}ui_radius`
export const FONT_SIZE_KEY = `${STORAGE_PREFIX}font_size`
export const DYNAMIC_TITLE_KEY = `${STORAGE_PREFIX}dynamic_title`
export const CHECK_UPDATES_KEY = `${STORAGE_PREFIX}check_updates`
export const BREADCRUMB_SHOW_HOME_KEY = `${STORAGE_PREFIX}breadcrumb_show_home`
export const BREADCRUMB_SHOW_ICON_KEY = `${STORAGE_PREFIX}breadcrumb_show_icon`
export const BREADCRUMB_HIDE_ONLY_ONE_KEY = `${STORAGE_PREFIX}breadcrumb_hide_only_one`
export const BREADCRUMB_STYLE_KEY = `${STORAGE_PREFIX}breadcrumb_style`
export const TABBAR_PERSIST_KEY = `${STORAGE_PREFIX}tabbar_persist`
export const TABBAR_VISIT_HISTORY_KEY = `${STORAGE_PREFIX}tabbar_visit_history`
export const TABBAR_DRAGGABLE_KEY = `${STORAGE_PREFIX}tabbar_draggable`
export const TABBAR_SHOW_MORE_KEY = `${STORAGE_PREFIX}tabbar_show_more`
export const TABBAR_SHOW_MAXIMIZE_KEY = `${STORAGE_PREFIX}tabbar_show_maximize`
export const TABBAR_MAX_COUNT_KEY = `${STORAGE_PREFIX}tabbar_max_count`
export const HEADER_MENU_ALIGN_KEY = `${STORAGE_PREFIX}header_menu_align`
export const HEADER_MODE_KEY = `${STORAGE_PREFIX}header_mode`
export const NAV_STYLE_KEY = `${STORAGE_PREFIX}navigation_style`
export const NAV_SPLIT_KEY = `${STORAGE_PREFIX}navigation_split`
export const SIDEBAR_WIDTH_KEY = `${STORAGE_PREFIX}sidebar_width`
export const SIDEBAR_SHOW_KEY = `${STORAGE_PREFIX}sidebar_show`
export const SIDEBAR_COLLAPSE_BUTTON_KEY = `${STORAGE_PREFIX}sidebar_collapse_button`
export const SIDEBAR_FIXED_BUTTON_KEY = `${STORAGE_PREFIX}sidebar_fixed_button`
export const SIDEBAR_EXPAND_HOVER_KEY = `${STORAGE_PREFIX}sidebar_expand_hover`
export const SIDEBAR_AUTO_ACTIVATE_CHILD_KEY = `${STORAGE_PREFIX}sidebar_auto_activate_child`
export const SIDEBAR_COLLAPSED_SHOW_TITLE_KEY = `${STORAGE_PREFIX}sidebar_collapsed_show_title`
export const CONTENT_COMPACT_KEY = `${STORAGE_PREFIX}content_compact`
export const CONTENT_MAX_WIDTH_KEY = `${STORAGE_PREFIX}content_max_width`
export const WIDGET_THEME_TOGGLE_KEY = `${STORAGE_PREFIX}widget_theme_toggle`
export const WIDGET_LANGUAGE_TOGGLE_KEY = `${STORAGE_PREFIX}widget_language_toggle`
export const WIDGET_FULLSCREEN_KEY = `${STORAGE_PREFIX}widget_fullscreen`
export const WIDGET_NOTIFICATION_KEY = `${STORAGE_PREFIX}widget_notification`
export const WIDGET_LOCKSCREEN_KEY = `${STORAGE_PREFIX}widget_lockscreen`
export const WIDGET_SIDEBAR_TOGGLE_KEY = `${STORAGE_PREFIX}widget_sidebar_toggle`
export const WIDGET_REFRESH_KEY = `${STORAGE_PREFIX}widget_refresh`
export const FOOTER_ENABLE_KEY = `${STORAGE_PREFIX}footer_enable`
export const FOOTER_FIXED_KEY = `${STORAGE_PREFIX}footer_fixed`
export const COPYRIGHT_ENABLE_KEY = `${STORAGE_PREFIX}copyright_enable`
export const COPYRIGHT_COMPANY_KEY = `${STORAGE_PREFIX}copyright_company`
export const COPYRIGHT_SITE_KEY = `${STORAGE_PREFIX}copyright_site`
export const SHORTCUT_ENABLE_KEY = `${STORAGE_PREFIX}shortcut_enable`
export const SHORTCUT_SEARCH_KEY = `${STORAGE_PREFIX}shortcut_search`
export const SHORTCUT_LOGOUT_KEY = `${STORAGE_PREFIX}shortcut_logout`
export const SHORTCUT_LOCK_KEY = `${STORAGE_PREFIX}shortcut_lock`
export const TRANSITION_ENABLE_KEY = `${STORAGE_PREFIX}transition_enable`
export const TRANSITION_NAME_KEY = `${STORAGE_PREFIX}transition_name`

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
export const DEFAULT_UI_RADIUS = 0.5
export const DEFAULT_FONT_SIZE = 14
export const LAYOUT_MODE_OPTIONS = [
  { label: '垂直', value: 'side' },
  { label: '双列菜单', value: 'side-mixed' },
  { label: '水平', value: 'top' },
  { label: '侧边导航', value: 'header-sidebar' },
  { label: '混合垂直', value: 'mix' },
  { label: '混合双列', value: 'header-mix' },
  { label: '内容全屏', value: 'full' },
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
