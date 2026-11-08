/**
 * useSignalRIntegration 的用户设置推送
 * 锁定：其它设备改了页面设置（scene=Page）推过来，按页面码与分区分发给已打开的页面；本端自己的回显忽略
 */
import { mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { defineComponent, h, nextTick, reactive, ref } from 'vue'
import { subscribeRemotePageSetting } from '~/components/schema'
import { USER_SETTING_CLIENT_ID } from '~/constants'
import { RealtimeMethods } from '~/contracts'
import { useAppStore } from '~/stores'
import { registerAppContext } from '~/stores/app-context'

const handlers = new Map<string, (payload: unknown) => void>()

vi.mock('vue-i18n', async importOriginal => ({
  ...await importOriginal<typeof import('vue-i18n')>(),
  useI18n: () => ({
    t: (key: string) => key,
  }),
}))
vi.mock('~/composables', async importOriginal => ({
  ...await importOriginal<typeof import('~/composables')>(),
  useSignalR: () => ({
    connected: ref(true),
    on: (method: string, handler: (payload: unknown) => void) => handlers.set(method, handler),
    start: () => Promise.resolve(),
    stop: () => {},
    destroy: () => Promise.resolve(),
  }),
}))
vi.mock('~/stores', async importOriginal => ({
  ...await importOriginal<typeof import('~/stores')>(),
  useAccessStore: () => reactive({
    accessToken: 'token',
    $reset: () => {},
  }),
}))

registerAppContext({
  apis: {
    userSettingApi: {
      get: () => Promise.resolve({ scene: 1, settingKey: '' }),
      save: () => Promise.resolve({ scene: 1, settingKey: '' }),
    },
  } as never,
})

async function mountIntegration() {
  vi.stubEnv('VITE_REALTIME_HUB', '/hubs/notification')
  const { useSignalRIntegration } = await import('~/layouts/basic/composables/use-signalr-integration')
  mount(defineComponent({
    setup() {
      useSignalRIntegration()
      return () => h('div')
    },
  }))
  await nextTick()
  const handler = handlers.get(RealtimeMethods.UserSettingChanged)
  expect(handler).toBeTypeOf('function')
  return handler!
}

beforeEach(() => {
  handlers.clear()
  setActivePinia(createPinia())
  useAppStore().tableSyncEnabled = true
})

describe('页面设置推送', () => {
  it('其它设备保存的列设置分发给这一页的 table 分区', async () => {
    const push = await mountIntegration()
    const received: unknown[] = []
    const unsubscribe = subscribeRemotePageSetting('identity:users', 'table', value => received.push(value))

    push({
      scene: 'Page',
      settingKey: 'identity:users',
      settingValue: JSON.stringify({
        table: {
          hidden: ['email'],
        },
      }),
      sourceClientId: 'other-tab',
    })
    unsubscribe()

    expect(received).toStrictEqual([
      {
        hidden: ['email'],
      },
    ])
  })

  it('本端自己保存的回显不再应用', async () => {
    const push = await mountIntegration()
    const received: unknown[] = []
    const unsubscribe = subscribeRemotePageSetting('identity:roles', 'table', value => received.push(value))

    push({
      scene: 'Page',
      settingKey: 'identity:roles',
      settingValue: JSON.stringify({
        table: {
          hidden: ['code'],
        },
      }),
      sourceClientId: USER_SETTING_CLIENT_ID,
    })
    unsubscribe()

    expect(received).toStrictEqual([])
  })
})
