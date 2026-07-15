import { onMounted, onUnmounted, ref, watch } from 'vue'
import { LOCK_STATE_KEY } from '~/constants'
import { useAppContext, useAppStore, useAuthStore, useLayoutBridgeStore } from '~/stores'
import { clearLockState } from './session-lock'

export type LockMode = 'off' | 'setting' | 'locked'

/**
 * 锁屏：**服务端强制**。
 *
 * 锁屏位存在 `SysUserSession.IsLocked`，服务端中间件会以 423 拒绝该会话的一切请求
 * （仅放行解锁/登出/刷新）。所以改 DOM、开新标签页、乃至绕过前端直接 curl 调 API，都取不到数据。
 *
 * 客户端这份 localStorage 标记**只是 UI 状态**，不是安全边界：它的作用是让本标签页
 * 立刻显示遮罩、并让其它标签页跟着同步，免得每个标签页都要先撞一次 423 才知道该锁。
 * 即便有人手动删掉它，服务端照样 423。
 */
const MAX_LOCK_ATTEMPTS = 5

export function useLockScreen() {
  const appStore = useAppStore()
  const authStore = useAuthStore()
  const layoutBridgeStore = useLayoutBridgeStore()
  const ctx = useAppContext()

  const lockMode = ref<LockMode>('off')
  const lockAttempts = ref(0)
  const lockPwdNew = ref('')
  const lockPwdConfirm = ref('')
  const lockPwdError = ref('')
  const lockLoading = ref(false)
  const unlockPwd = ref('')
  const unlockError = ref('')
  const unlockLoading = ref(false)
  // 服务端强制锁屏必须有口令，因此锁屏态下永远需要输入密码
  const hasLockPwd = ref(true)

  function doLock() {
    lockMode.value = 'setting'
    lockPwdNew.value = ''
    lockPwdConfirm.value = ''
    lockPwdError.value = ''
  }

  /** 设置锁屏口令并请求服务端置位 */
  async function confirmLock() {
    if (!lockPwdNew.value) {
      // 空口令锁屏已被移除：服务端强制模式下，无口令的锁屏毫无意义
      // （任何持有该 token 的人调一次解锁接口就打开了）
      lockPwdError.value = '请输入锁屏密码'
      return
    }
    if (lockPwdNew.value !== lockPwdConfirm.value) {
      lockPwdError.value = '两次输入不一致'
      return
    }

    lockLoading.value = true
    try {
      await ctx.apis.lockSessionApi({ password: lockPwdNew.value })
      localStorage.setItem(LOCK_STATE_KEY, '1')
      lockMode.value = 'locked'
      lockPwdNew.value = ''
      lockPwdConfirm.value = ''
      lockPwdError.value = ''
      unlockPwd.value = ''
      unlockError.value = ''
      lockAttempts.value = 0
    }
    catch (error) {
      lockPwdError.value = (error as Error)?.message || '锁屏失败'
    }
    finally {
      lockLoading.value = false
    }
  }

  /** 解锁：口令由服务端校验（PBKDF2），本地不持有任何哈希 */
  async function doUnlock() {
    if (!unlockPwd.value) {
      unlockError.value = '请输入锁屏密码'
      return
    }

    unlockLoading.value = true
    try {
      await ctx.apis.unlockSessionApi({ password: unlockPwd.value })
      releaseLock()
    }
    catch (error) {
      lockAttempts.value++
      unlockPwd.value = ''
      // 连续失败达上限时服务端已吊销会话，此处直接登出，避免停留在一个已失效的锁屏页
      if (lockAttempts.value >= MAX_LOCK_ATTEMPTS) {
        releaseLock()
        void authStore.logout()
        return
      }
      unlockError.value = (error as Error)?.message || '解锁失败'
    }
    finally {
      unlockLoading.value = false
    }
  }

  function releaseLock() {
    clearLockState()
    lockMode.value = 'off'
    lockAttempts.value = 0
    unlockPwd.value = ''
    unlockError.value = ''
  }

  /** 以 localStorage 为准同步本标签页的锁屏 UI 态 */
  function syncFromStorage() {
    const locked = localStorage.getItem(LOCK_STATE_KEY) === '1'
    if (locked && lockMode.value !== 'locked') {
      lockMode.value = 'locked'
      lockAttempts.value = 0
      unlockPwd.value = ''
      unlockError.value = ''
    }
    else if (!locked && lockMode.value === 'locked') {
      lockMode.value = 'off'
      unlockPwd.value = ''
      unlockError.value = ''
    }
  }

  /**
   * storage 事件只在**其他**标签页触发，正是跨标签页同步所需：
   * 一处锁屏，所有已打开的标签页立即跟着锁；一处解锁，其余一起解开。
   */
  function handleStorage(e: StorageEvent) {
    if (e.key === null || e.key === LOCK_STATE_KEY) {
      syncFromStorage()
    }
  }

  /** 请求被服务端 423 拒绝（如新开标签页、或锁屏期间后台请求）→ 立刻进入锁屏 */
  function handleServerLocked() {
    syncFromStorage()
  }

  watch(
    () => layoutBridgeStore.lockScreenVersion,
    () => {
      if (appStore.widgetLockScreen) {
        doLock()
      }
    },
  )

  onMounted(() => {
    window.addEventListener('storage', handleStorage)
    window.addEventListener('xihan:session-locked', handleServerLocked)
    // 新标签页/刷新时恢复锁屏态。真正的兜底是服务端 423——即使这里被绕过，请求也拿不到数据。
    syncFromStorage()
  })

  onUnmounted(() => {
    window.removeEventListener('storage', handleStorage)
    window.removeEventListener('xihan:session-locked', handleServerLocked)
  })

  return {
    lockMode,
    lockPwdNew,
    lockPwdConfirm,
    lockPwdError,
    lockLoading,
    unlockPwd,
    unlockError,
    unlockLoading,
    hasLockPwd,
    confirmLock,
    doUnlock,
    releaseLock,
  }
}
