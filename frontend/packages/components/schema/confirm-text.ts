/** 页面侧 useI18n 的 t；确认文案在点击时才生成，拿页面的 t 即可跟随当前语言 */
type Translate = (key: string, params?: Record<string, unknown>) => string

/** 删除确认：写明删的是哪一条，并提示不可恢复 */
export function deleteConfirmText(t: Translate, name: string) {
  return t('common.messages.confirm_delete', { name })
}

/** 启停确认：按当前状态说清这次是启用还是停用哪一条 */
export function statusConfirmText(t: Translate, enabled: boolean, name: string) {
  return t(enabled ? 'common.messages.confirm_disable' : 'common.messages.confirm_enable', { name })
}

/** 其它不可撤回或有外部副作用的操作：写明对哪一条执行什么 */
export function actionConfirmText(t: Translate, action: string, name: string) {
  return t('common.messages.confirm_action', { action, name })
}
