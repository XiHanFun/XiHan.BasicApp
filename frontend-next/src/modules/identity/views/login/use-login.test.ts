import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it } from 'vitest'
import { configureRequest } from '~/request'
import { useAuthStore } from '~/stores'
import { useLogin } from './use-login'

function json(status: number, body: object, contentType = 'application/json'): Response {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': contentType } })
}

const user = { id: 'u-1', userName: 'alice', displayName: 'Alice', email: null, phoneNumber: null, homeTenantId: 'acme', status: 'Active', lastLoginAt: null, createdAt: '2026-10-28T00:00:00Z', createdBy: null }

describe('useLogin', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    localStorage.clear()
  })

  it('shows the localized detail for a denied login and signs in on success', async () => {
    const bodies: unknown[] = []
    configureRequest({
      baseUrl: '',
      getToken: () => null,
      fetch: (async (_url: RequestInfo | URL, init?: RequestInit) => {
        const body = JSON.parse(String(init?.body)) as { password: string }
        bodies.push(body)
        if (body.password !== 'Alice@12345')
          return json(403, { code: 'IDENTITY_DENIED_003', status: 403, detail: '用户名或密码不正确。', traceId: 'x' }, 'application/problem+json')
        return json(200, { accessToken: 'a-1', accessExpiresAt: '2026-10-28T02:00:00Z', refreshToken: 'r-1', refreshExpiresAt: '2026-11-04T00:00:00Z', sessionId: 's-1', tenantId: 'acme', mustChangePassword: false, user })
      }) as unknown as typeof fetch,
    })
    const login = useLogin()
    login.form.tenant = ' acme '
    login.form.userName = 'alice'
    login.form.password = 'wrong-12345'
    expect(await login.submit()).toBe(false)
    expect(login.error.value).toBe('用户名或密码不正确。')
    expect(bodies[0]).toEqual({ userName: 'alice', password: 'wrong-12345', tenant: 'acme' })

    login.form.password = 'Alice@12345'
    expect(await login.submit()).toBe(true)
    expect(login.error.value).toBeNull()
    expect(login.form.password).toBe('')
    expect(useAuthStore().user?.userName).toBe('alice')
  })

  it('hands schema violations to the fields', async () => {
    configureRequest({
      baseUrl: '',
      getToken: () => null,
      fetch: (async () => json(400, { code: 'KERNEL_VALIDATION_003', status: 400, detail: 'invalid', traceId: 'x', errors: { password: ['required'], tenant: ['pattern'] } }, 'application/problem+json')) as unknown as typeof fetch,
    })
    const login = useLogin()
    login.form.userName = 'alice'
    expect(await login.submit()).toBe(false)
    expect(login.fieldError('password')).toBe('required')
    expect(login.fieldError('tenant')).toBe('pattern')
    expect(login.error.value).toBeNull()
  })
})
