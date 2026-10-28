import { describe, expect, it } from 'vitest'
import { applyPermissionTransfer, diffPermissionGrants, toGrantItems } from './use-user-grants'

describe('user direct grants', () => {
  it('moves a permission between the grant and deny sides without keeping it in both', () => {
    const start = new Map([['identity.users.read', 'Grant' as const], ['identity.users.update', 'Deny' as const]])
    const denied = applyPermissionTransfer(start, 'Deny', ['identity.users.update', 'identity.users.read'])
    expect(denied.get('identity.users.read')).toBe('Deny')
    expect([...denied.values()].filter(action => action === 'Grant')).toHaveLength(0)
    const cleared = applyPermissionTransfer(denied, 'Deny', ['identity.users.read'])
    expect(cleared.has('identity.users.update')).toBe(false)
  })

  it('splits the draft into grants, denies and revocations, skipping unchanged ones', () => {
    const current = new Map([['a.b.read', 'Grant' as const], ['a.b.update', 'Deny' as const], ['a.b.delete', 'Grant' as const]])
    const draft = new Map([['a.b.read', 'Grant' as const], ['a.b.update', 'Grant' as const], ['a.b.create', 'Deny' as const]])
    expect(diffPermissionGrants(draft, current)).toEqual({ grant: ['a.b.update'], deny: ['a.b.create'], revoke: ['a.b.delete'] })
  })

  it('lists current codes outside the catalog so the transfer never drops them silently', () => {
    const items = toGrantItems([{ code: 'a.b.read', displayName: 'Read', package: 'a', resource: 'b', action: 'read', scope: 'Both', audited: false }], ['*', 'a.b.read'], 'Outside')
    expect(items.map(item => [item.basicId, item.groupCode, item.outside])).toEqual([['a.b.read', 'a.b', false], ['*', 'Outside', true]])
  })
})
