import { onMounted, onUnmounted, ref, watch } from 'vue'
import { useAppStore, useAuthStore, useLayoutBridgeStore } from '~/stores'

export type LockMode = 'off' | 'setting' | 'locked'

const LOCK_SESS_KEY = 'xihan_locked'
const LOCK_PWD_SESS_KEY = 'xihan_lock_pwd'
const MAX_LOCK_ATTEMPTS = 5

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

  function confirmLock() {
    if (lockPwdNew.value && lockPwdNew.value !== lockPwdConfirm.value) {
      lockPwdError.value = '两次输入不一致'
      return
    }
    sessionStorage.setItem(LOCK_SESS_KEY, '1')
    if (lockPwdNew.value) {
      sessionStorage.setItem(LOCK_PWD_SESS_KEY, btoa(lockPwdNew.value))
      hasLockPwd.value = true
    }
    else {
      sessionStorage.removeItem(LOCK_PWD_SESS_KEY)
      hasLockPwd.value = false
    }
    lockMode.value = 'locked'
    unlockPwd.value = ''
    unlockError.value = ''
    lockAttempts.value = 0
  }

  function doUnlock() {
    const stored = sessionStorage.getItem(LOCK_PWD_SESS_KEY)
    if (!stored) {
      releaseLock()
      return
    }
    if (btoa(unlockPwd.value) !== stored) {
      lockAttempts.value++
      unlockPwd.value = ''
      if (lockAttempts.value >= MAX_LOCK_ATTEMPTS) {
        releaseLock()
        authStore.logout()
        return
      }
      unlockError.value = `密码错误，还可尝试 ${MAX_LOCK_ATTEMPTS - lockAttempts.value} 次`
      return
    }
    releaseLock()
  }

  function releaseLock() {
    sessionStorage.removeItem(LOCK_SESS_KEY)
    sessionStorage.removeItem(LOCK_PWD_SESS_KEY)
    lockMode.value = 'off'
    lockAttempts.value = 0
    hasLockPwd.value = false
    unlockPwd.value = ''
    unlockError.value = ''
  }

  function handleEscUnlock(e: KeyboardEvent) {
    if (e.key === 'Escape' && lockMode.value === 'locked') {
      if (!sessionStorage.getItem(LOCK_PWD_SESS_KEY))
        releaseLock()
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
    if (sessionStorage.getItem(LOCK_SESS_KEY) === '1') {
      lockMode.value = 'locked'
      lockAttempts.value = 0
      hasLockPwd.value = !!sessionStorage.getItem(LOCK_PWD_SESS_KEY)
    }
  })

  onUnmounted(() => {
    window.removeEventListener('keydown', handleEscUnlock)
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
