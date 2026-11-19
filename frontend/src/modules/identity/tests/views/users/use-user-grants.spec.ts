import { describe, expect, it } from 'vitest'
import { toGrantItems } from '@/modules/identity/views/users/use-user-grants'

describe('user direct grants', () => {
  it('lists current codes outside the catalog so the transfer never drops them silently', () => {
    const items = toGrantItems([
      {
        code: 'a.b.read',
        displayName: 'Read',
        package: 'a',
        resource: 'b',
        action: 'read',
        scope: 'Both',
        audited: false,
      },
    ], ['*', 'a.b.read'], 'Outside')
    expect(items.map(item => [item.basicId, item.groupCode, item.outside])).toEqual([
      ['a.b.read', 'a.b', false],
      ['*', 'Outside', true],
    ])
  })
})
