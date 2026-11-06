import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it } from 'vitest'
import { configureRequest } from '~/request'
import { useAppStore, useAuthStore } from '~/stores'
import { usePermissionNames } from './permission-names'

interface Call { url: string, locale: null | string }

/** 权限目录按 Accept-Language 给显示名（与后端一样） */
const DISPLAY: Record<string, Record<string, string>> = {
  'zh-CN': { 'identity.users.read': '读取用户' },
  'en-US': { 'identity.users.read': 'Read users' },
}

function record(): Call[] {
  const calls: Call[] = []
  configureRequest({
    baseUrl: '',
    getToken: () => 't',
    getLocale: () => useAppStore().locale,
    fetch: (async (url: RequestInfo | URL, init?: RequestInit) => {
      const locale = new Headers(init?.headers).get('Accept-Language')
      calls.push({
        url: String(url),
        locale,
      })
      const items = Object.entries(DISPLAY[locale ?? 'en-US'] ?? {}).map(([code, displayName]) => ({
        code,
        displayName,
        description: null,
        package: 'identity',
        resource: 'users',
        action: 'read',
        scope: 'Both',
        audited: false,
      }))
      return new Response(JSON.stringify({ items }), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      })
    }) as unknown as typeof fetch,
  })
  return calls
}

function grant(permissions: string[]) {
  useAuthStore().permissions = {
    permissions,
    roles: [],
    menus: [],
    pages: [],
    isPlatform: true,
  } as never
}

beforeEach(() => {
  setActivePinia(createPinia())
})

describe('usePermissionNames', () => {
  it('取当前界面语言的权限目录：同一语言只取一次，换了语言再取；目录里没有的原样显示权限码', async () => {
    grant(['access.permissions.read'])
    const calls = record()
    const app = useAppStore()
    app.locale = 'zh-CN'
    const names = usePermissionNames()
    await names.load()
    await names.load()
    expect(calls).toEqual([
      { url: '/api/permissions', locale: 'zh-CN' },
    ])
    expect(names.nameOf('identity.users.read')).toBe('读取用户')
    expect(names.nameOf('custom.thing')).toBe('custom.thing')

    app.locale = 'en-US'
    await names.load()
    expect(calls.map(c => c.locale)).toEqual(['zh-CN', 'en-US'])
    expect(names.nameOf('identity.users.read')).toBe('Read users')
  })

  it('没有读权限目录的权限：不请求，原样显示权限码', async () => {
    grant([])
    const calls = record()
    const names = usePermissionNames()
    await names.load()
    expect(calls).toEqual([])
    expect(names.nameOf('identity.users.read')).toBe('identity.users.read')
  })
})
