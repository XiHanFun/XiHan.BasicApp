// ==================== 用户设置（全场景跨端同步） ====================
// 与后端 XiHan.BasicApp.Saas 的 UserSettingScene 枚举对齐：
// 一条用户设置记录由 (scene, settingKey) 唯一定位，覆盖偏好设置 / 页面设置等全部场景。

/**
 * 用户设置场景（值与后端 UserSettingScene 枚举一致）。
 */
export const UserSettingScene = {
  /** 偏好/全局：应用偏好（主题/外观/布局/组件/快捷键）、收藏夹等全局用户级设置，按 settingKey 区分 */
  Preference: 0,
  /** 页面设置：列表页列设置/视图/搜索等（settingKey 为 pageCode） */
  Page: 1,
} as const

/** 用户设置场景取值类型 */
export type UserSettingScene = typeof UserSettingScene[keyof typeof UserSettingScene]

/** 应用偏好设置键（主题/外观/布局/组件/快捷键，偏好场景下） */
export const PREFERENCE_SETTING_KEY = 'global'

/** 收藏夹设置键（偏好场景下） */
export const FAVORITES_SETTING_KEY = 'favorites'
