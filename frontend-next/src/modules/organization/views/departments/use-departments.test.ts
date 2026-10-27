import { describe, expect, it } from 'vitest'
import { configureRequest } from '~/request'
import { useDepartments } from './use-departments'

function json(status: number, body: object, contentType = 'application/json'): Response {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': contentType } })
}

function department(id: string, code: string, name: string, parentId: string | null, order: number) {
  return { id, code, name, parentId, leaderUserId: null, status: 'Active', order, description: null, createdAt: '2026-10-28T00:00:00Z' }
}

const hq = department('d-1', 'hq', 'Headquarters', null, 0)
const rd = department('d-2', 'rd', 'R&D', 'd-1', 10)
const mkt = department('d-3', 'mkt', 'Marketing', 'd-1', 20)

describe('useDepartments', () => {
  it('flattens the tree with depth, selects a department with its members, adds a member and moves it to the top level', async () => {
    const calls: { method: string, url: string, body: unknown }[] = []
    configureRequest({
      baseUrl: '',
      getToken: () => 't',
      fetch: (async (url: RequestInfo | URL, init?: RequestInit) => {
        const path = String(url)
        const body = init?.body ? JSON.parse(String(init.body)) : undefined
        calls.push({ method: init?.method ?? 'GET', url: path, body })
        if (path.endsWith('/api/departments:tree')) {
          return json(200, { items: [{ department: hq, memberCount: 1, children: [
            { department: rd, memberCount: 2, children: [] },
            { department: mkt, memberCount: 0, children: [] },
          ] }], total: 3 })
        }
        if (path.endsWith('/api/departments/d-2') && (init?.method ?? 'GET') === 'GET')
          return json(200, { department: rd, ancestors: [hq], leaderUserName: null, childCount: 0, memberCount: 2 })
        if (path.endsWith('/api/memberships:list')) {
          return json(200, { items: [
            { id: 'm-1', userId: 'u-1', userName: 'bob', displayName: 'Bob', departmentId: 'd-2', departmentName: 'R&D', positionId: 'p-1', positionName: 'Manager', isPrimary: true, employeeNumber: '0001', jobGrade: null, hiredAt: null, order: 0, createdAt: '2026-10-28T00:00:00Z' },
          ], total: 1, page: { index: 1, size: 100 } })
        }
        if (path.endsWith('/api/memberships') && init?.method === 'POST')
          return json(201, { id: 'm-2', userId: body.userId, userName: 'carol', displayName: 'Carol', departmentId: body.departmentId, departmentName: 'R&D', positionId: body.positionId, positionName: null, isPrimary: true, employeeNumber: body.employeeNumber, jobGrade: null, hiredAt: null, order: 0, createdAt: '2026-10-28T00:00:00Z' })
        if (path.endsWith('/api/departments/d-2:move'))
          return json(200, { ...rd, parentId: body.parentId })
        return json(404, { code: 'PROTOCOLS_NOTFOUND_000', status: 404, detail: path }, 'application/problem+json')
      }) as unknown as typeof fetch,
    })

    const page = useDepartments()
    await page.load()
    expect(page.flat.value.map(r => `${r.depth}:${r.code}:${r.memberCount}`)).toEqual(['0:hq:1', '1:rd:2', '1:mkt:0'])

    await page.select('d-2')
    expect(page.selected.value?.ancestors.map(a => a.code)).toEqual(['hq'])
    expect(page.edit.name).toBe('R&D')
    expect(page.moveTarget.value).toBe('d-1')
    expect(page.moveOptions.value.map(r => r.code)).toEqual(['hq', 'mkt'])
    expect(page.members.value.map(m => m.userName)).toEqual(['bob'])
    const list = calls.find(c => c.url.endsWith('/api/memberships:list'))
    expect(list?.body).toMatchObject({ filters: [{ field: 'departmentId', operator: 'eq', value: 'd-2' }] })

    page.memberForm.userId = 'u-2'
    page.memberForm.employeeNumber = '0002'
    expect(await page.addMember()).toBe(true)
    expect(calls.find(c => c.method === 'POST' && c.url.endsWith('/api/memberships'))?.body).toEqual({ userId: 'u-2', departmentId: 'd-2', positionId: null, isPrimary: false, employeeNumber: '0002' })
    expect(page.notice.value).toBe('member_added')

    page.moveTarget.value = ''
    expect(await page.move()).toBe(true)
    expect(calls.find(c => c.url.endsWith(':move'))?.body).toEqual({ parentId: null })
    expect(page.notice.value).toBe('moved')
  })

  it('hands creation validation errors to the fields', async () => {
    configureRequest({
      baseUrl: '',
      getToken: () => 't',
      fetch: (async () => json(400, { code: 'KERNEL_VALIDATION_003', status: 400, detail: 'invalid', traceId: 'x', errors: { code: ['pattern'] } }, 'application/problem+json')) as unknown as typeof fetch,
    })
    const page = useDepartments()
    page.form.code = 'bad code!'
    page.form.name = 'x'
    expect(await page.submit()).toBe(false)
    expect(page.fieldError('code')).toBe('pattern')
    expect(page.submitError.value).toBeNull()
  })
})
