import { onMounted, onUnmounted, ref, watch } from 'vue'
import { LOCK_PASSWORD_KEY, LOCK_STATE_KEY } from '~/constants'
import { useAppStore, useAuthStore, useLayoutBridgeStore } from '~/stores'

export type LockMode = 'off' | 'setting' | 'locked'

const MAX_LOCK_ATTEMPTS = 5

/**
 * 清除锁屏状态。登出时必须调用：锁屏状态在 localStorage 里跨会话存活，
 * 不清会导致重新登录后仍卡在锁屏。
 */
export function clearLockState() {
  localStorage.removeItem(LOCK_STATE_KEY)
  localStorage.removeItem(LOCK_PASSWORD_KEY)
}

function toHex(buffer: ArrayBuffer) {
  return Array.from(new Uint8Array(buffer))
    .map(byte => byte.toString(16).padStart(2, '0'))
    .join('')
}

function randomSalt() {
  const bytes = new Uint8Array(16)
  crypto.getRandomValues(bytes)
  return toHex(bytes.buffer)
}

/**
 * 口令摘要：SHA-256(salt + password)，按 UTF-8 编码。
 *
 * 不用 btoa：它是 base64 编码而非哈希（`atob()` 一步还原，等于明文存储），
 * 且遇到非 Latin-1 字符（如中文密码）会直接抛 InvalidCharacterError。
 */
async function hashPassword(password: string, salt: string) {
  const data = new TextEncoder().encode(`${salt}:${password}`)
  const digest = await crypto.subtle.digest('SHA-256', data)
  return toHex(digest)
}

/** WebCrypto 仅在安全上下文（HTTPS / localhost）可用 */
function isCryptoAvailable() {
  return typeof crypto !== 'undefined' && !!crypto.subtle
}

export function useLockScreen() {
  const appStore = useAppStore()
  const authStore = useAuthStore()
  const layoutBridgeStore = useLayoutBridgeStore()

  const lockMode = ref<LockMode>('off')
  const lockAttempts = ref(0)
  const lockPwdNew = ref('')
  const lockPwdConfirm = ref('')
  const lockPwdError = ref('')
  const unlockPwd = ref('')
  const unlockError = ref('')
  const hasLockPwd = ref(false)

  function doLock() {
    lockMode.value = 'setting'
    lockPwdNew.value = ''
    lockPwdConfirm.value = ''
    lockPwdError.value = ''
  }

  async function confirmLock() {
    if (lockPwdNew.value && lockPwdNew.value !== lockPwdConfirm.value) {
      lockPwdError.value = '两次输入不一致'
      return
    }

    if (lockPwdNew.value) {
      if (!isCryptoAvailable()) {
        lockPwdError.value = '当前环境不支持加密（需 HTTPS 或 localhost），无法设置锁屏密码'
        return
      }
      try {
        const salt = randomSalt()
        const digest = await hashPassword(lockPwdNew.value, salt)
        localStorage.setItem(LOCK_PASSWORD_KEY, `${salt}:${digest}`)
        hasLockPwd.value = true
      }
      catch {
        lockPwdError.value = '锁屏密码设置失败'
        return
      }
    }
    else {
      localStorage.removeItem(LOCK_PASSWORD_KEY)
      hasLockPwd.value = false
    }

    // 密码先落库、状态后置：其他标签页收到 storage 事件时，密码摘要必然已就绪
    localStorage.setItem(LOCK_STATE_KEY, '1')
    lockMode.value = 'locked'
    unlockPwd.value = ''
    unlockError.value = ''
    lockAttempts.value = 0
  }

  async function doUnlock() {
    const stored = localStorage.getItem(LOCK_PASSWORD_KEY)
    if (!stored) {
      releaseLock()
      return
    }

    const separator = stored.indexOf(':')
    if (separator <= 0) {
      // 摘要格式异常（被篡改/残留旧格式）：不能当作"无密码"放行，直接登出
      releaseLock()
      void authStore.logout()
      return
    }

    const salt = stored.slice(0, separator)
    const expected = stored.slice(separator + 1)

    let actual: string
    try {
      actual = await hashPassword(unlockPwd.value, salt)
    }
    catch {
      unlockError.value = '解锁失败，请重试'
      return
    }

    if (actual !== expected) {
      lockAttempts.value++
      unlockPwd.value = ''
      if (lockAttempts.value >= MAX_LOCK_ATTEMPTS) {
        releaseLock()
        void authStore.logout()
        return
      }
      unlockError.value = `密码错误，还可尝试 ${MAX_LOCK_ATTEMPTS - lockAttempts.value} 次`
      return
    }

    releaseLock()
  }

  function releaseLock() {
    clearLockState()
    lockMode.value = 'off'
    lockAttempts.value = 0
    hasLockPwd.value = false
    unlockPwd.value = ''
    unlockError.value = ''
  }

  function handleEscUnlock(e: KeyboardEvent) {
    if (e.key === 'Escape' && lockMode.value === 'locked') {
      if (!localStorage.getItem(LOCK_PASSWORD_KEY)) {
        releaseLock()
      }
    }
  }

  /** 以 localStorage 为唯一事实源同步本标签页的锁屏态 */
  function syncFromStorage() {
    const locked = localStorage.getItem(LOCK_STATE_KEY) === '1'

    if (locked && lockMode.value !== 'locked') {
      lockMode.value = 'locked'
      lockAttempts.value = 0
      unlockPwd.value = ''
      unlockError.value = ''
      hasLockPwd.value = !!localStorage.getItem(LOCK_PASSWORD_KEY)
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
   * key 为 null 表示 storage 被整体 clear()。
   */
  function handleStorage(e: StorageEvent) {
    if (e.key === null || e.key === LOCK_STATE_KEY || e.key === LOCK_PASSWORD_KEY) {
      syncFromStorage()
    }
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
    window.addEventListener('keydown', handleEscUnlock)
    window.addEventListener('storage', handleStorage)
    // 新标签页/刷新时从 localStorage 恢复锁屏态——这一步是"新开标签页绕过锁屏"的堵口
    syncFromStorage()
  })

  onUnmounted(() => {
    window.removeEventListener('keydown', handleEscUnlock)
    window.removeEventListener('storage', handleStorage)
  })

  return {
    lockMode,
    lockPwdNew,
    lockPwdConfirm,
    lockPwdError,
    unlockPwd,
    unlockError,
    hasLockPwd,
    confirmLock,
    doUnlock,
    releaseLock,
  }
}
