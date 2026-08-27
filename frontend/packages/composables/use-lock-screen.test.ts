/**
 * useLockScreen 锁屏 / 强制改密引导单元测试。
 * 职责：锁定前端校验的各条分支与文案、服务端置位后的本地 UI 标记、
 * 「会话已失效就直接收起遮罩而不是把用户困在打不开的锁上」「连续失败达上限直接登出」
 * 这两条注释里写明的坑，以及跨标签页 / 本标签页事件同步与卸载后监听器必须被摘干净。
 */
import type { App } from 'vue'
import { mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { defineComponent, h } from 'vue'
import { HOME_PATH, LOCK_REASON_KEY, LOCK_STATE_KEY } from '~/constants'
import { i18n } from '~/locales'
import { useAccessStore, useAuthStore } from '~/stores'
import { registerAppContext } from '~/stores/app-context'
import { SESSION_LOCK_CHANGED_EVENT } from './session-lock'
import { useLockScreen } from './useLockScreen'

type LockApi = (input: { password: string }) => Promise<unknown>
type ChangeApi = (input: { oldPassword: string, newPassword: string }) => Promise<unknown>

let lockSessionApi = vi.fn<LockApi>()
let unlockSessionApi = vi.fn<LockApi>()
let changePasswordApi = vi.fn<ChangeApi>()
let replaceSpy = vi.fn()
let logoutSpy = vi.fn()

const wrappers: Array<{ unmount: () => void }> = []

function mountLockScreen() {
  let captured: ReturnType<typeof useLockScreen> | null = null
  const wrapper = mount(defineComponent({
    setup() {
      captured = useLockScreen()
      return () => h('div')
    },
  }), { global: { plugins: [i18n as unknown as { install: (app: App) => void }] } })
  wrappers.push(wrapper)
  return { lock: captured as unknown as ReturnType<typeof useLockScreen>, wrapper }
}

beforeEach(() => {
  setActivePinia(createPinia())
  i18n.global.locale.value = 'zh-CN'

  lockSessionApi = vi.fn<LockApi>(async () => ({}))
  unlockSessionApi = vi.fn<LockApi>(async () => ({}))
  changePasswordApi = vi.fn<ChangeApi>(async () => ({}))
  replaceSpy = vi.fn()
  registerAppContext({
    apis: { lockSessionApi, unlockSessionApi, changePasswordApi } as never,
    getRouter: () => Promise.resolve({ replace: replaceSpy } as never),
  })

  logoutSpy = vi.fn(async () => {})
  const authStore = useAuthStore()
  vi.spyOn(authStore, 'logout').mockImplementation(logoutSpy as unknown as typeof authStore.logout)

  useAccessStore().setAccessToken('tok-1')
})

afterEach(() => {
  while (wrappers.length > 0) {
    wrappers.pop()?.unmount()
  }
  vi.restoreAllMocks()
})

describe('挂载时按本地标记恢复', () => {
  it('无锁定标记时保持关闭态', () => {
    const { lock } = mountLockScreen()

    expect(lock.lockMode.value).toBe('off')
  })

  it('有锁定标记且已登录时恢复到口令解锁引导', () => {
    localStorage.setItem(LOCK_STATE_KEY, '1')

    const { lock } = mountLockScreen()

    expect(lock.lockMode.value).toBe('locked')
    expect(lock.hasLockPwd.value).toBe(true)
  })

  it('锁定原因为强制改密时进入改密引导而不是口令框', () => {
    localStorage.setItem(LOCK_STATE_KEY, '1')
    localStorage.setItem(LOCK_REASON_KEY, 'PasswordChangeRequired')

    const { lock } = mountLockScreen()

    expect(lock.lockMode.value).toBe('password-change')
  })

  it('有锁定标记但已无令牌时清掉残留标记，不让遮罩盖在登录页上', () => {
    localStorage.setItem(LOCK_STATE_KEY, '1')
    useAccessStore().setAccessToken(null)

    const { lock } = mountLockScreen()

    expect(lock.lockMode.value).toBe('off')
    expect(localStorage.getItem(LOCK_STATE_KEY)).toBeNull()
  })
})

describe('confirmLock 设置锁屏口令', () => {
  it('空口令被拒绝：服务端强制模式下无口令锁屏毫无意义', async () => {
    const { lock } = mountLockScreen()

    await lock.confirmLock()

    expect(lock.lockPwdError.value).toBe('请输入锁屏密码')
    expect(lockSessionApi).not.toHaveBeenCalled()
  })

  it('口令短于 4 位时前端先拦一道，不去撞后端 400', async () => {
    const { lock } = mountLockScreen()
    lock.lockPwdNew.value = '123'
    lock.lockPwdConfirm.value = '123'

    await lock.confirmLock()

    expect(lock.lockPwdError.value).toBe('锁屏密码长度须为 4-64 位')
    expect(lockSessionApi).not.toHaveBeenCalled()
  })

  it('口令超过 64 位同样被拦下', async () => {
    const { lock } = mountLockScreen()
    lock.lockPwdNew.value = 'a'.repeat(65)
    lock.lockPwdConfirm.value = 'a'.repeat(65)

    await lock.confirmLock()

    expect(lock.lockPwdError.value).toBe('锁屏密码长度须为 4-64 位')
  })

  it('恰好 4 位与 64 位是允许的边界', async () => {
    const { lock } = mountLockScreen()
    lock.lockPwdNew.value = '1234'
    lock.lockPwdConfirm.value = '1234'
    await lock.confirmLock()
    expect(lock.lockPwdError.value).toBe('')

    lock.lockPwdNew.value = 'b'.repeat(64)
    lock.lockPwdConfirm.value = 'b'.repeat(64)
    await lock.confirmLock()
    expect(lock.lockPwdError.value).toBe('')
    expect(lockSessionApi).toHaveBeenCalledTimes(2)
  })

  it('两次输入不一致时报错且不调接口', async () => {
    const { lock } = mountLockScreen()
    lock.lockPwdNew.value = 'abcd'
    lock.lockPwdConfirm.value = 'abce'

    await lock.confirmLock()

    expect(lock.lockPwdError.value).toBe('两次输入不一致')
    expect(lockSessionApi).not.toHaveBeenCalled()
  })

  it('服务端置位成功后写本地 UI 标记并切到锁定态，同时清空输入', async () => {
    const { lock } = mountLockScreen()
    lock.lockPwdNew.value = 'abcd'
    lock.lockPwdConfirm.value = 'abcd'

    await lock.confirmLock()

    expect(lockSessionApi).toHaveBeenCalledWith({ password: 'abcd' })
    expect(localStorage.getItem(LOCK_STATE_KEY)).toBe('1')
    expect(lock.lockMode.value).toBe('locked')
    expect(lock.lockPwdNew.value).toBe('')
    expect(lock.lockPwdConfirm.value).toBe('')
    expect(lock.lockLoading.value).toBe(false)
  })

  it('服务端置位失败时展示接口错误消息，且不写本地标记', async () => {
    lockSessionApi.mockRejectedValueOnce(new Error('会话已失效'))
    const { lock } = mountLockScreen()
    lock.lockPwdNew.value = 'abcd'
    lock.lockPwdConfirm.value = 'abcd'

    await lock.confirmLock()

    expect(lock.lockPwdError.value).toBe('会话已失效')
    expect(localStorage.getItem(LOCK_STATE_KEY)).toBeNull()
    expect(lock.lockMode.value).toBe('off')
  })

  it('接口错误没有消息时退回通用文案', async () => {
    lockSessionApi.mockRejectedValueOnce({ name: 'HttpError' })
    const { lock } = mountLockScreen()
    lock.lockPwdNew.value = 'abcd'
    lock.lockPwdConfirm.value = 'abcd'

    await lock.confirmLock()

    expect(lock.lockPwdError.value).toBe('锁屏失败')
  })

  it('取消设置只收起引导，不调用服务端也不写标记', () => {
    const { lock } = mountLockScreen()
    lock.lockPwdNew.value = 'abcd'

    lock.cancelLock()

    expect(lock.lockMode.value).toBe('off')
    expect(lock.lockPwdNew.value).toBe('')
    expect(lockSessionApi).not.toHaveBeenCalled()
  })
})

describe('doUnlock 解锁', () => {
  it('空口令被拒绝，不调接口', async () => {
    localStorage.setItem(LOCK_STATE_KEY, '1')
    const { lock } = mountLockScreen()

    await lock.doUnlock()

    expect(lock.unlockError.value).toBe('请输入锁屏密码')
    expect(unlockSessionApi).not.toHaveBeenCalled()
  })

  it('解锁成功后清标记、收起遮罩并清空输入', async () => {
    localStorage.setItem(LOCK_STATE_KEY, '1')
    const { lock } = mountLockScreen()
    lock.unlockPwd.value = 'abcd'

    await lock.doUnlock()

    expect(unlockSessionApi).toHaveBeenCalledWith({ password: 'abcd' })
    expect(localStorage.getItem(LOCK_STATE_KEY)).toBeNull()
    expect(lock.lockMode.value).toBe('off')
    expect(lock.unlockPwd.value).toBe('')
  })

  it('解锁失败时清空输入并展示错误，遮罩仍在', async () => {
    localStorage.setItem(LOCK_STATE_KEY, '1')
    unlockSessionApi.mockRejectedValueOnce(new Error('口令不正确'))
    const { lock } = mountLockScreen()
    lock.unlockPwd.value = 'wrong'

    await lock.doUnlock()

    expect(lock.unlockError.value).toBe('口令不正确')
    expect(lock.unlockPwd.value).toBe('')
    expect(lock.lockMode.value).toBe('locked')
  })

  it('会话已失效（令牌已被清空）时直接收起遮罩，不把用户困在打不开的锁上', async () => {
    localStorage.setItem(LOCK_STATE_KEY, '1')
    unlockSessionApi.mockImplementationOnce(async () => {
      useAccessStore().setAccessToken(null)
      throw new Error('登录已过期')
    })
    const { lock } = mountLockScreen()
    lock.unlockPwd.value = 'abcd'

    await lock.doUnlock()

    expect(lock.lockMode.value).toBe('off')
    expect(lock.unlockError.value).toBe('')
    expect(logoutSpy).not.toHaveBeenCalled()
  })

  it('连续失败到第 5 次时直接登出，不停留在已被服务端吊销的锁屏页', async () => {
    localStorage.setItem(LOCK_STATE_KEY, '1')
    unlockSessionApi.mockRejectedValue(new Error('口令不正确'))
    const { lock } = mountLockScreen()

    for (let i = 0; i < 4; i++) {
      lock.unlockPwd.value = 'wrong'
      await lock.doUnlock()
    }
    expect(logoutSpy).not.toHaveBeenCalled()
    expect(lock.lockMode.value).toBe('locked')

    lock.unlockPwd.value = 'wrong'
    await lock.doUnlock()

    expect(logoutSpy).toHaveBeenCalledTimes(1)
    expect(lock.lockMode.value).toBe('off')
  })

  it('解锁请求过程中 loading 复位', async () => {
    localStorage.setItem(LOCK_STATE_KEY, '1')
    const { lock } = mountLockScreen()
    lock.unlockPwd.value = 'abcd'

    await lock.doUnlock()

    expect(lock.unlockLoading.value).toBe(false)
  })
})

describe('doChangePassword 强制改密', () => {
  it('三个字段任缺其一都被拦下', async () => {
    localStorage.setItem(LOCK_STATE_KEY, '1')
    localStorage.setItem(LOCK_REASON_KEY, 'PasswordChangeRequired')
    const { lock } = mountLockScreen()

    lock.changePwdOld.value = 'old'
    await lock.doChangePassword()

    expect(lock.changePwdError.value).toBe('请填写当前密码与新密码')
    expect(changePasswordApi).not.toHaveBeenCalled()
  })

  it('新密码两次输入不一致时报错', async () => {
    localStorage.setItem(LOCK_STATE_KEY, '1')
    localStorage.setItem(LOCK_REASON_KEY, 'PasswordChangeRequired')
    const { lock } = mountLockScreen()
    lock.changePwdOld.value = 'old'
    lock.changePwdNew.value = 'new-1'
    lock.changePwdConfirm.value = 'new-2'

    await lock.doChangePassword()

    expect(lock.changePwdError.value).toBe('两次输入不一致')
    expect(changePasswordApi).not.toHaveBeenCalled()
  })

  it('改密成功后收起引导并重新导航，让守卫完整跑一遍', async () => {
    localStorage.setItem(LOCK_STATE_KEY, '1')
    localStorage.setItem(LOCK_REASON_KEY, 'PasswordChangeRequired')
    const { lock } = mountLockScreen()
    lock.changePwdOld.value = 'old'
    lock.changePwdNew.value = 'newpwd'
    lock.changePwdConfirm.value = 'newpwd'

    await lock.doChangePassword()

    expect(changePasswordApi).toHaveBeenCalledWith({ oldPassword: 'old', newPassword: 'newpwd' })
    expect(localStorage.getItem(LOCK_STATE_KEY)).toBeNull()
    expect(lock.lockMode.value).toBe('off')
    expect(replaceSpy).toHaveBeenCalledWith(HOME_PATH)
  })

  it('改密失败时展示接口错误，引导仍在', async () => {
    localStorage.setItem(LOCK_STATE_KEY, '1')
    localStorage.setItem(LOCK_REASON_KEY, 'PasswordChangeRequired')
    changePasswordApi.mockRejectedValueOnce(new Error('原密码不正确'))
    const { lock } = mountLockScreen()
    lock.changePwdOld.value = 'old'
    lock.changePwdNew.value = 'newpwd'
    lock.changePwdConfirm.value = 'newpwd'

    await lock.doChangePassword()

    expect(lock.changePwdError.value).toBe('原密码不正确')
    expect(lock.lockMode.value).toBe('password-change')
    expect(replaceSpy).not.toHaveBeenCalled()
  })

  it('改密失败且会话已失效时直接收起引导', async () => {
    localStorage.setItem(LOCK_STATE_KEY, '1')
    localStorage.setItem(LOCK_REASON_KEY, 'PasswordChangeRequired')
    changePasswordApi.mockImplementationOnce(async () => {
      useAccessStore().setAccessToken(null)
      throw new Error('登录已过期')
    })
    const { lock } = mountLockScreen()
    lock.changePwdOld.value = 'old'
    lock.changePwdNew.value = 'newpwd'
    lock.changePwdConfirm.value = 'newpwd'

    await lock.doChangePassword()

    expect(lock.lockMode.value).toBe('off')
    expect(lock.changePwdError.value).toBe('')
  })
})

describe('锁屏页的退出登录', () => {
  it('先收起遮罩再登出，失败也不把用户继续困在锁屏上', async () => {
    localStorage.setItem(LOCK_STATE_KEY, '1')
    const { lock } = mountLockScreen()

    await lock.logoutAndRelogin()

    expect(lock.lockMode.value).toBe('off')
    expect(localStorage.getItem(LOCK_STATE_KEY)).toBeNull()
    expect(logoutSpy).toHaveBeenCalledTimes(1)
    expect(lock.logoutLoading.value).toBe(false)
  })
})

describe('锁定态同步与监听器清理', () => {
  it('本标签页的锁定态变更事件驱动 UI 同步（storage 事件不覆盖同页改动）', async () => {
    const { lock } = mountLockScreen()
    expect(lock.lockMode.value).toBe('off')

    localStorage.setItem(LOCK_STATE_KEY, '1')
    window.dispatchEvent(new CustomEvent(SESSION_LOCK_CHANGED_EVENT))

    expect(lock.lockMode.value).toBe('locked')
  })

  it('其它标签页的 storage 事件同样触发同步', () => {
    const { lock } = mountLockScreen()

    localStorage.setItem(LOCK_STATE_KEY, '1')
    window.dispatchEvent(new StorageEvent('storage', { key: LOCK_STATE_KEY }))

    expect(lock.lockMode.value).toBe('locked')
  })

  it('整体清空 localStorage（key 为 null）的 storage 事件也参与同步', () => {
    localStorage.setItem(LOCK_STATE_KEY, '1')
    const { lock } = mountLockScreen()
    expect(lock.lockMode.value).toBe('locked')

    localStorage.clear()
    window.dispatchEvent(new StorageEvent('storage', { key: null }))

    expect(lock.lockMode.value).toBe('off')
  })

  it('无关键名的 storage 事件被忽略，不触发同步', () => {
    const { lock } = mountLockScreen()

    localStorage.setItem(LOCK_STATE_KEY, '1')
    window.dispatchEvent(new StorageEvent('storage', { key: 'xihan_unrelated' }))

    expect(lock.lockMode.value).toBe('off')
  })

  it('令牌被清空（强制登出）时自动收起遮罩', async () => {
    localStorage.setItem(LOCK_STATE_KEY, '1')
    const { lock } = mountLockScreen()
    expect(lock.lockMode.value).toBe('locked')

    useAccessStore().setAccessToken(null)
    await Promise.resolve()

    expect(lock.lockMode.value).toBe('off')
  })

  it('卸载后两个窗口监听器都被摘掉，再派事件不再改动已卸载实例的状态', () => {
    const addSpy = vi.spyOn(window, 'addEventListener')
    const removeSpy = vi.spyOn(window, 'removeEventListener')
    const { lock, wrapper } = mountLockScreen()

    const added = addSpy.mock.calls.map(call => call[0])
    expect(added).toContain('storage')
    expect(added).toContain(SESSION_LOCK_CHANGED_EVENT)

    wrapper.unmount()
    wrappers.pop()

    const removed = removeSpy.mock.calls.map(call => call[0])
    expect(removed).toContain('storage')
    expect(removed).toContain(SESSION_LOCK_CHANGED_EVENT)

    localStorage.setItem(LOCK_STATE_KEY, '1')
    window.dispatchEvent(new CustomEvent(SESSION_LOCK_CHANGED_EVENT))
    window.dispatchEvent(new StorageEvent('storage', { key: LOCK_STATE_KEY }))

    expect(lock.lockMode.value).toBe('off')
  })
})
