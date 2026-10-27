import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it } from 'vitest'
import { decodeClaims, useAuthStore } from './auth'

function token(payload: object): string {
  const encode = (value: object) => btoa(JSON.stringify(value)).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '')
  return `${encode({ alg: 'HS256' })}.${encode(payload)}.sig`
}

describe('auth store', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    localStorage.clear()
  })

  it('decodes subject, tenant and role claims without verifying the signature', () => {
    expect(decodeClaims(token({ sub: 'alice', tenant: 'acme', role: ['admin'] }))).toEqual({ sub: 'alice', tenant: 'acme', role: ['admin'] })
    expect(decodeClaims('not-a-jwt')).toBeNull()
    expect(decodeClaims(null)).toBeNull()
  })

  it('persists the token and exposes authentication state', () => {
    const store = useAuthStore()
    expect(store.isAuthenticated).toBe(false)
    store.setToken(token({ sub: 'alice', tenant: 'acme' }))
    expect(store.isAuthenticated).toBe(true)
    expect(store.claims?.tenant).toBe('acme')
    expect(localStorage.getItem('xihan.next.token')).toBe(store.token)
    store.setToken('   ')
    expect(store.token).toBeNull()
    expect(localStorage.getItem('xihan.next.token')).toBeNull()
  })
})
