import { clearLockState } from '~/composables/session-lock'
import { TABS_LIST_KEY } from '~/constants'

/**
 * 换了上下文（切换租户 / 进入平台）或身份（模仿 / 退出模仿）后整页重建：
 * 标签栏与锁屏标记属于上一上下文，先清掉；回到入口后由守卫按新会话重取权限、重装菜单路由。
 */
export function relaunchApp(): void {
  try {
    sessionStorage.removeItem(TABS_LIST_KEY)
    localStorage.removeItem(TABS_LIST_KEY)
  }
  catch {}
  clearLockState()
  window.location.assign(import.meta.env.BASE_URL || '/')
}
