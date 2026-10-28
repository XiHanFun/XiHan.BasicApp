import { describe, expect, it } from 'vitest'
import { configureRequest } from '~/request'
import { flattenDepartments, useUserDepartments } from './use-user-departments'

function json(status: number, body: object, contentType = 'application/json'): Response {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': contentType } })
}

const bob = { id: 'u-1', userName: 'bob', displayName: 'Bob', email: null, phoneNumber: null, homeTenantId: 'acme', status: 'Active', lastLoginAt: null, createdAt: '2026-10-28T00:00:00Z', createdBy: null }

function department(id: string, name: string, children: unknown[] = []) {
  return { department: { id, code: id, name, parentId: null, leaderUserId: null, status: 'Active', order: 0, description: null, createdAt: '2026-10-28T00:00:00Z' }, memberCount: 0, children }
}

function membership(id: string, departmentId: string, departmentName: string, extra: Record<string, unknown> = {}) {
  return { id, userId: 'u-1', userName: 'bob', displayName: 'Bob', departmentId, departmentName, positionId: null, positionName: null, isPrimary: false, employeeNumber: null, jobGrade: null, hiredAt: null, order: 0, createdAt: '2026-10-28T00:00:00Z', ...extra }
}

interface Call { method: string, url: string, body: Record<string, unknown> | undefined }

/** 带状态的假后端：归属按请求增删；createError / deleteFailure 模拟被拒 */
function backend(options: { createError?: Response, deleteFailure?: string } = {}) {
  const rows = [membership('m-1', 'd-1', 'HQ', { isPrimary: true })]
  const calls: Call[] = []
  configureRequest({
    baseUrl: '',
    getToken: () => 't',
    fetch: (async (url: RequestInfo | URL, init?: RequestInit) => {
      const path = String(url)
      const method = init?.method ?? 'GET'
      const body = init?.body ? JSON.parse(String(init.body)) : undefined
      calls.push({ method, url: path, body })
      if (path.endsWith('/api/memberships:list'))
        return json(200, { items: rows, total: rows.length, page: { index: 1, size: 100 } })
      if (path.endsWith('/api/departments:tree'))
        return json(200, { items: [department('d-1', 'HQ', [department('d-2', 'R&D', [department('d-3', 'Web')])])], total: 3 })
      if (path.endsWith('/api/positions:list'))
        return json(200, { items: [{ id: 'p-1', code: 'dev', name: 'Developer', status: 'Active', order: 0, description: null, createdAt: '2026-10-28T00:00:00Z' }], total: 1, page: { index: 1, size: 100 } })
      if (path.endsWith('/api/memberships') && method === 'POST') {
        if (options.createError)
          return options.createError
        rows.push(membership('m-2', body.departmentId, 'R&D', { positionId: body.positionId }))
        return json(201, rows.at(-1)!)
      }
      if (path.startsWith('/api/memberships?') && method === 'DELETE') {
        if (options.deleteFailure)
          return json(200, { succeeded: [], failed: [{ id: 'm-1', code: options.deleteFailure }] })
        rows.splice(rows.findIndex(row => path.endsWith(`ids=${row.id}`)), 1)
        return json(200, { succeeded: ['m-1'], failed: [] })
      }
      return json(404, { code: 'PROTOCOLS_NOTFOUND_000', status: 404, detail: path }, 'application/problem+json')
    }) as unknown as typeof fetch,
  })
  return calls
}

describe('flattenDepartments', () => {
  it('部门树展平成下拉选项，层级用全角空格缩进', () => {
    expect(flattenDepartments([department('d-1', 'HQ', [department('d-2', 'R&D', [department('d-3', 'Web')])])] as never)).toEqual([
      { label: 'HQ', value: 'd-1' },
      { label: '　R&D', value: 'd-2' },
      { label: '　　Web', value: 'd-3' },
    ])
  })
})

describe('useUserDepartments', () => {
  it('打开时装载归属与候选（部门树、启用的岗位）；加入部门带岗位提交，随后重取归属', async () => {
    const calls = backend()
    const departments = useUserDepartments()
    await departments.start(bob as never, { departments: true, positions: true })
    expect(departments.open.value).toBe(true)
    expect(departments.loading.value).toBe(false)
    expect(departments.memberships.value.map(m => m.departmentName)).toEqual(['HQ'])
    expect(departments.departmentOptions.value.map(o => o.value)).toEqual(['d-1', 'd-2', 'd-3'])
    expect(departments.positionOptions.value).toEqual([{ label: 'Developer', value: 'p-1' }])
    expect(calls.find(c => c.url.endsWith('/api/positions:list'))?.body).toMatchObject({ filters: [{ field: 'status', operator: 'eq', value: 'Active' }] })

    departments.form.departmentId = 'd-2'
    departments.form.positionId = 'p-1'
    expect(await departments.add()).toBe(true)
    expect(calls.find(c => c.method === 'POST' && c.url.endsWith('/api/memberships'))?.body).toEqual({ userId: 'u-1', departmentId: 'd-2', positionId: 'p-1' })
    expect(departments.memberships.value.map(m => m.departmentName)).toEqual(['HQ', 'R&D'])
    expect(departments.form).toEqual({ departmentId: null, positionId: null })
  })

  it('没有候选权限时不去取部门树与岗位；没选部门时不提交', async () => {
    const calls = backend()
    const departments = useUserDepartments()
    await departments.start(bob as never, { departments: false, positions: false })
    expect(calls.map(c => c.url)).toEqual(['/api/memberships:list'])
    expect(departments.departmentOptions.value).toEqual([])
    expect(await departments.add()).toBe(false)
    expect(calls).toHaveLength(1)
  })

  it('加入部门的字段级错误挂回字段并返回 false；其余失败抛出', async () => {
    backend({ createError: json(400, { code: 'KERNEL_VALIDATION_003', status: 400, detail: 'invalid', traceId: 'x', errors: { departmentId: ['department disabled'] } }, 'application/problem+json') })
    const departments = useUserDepartments()
    await departments.start(bob as never, { departments: true, positions: false })
    departments.form.departmentId = 'd-3'
    expect(await departments.add()).toBe(false)
    expect(departments.fieldError('departmentId')).toBe('department disabled')
    expect(departments.form.departmentId).toBe('d-3')

    backend({ createError: json(409, { code: 'ORGANIZATION_CONFLICT_002', status: 409, detail: 'already a member', traceId: 'x' }, 'application/problem+json') })
    await expect(departments.add()).rejects.toThrow('already a member')
    expect(departments.working.value).toBe(false)
  })

  it('移出部门走 memberships.delete；被拒时带着错误码抛出，归属照样重取', async () => {
    let calls = backend()
    const departments = useUserDepartments()
    await departments.start(bob as never, { departments: false, positions: false })
    await departments.remove('m-1')
    expect(calls.find(c => c.method === 'DELETE')?.url).toBe('/api/memberships?ids=m-1')
    expect(departments.memberships.value).toEqual([])

    calls = backend({ deleteFailure: 'ORGANIZATION_VALIDATION_007' })
    await departments.start(bob as never, { departments: false, positions: false })
    await expect(departments.remove('m-1')).rejects.toThrow('ORGANIZATION_VALIDATION_007')
    expect(calls.filter(c => c.url.endsWith('/api/memberships:list'))).toHaveLength(2)
    expect(departments.memberships.value.map(m => m.id)).toEqual(['m-1'])
  })
})
