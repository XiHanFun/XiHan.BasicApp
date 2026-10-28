import { beforeEach, describe, expect, it, vi } from 'vitest'
import { permissions } from '~/contracts'
import { packagesOf, permissionsResource } from './use-permissions'

const catalog = [
  { code: 'access.roles.read', displayName: 'Read roles', description: 'View roles', package: 'access', resource: 'roles', action: 'read', scope: 'Both' as const, audited: false },
  { code: 'tenancy.tenants.create', displayName: 'Create tenants', description: null, package: 'tenancy', resource: 'tenants', action: 'create', scope: 'Platform' as const, audited: true },
  { code: 'access.roles.grant', displayName: 'Grant roles', description: 'Grant permissions', package: 'access', resource: 'roles', action: 'grant', scope: 'Both' as const, audited: true },
]

describe('permission catalog resource', () => {
  beforeEach(() => {
    vi.spyOn(permissions, 'list').mockResolvedValue({ items: catalog })
  })

  it('filters by keyword, package, side and audit flag, then pages in the browser', async () => {
    const resource = permissionsResource()
    const audited = await resource.page!({ page: 1, pageSize: 10, filters: { audited: 'true' } })
    expect(audited.items.map(item => item.code)).toEqual(['tenancy.tenants.create', 'access.roles.grant'])
    const keyword = await resource.page!({ page: 1, pageSize: 10, filters: { keyword: 'GRANT', package: 'access' } })
    expect(keyword.items.map(item => item.code)).toEqual(['access.roles.grant'])
    const sorted = await resource.page!({ page: 1, pageSize: 2, filters: {}, sorts: [{ field: 'code', order: 'asc' }] })
    expect(sorted.items.map(item => item.code)).toEqual(['access.roles.grant', 'access.roles.read'])
    expect(sorted.page.totalCount).toBe(3)
  })

  it('refuses conditions the page did not register instead of dropping them', async () => {
    await expect(permissionsResource().page!({ page: 1, pageSize: 10, filters: { action: 'read' } })).rejects.toThrow('action')
  })

  it('lists each package once, sorted', () => {
    expect(packagesOf(catalog)).toEqual(['access', 'tenancy'])
  })
})
