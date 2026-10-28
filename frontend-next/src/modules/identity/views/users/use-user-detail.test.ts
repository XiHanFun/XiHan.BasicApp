import { describe, expect, it } from 'vitest'
import { configureRequest } from '~/request'
import { useUserDetail } from './use-user-detail'

function json(status: number, body: object, contentType = 'application/json'): Response {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': contentType } })
}

const user = { id: 'u-1', userName: 'bob', displayName: 'Bob', email: null, phoneNumber: null, homeTenantId: 'acme', status: 'Active', lastLoginAt: null, createdAt: '2026-10-28T00:00:00Z', createdBy: null }
function role(id: string, code: string) {
  return { id, ownerTenantId: 'acme', code, name: code, description: null, order: 0, isBuiltIn: false, status: 'Active', dataScope: 'SelfOnly', createdAt: '2026-10-28T00:00:00Z' }
}

describe('useUserDetail', () => {
  it('loads the user with roles and memberships, saves role changes as a diff, adds a department and locks the user', async () => {
    const calls: { method: string, url: string, body: unknown }[] = []
    let status = 'Active'
    configureRequest({
      baseUrl: '',
      getToken: () => 't',
      fetch: (async (url: RequestInfo | URL, init?: RequestInit) => {
        const path = String(url)
        const body = init?.body ? JSON.parse(String(init.body)) : undefined
        calls.push({ method: init?.method ?? 'GET', url: path, body })
        if (path.endsWith('/api/users/u-1'))
          return json(200, { ...user, status })
        if (path.endsWith('/api/roles:list'))
          return json(200, { items: [role('r-1', 'viewer'), role('r-2', 'lead')], total: 2, page: { index: 1, size: 100 } })
        if (path.endsWith('/api/roles:by-user?userId=u-1'))
          return json(200, { items: [role('r-1', 'viewer')] })
        if (path.endsWith('/api/roles/r-2:assign') || path.endsWith('/api/roles/r-1:unassign'))
          return json(200, { succeeded: body.userIds, failed: [] })
        if (path.endsWith('/api/memberships:list')) {
          return json(200, { items: [
            { id: 'm-1', userId: 'u-1', userName: 'bob', displayName: 'Bob', departmentId: 'd-1', departmentName: 'HQ', positionId: null, positionName: null, isPrimary: true, employeeNumber: null, jobGrade: null, hiredAt: null, order: 0, createdAt: '2026-10-28T00:00:00Z' },
          ], total: 1, page: { index: 1, size: 100 } })
        }
        if (path.endsWith('/api/memberships') && init?.method === 'POST')
          return json(201, { id: 'm-2', userId: 'u-1', userName: 'bob', displayName: 'Bob', departmentId: body.departmentId, departmentName: 'R&D', positionId: body.positionId, positionName: null, isPrimary: false, employeeNumber: null, jobGrade: null, hiredAt: null, order: 0, createdAt: '2026-10-28T00:00:00Z' })
        if (path.endsWith('/api/users:lock')) {
          status = 'Locked'
          return json(200, { succeeded: body.ids, failed: [] })
        }
        return json(404, { code: 'PROTOCOLS_NOTFOUND_000', status: 404, detail: path }, 'application/problem+json')
      }) as unknown as typeof fetch,
    })

    const detail = useUserDetail()
    await detail.select('u-1', { roles: true, departments: true })
    expect(detail.user.value?.userName).toBe('bob')
    expect(detail.roleOptions.value.map(r => r.code)).toEqual(['viewer', 'lead'])
    expect([...detail.assigned.value]).toEqual(['r-1'])
    expect(detail.memberships.value.map(m => m.departmentName)).toEqual(['HQ'])
    expect(detail.rolesDirty.value).toBe(false)

    detail.toggleRole('r-2')
    detail.toggleRole('r-1')
    expect(detail.rolesDirty.value).toBe(true)
    expect(await detail.saveRoles()).toBe(true)
    expect(calls.find(c => c.url.endsWith('/api/roles/r-2:assign'))?.body).toEqual({ userIds: ['u-1'] })
    expect(calls.find(c => c.url.endsWith('/api/roles/r-1:unassign'))?.body).toEqual({ userIds: ['u-1'] })
    expect(detail.notice.value).toBe('roles_saved')

    detail.memberForm.departmentId = 'd-2'
    expect(await detail.addDepartment()).toBe(true)
    expect(calls.find(c => c.method === 'POST' && c.url.endsWith('/api/memberships'))?.body).toEqual({ userId: 'u-1', departmentId: 'd-2', positionId: null })
    expect(detail.notice.value).toBe('department_added')

    expect(await detail.lock()).toBe(true)
    expect(calls.find(c => c.url.endsWith('/api/users:lock'))?.body).toEqual({ ids: ['u-1'] })
    expect(detail.user.value?.status).toBe('Locked')
    expect(detail.notice.value).toBe('locked')
  })

  it('hands password reset validation errors to the field', async () => {
    configureRequest({
      baseUrl: '',
      getToken: () => 't',
      fetch: (async (url: RequestInfo | URL) => {
        if (String(url).endsWith('/api/users/u-1'))
          return json(200, user)
        return json(400, { code: 'KERNEL_VALIDATION_003', status: 400, detail: 'invalid', traceId: 'x', errors: { newPassword: ['too short'] } }, 'application/problem+json')
      }) as unknown as typeof fetch,
    })
    const detail = useUserDetail()
    await detail.select('u-1', { roles: false, departments: false })
    detail.resetForm.newPassword = 'short'
    expect(await detail.resetPassword()).toBe(false)
    expect(detail.resetError('newPassword')).toBe('too short')
    expect(detail.error.value).toBeNull()
  })
})
