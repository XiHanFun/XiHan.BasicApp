// 与 useLockScreen 分开：这里是与 Vue 无关的纯锁定状态读写，
// 供请求层（423 拦截）和入口（main.ts）调用；useLockScreen 才是绑定组件生命周期的那一层。
import { LOCK_STATE_KEY } from '~/constants'

/**
 * 会话锁定原因：**锁定不等于锁屏**，锁屏只是其中一种。
 * 与后端 `SessionLockReasons` 对齐；将来若加入风控挂起、强制改密等，
 * 应各自引导对应的解锁方式，而不是一律弹锁屏口令框。
 */
export const SESSION_LOCK_REASON_SCREEN = 'ScreenLock'

/**
 * 本标签页内的锁定态变更事件。
 *
 * `storage` 事件只在**其他**标签页触发，所以同一个标签页里改动 localStorage 后
 * UI 不会自己跟着变——强制登出清锁定标记正是这种情况，不广播就会留下一个刷新也去不掉的遮罩。
 */
export const SESSION_LOCK_CHANGED_EVENT = 'xihan:session-lock-changed'

function notifyLockChanged() {
  window.dispatchEvent(new CustomEvent(SESSION_LOCK_CHANGED_EVENT))
}

/** 当前是否处于锁定 UI 态 */
export function isLockedState() {
  return localStorage.getItem(LOCK_STATE_KEY) === '1'
}

/** 清除锁定 UI 标记（登出/强制登出时调用） */
export function clearLockState() {
  localStorage.removeItem(LOCK_STATE_KEY)
  notifyLockChanged()
}

/**
 * 被服务端 423 拦截时由请求层调用：把本标签页拉回锁定态。
 *
 * 只有原因是「锁屏」时才拉起锁屏遮罩——其它锁定原因（风控挂起等）不该弹一个
 * 根本打不开的口令框，那会把用户困死。原因未知时同样不接管，交由调用方的错误提示处理。
 *
 * 注意：这个 localStorage 标记只是 UI 状态，**不是安全边界**。真正的强制在服务端：
 * 锁定位存在 `SysUserSession.IsLocked`，中间件会以 423 拒绝该会话的一切请求。
 */
export function markLockedFromServer(reason?: string | null) {
  if (reason && reason !== SESSION_LOCK_REASON_SCREEN) {
    return
  }
  localStorage.setItem(LOCK_STATE_KEY, '1')
  notifyLockChanged()
}
