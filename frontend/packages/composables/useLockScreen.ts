import { onMounted, onUnmounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { LOCK_STATE_KEY } from '~/constants'
import { useAccessStore, useAppContext, useAppStore, useAuthStore, useLayoutBridgeStore } from '~/stores'
import { clearLockState, isLockedState, SESSION_LOCK_CHANGED_EVENT } from './session-lock'

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

/** 与后端 LockSessionRequestDto 的 [StringLength(64, MinimumLength = 4)] 对齐 */
const LOCK_PWD_MIN_LENGTH = 4
const LOCK_PWD_MAX_LENGTH = 64

export function useLockScreen() {
  const { t } = useI18n()
  const accessStore = useAccessStore()
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
  const logoutLoading = ref(false)
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
      lockPwdError.value = t('component.lock_screen.password_required')
      return
    }
    // 长度先在前端拦一道，否则要撞一次后端 400 才知道口令太短
    if (
      lockPwdNew.value.length < LOCK_PWD_MIN_LENGTH
      || lockPwdNew.value.length > LOCK_PWD_MAX_LENGTH
    ) {
      lockPwdError.value = t('component.lock_screen.password_length', {
        min: LOCK_PWD_MIN_LENGTH,
        max: LOCK_PWD_MAX_LENGTH,
      })
      return
    }
    if (lockPwdNew.value !== lockPwdConfirm.value) {
      lockPwdError.value = t('component.lock_screen.password_mismatch')
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
      lockPwdError.value = (error as Error)?.message || t('component.lock_screen.lock_failed')
    }
    finally {
      lockLoading.value = false
    }
  }

  /** 取消设置（此刻服务端还没置位，遮罩收起即可） */
  function cancelLock() {
    lockMode.value = 'off'
    lockPwdNew.value = ''
    lockPwdConfirm.value = ''
    lockPwdError.value = ''
  }

  /** 解锁：口令由服务端校验（PBKDF2），本地不持有任何哈希 */
  async function doUnlock() {
    if (!unlockPwd.value) {
      unlockError.value = t('component.lock_screen.password_required')
      return
    }

    unlockLoading.value = true
    try {
      await ctx.apis.unlockSessionApi({ password: unlockPwd.value })
      releaseLock()
    }
    catch (error) {
      // 会话本身已失效（被其它设备登出、令牌过期）：请求层收到 401 已强制登出并跳登录页。
      // 此时把「登录已过期」写进锁屏框，只会让用户对着一个永远打不开的锁——直接收起遮罩。
      if (!accessStore.accessToken) {
        releaseLock()
        return
      }
      lockAttempts.value++
      unlockPwd.value = ''
      // 连续失败达上限时服务端已吊销会话，此处直接登出，避免停留在一个已失效的锁屏页
      if (lockAttempts.value >= MAX_LOCK_ATTEMPTS) {
        releaseLock()
        void authStore.logout()
        return
      }
      unlockError.value = (error as Error)?.message || t('component.lock_screen.unlock_failed')
    }
    finally {
      unlockLoading.value = false
    }
  }

  /** 锁屏页的「退出登录」：不解锁，直接结束会话回登录页 */
  async function logoutAndRelogin() {
    logoutLoading.value = true
    try {
      // 先收起遮罩：logout 内部会走接口 + 清路由 + 跳转，中途一直挂着锁屏很难看，
      // 而且失败时也不该把用户继续困在锁屏上（会话是否结束由服务端说了算）
      releaseLock()
      await authStore.logout()
    }
    finally {
      logoutLoading.value = false
    }
  }

  function releaseLock() {
    clearLockState()
    lockMode.value = 'off'
    lockAttempts.value = 0
    lockPwdNew.value = ''
    lockPwdConfirm.value = ''
    lockPwdError.value = ''
    unlockPwd.value = ''
    unlockError.value = ''
  }

  /** 以 localStorage 为准同步本标签页的锁屏 UI 态 */
  function syncFromStorage() {
    const locked = isLockedState()
    // 未登录就没有会话可锁：被其它设备顶下线、令牌过期后本地令牌已被清空，
    // 此时若仍按标记挂上遮罩，用户会看到「登录页上盖着一层怎么刷新都去不掉的锁屏」。
    // 这个残留标记顺手清掉，免得下次进来又撞上。
    if (locked && !accessStore.accessToken) {
      releaseLock()
      return
    }
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

  /**
   * 本标签页内锁定态变更：423 被拦下置位、或强制登出清位，都从这里回到 UI。
   * （同标签页的 localStorage 改动不触发 storage 事件，必须靠这条自定义事件。）
   */
  function handleLockChanged() {
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

  // 令牌没了 = 会话结束（强制登出/主动登出），锁屏必须一并收起，
  // 否则遮罩会盖在登录页上，且刷新后又被 localStorage 标记拉回来
  watch(
    () => accessStore.accessToken,
    (token) => {
      if (!token && lockMode.value !== 'off') {
        releaseLock()
      }
    },
  )

  onMounted(() => {
    window.addEventListener('storage', handleStorage)
    window.addEventListener(SESSION_LOCK_CHANGED_EVENT, handleLockChanged)
    // 新标签页/刷新时恢复锁屏态。真正的兜底是服务端 423——即使这里被绕过，请求也拿不到数据。
    syncFromStorage()
  })

  onUnmounted(() => {
    window.removeEventListener('storage', handleStorage)
    window.removeEventListener(SESSION_LOCK_CHANGED_EVENT, handleLockChanged)
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
    logoutLoading,
    hasLockPwd,
    confirmLock,
    cancelLock,
    doUnlock,
    logoutAndRelogin,
    releaseLock,
  }
}
