import { describe, expect, it } from 'vitest'
import { resolveView, viewKeys } from './views'

describe('view resolution', () => {
  it('maps a kebab component path to the module view file', () => {
    expect(viewKeys()).toContain('identity/users/index')
    expect(typeof resolveView('identity/users/index')).toBe('function')
  })

  it('fails loudly for an unknown component path', () => {
    expect(() => resolveView('identity/nope/index')).toThrow(/is not found/)
  })
})
