import type { PermissionRequestRow } from './use-permission-requests'
import { describe, expect, it } from 'vitest'
import { configureRequest } from '~/request'
import { resolveView } from '~/router/views'
import { QueryOperator } from '~/types/contracts'
import {
  canClose,
  canResubmit,
  canTrace,
  canWithdraw,
  permissionRequestsResource,
  requestName,
  requestTarget,
  toCreateRequest,
  toForm,
  toUpdateRequest,
  usePermissionRequestCloser,
  usePermissionRequestEditor,
  withdrawPermissionRequest,
} from './use-permission-requests'

function json(status: number, body: object, contentType = 'application/json'): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': contentType },
  })
}

interface Call { method: string, url: string, body: unknown }

function record(respond: (call: Call) => Response): Call[] {
  const calls: Call[] = []
  configureRequest({
    baseUrl: '',
    getToken: () => 't',
    fetch: (async (url: RequestInfo | URL, init?: RequestInit) => {
      const call = {
        method: init?.method ?? 'GET',
        url: String(url),
        body: init?.body
          ? JSON.parse(String(init.body))
          : undefined,
      }
      calls.push(call)
      return respond(call)
    }) as unknown as typeof fetch,
  })
  return calls
}

function request(extra: Partial<PermissionRequestRow> = {}): PermissionRequestRow {
  return {
    id: 'pr-1',
    requestUserId: 'u-1',
    requestUserName: 'Alice',
    roleId: 'r-1',
    roleCode: 'approver',
    roleName: 'Approver',
    permissionCode: null,
    permissionName: null,
    status: 'Pending',
    reason: '月底结账',
    expectedEffectiveAt: null,
    expectedExpiresAt: '2026-12-01T00:00:00Z',
    isExpectedExpired: false,
    workflowInstanceId: 'wi-1',
    workflow: null,
    approvalEnabled: true,
    remark: null,
    createdAt: '2026-11-01T00:00:00Z',
    ...extra,
  }
}

describe('permissionRequestsResource', () => {
  it('关键字与状态多选按后端白名单下发，排序带上', async () => {
    const calls = record(() => json(200, {
      items: [
        request(),
      ],
      total: 1,
      page: { index: 1, size: 20 },
    }))
    const result = await permissionRequestsResource().page!({
      page: 1,
      pageSize: 20,
      sorts: [
        { field: 'createdAt', order: 'desc' },
      ],
      filters: { keyword: ' 结账 ' },
      conditionFilters: [
        {
          field: 'status',
          operator: QueryOperator.In,
          values: ['Pending', 'Approved'],
        },
      ],
    })
    expect(calls[0]).toMatchObject({ method: 'POST', url: '/api/permission-requests:list' })
    expect(calls[0]!.body).toMatchObject({
      keyword: '结账',
      filters: [
        { field: 'status', operator: 'in', value: 'Pending,Approved' },
      ],
      sorts: [
        { field: 'createdAt', descending: true },
      ],
    })
    expect(result.items.map(r => r.id)).toEqual(['pr-1'])
  })
})

describe('授权申请的显示与可用操作', () => {
  it('申请对象列出角色与权限（后端给的显示名，缺了用编码）', () => {
    expect(requestTarget(request())).toBe('Approver')
    expect(requestTarget(request({
      roleId: null,
      roleCode: null,
      roleName: null,
      permissionCode: 'identity.users.read',
      permissionName: '读取用户',
    }))).toBe('读取用户')
    expect(requestTarget(request({ roleName: null, permissionCode: 'custom.thing' }))).toBe('approver / custom.thing')
    expect(requestTarget(request({ roleId: null, roleCode: null, roleName: null }))).toBe('-')
    expect(requestName(request())).toBe('Alice · Approver')
    expect(requestName(request({ requestUserName: null }))).toBe('u-1 · Approver')
  })

  it('自己待审批的能撤回与修改重提，别人待审批的能手工处理，结束了的都不能', () => {
    expect(canWithdraw(request(), 'u-1')).toBe(true)
    expect(canResubmit(request(), 'u-1')).toBe(true)
    expect(canClose(request(), 'u-1')).toBe(false)
    expect(canWithdraw(request(), 'u-2')).toBe(false)
    expect(canResubmit(request(), 'u-2')).toBe(false)
    expect(canClose(request(), 'u-2')).toBe(true)
    for (const status of ['Approved', 'Rejected', 'Withdrawn'] as const) {
      expect(canWithdraw(request({ status }), 'u-1')).toBe(false)
      expect(canResubmit(request({ status }), 'u-1')).toBe(false)
      expect(canClose(request({ status }), 'u-2')).toBe(false)
    }
    expect(canClose(request(), null)).toBe(false)
  })

  it('查看审批要审批启用且发起过流程', () => {
    expect(canTrace(request())).toBe(true)
    expect(canTrace(request({ workflowInstanceId: null }))).toBe(false)
    expect(canTrace(request({ approvalEnabled: false }))).toBe(false)
  })
})

describe('申请表单与请求', () => {
  it('只带当前申请类型的对象，时间换成 ISO，空备注不传', () => {
    const at = Date.UTC(2026, 11, 1)
    const form = {
      targetKind: 'permission' as const,
      roleId: 'r-1',
      permissionCode: 'identity.users.read',
      expectedEffectiveAt: null,
      expectedExpiresAt: at,
      reason: ' 月底结账 ',
      remark: '  ',
    }
    expect(toCreateRequest(form)).toEqual({
      roleId: null,
      permissionCode: 'identity.users.read',
      reason: '月底结账',
      expectedEffectiveAt: null,
      expectedExpiresAt: '2026-12-01T00:00:00.000Z',
      remark: null,
    })
    expect(toCreateRequest({
      ...form,
      targetKind: 'role',
      expectedEffectiveAt: at,
      remark: '加急',
    })).toMatchObject({
      roleId: 'r-1',
      permissionCode: null,
      expectedEffectiveAt: '2026-12-01T00:00:00.000Z',
      remark: '加急',
    })
    expect(toUpdateRequest('pr-1', form)).toMatchObject({ id: 'pr-1', permissionCode: 'identity.users.read' })
  })

  it('修改重提按原申请回填', () => {
    expect(toForm(request({ remark: '加急' }))).toEqual({
      targetKind: 'role',
      roleId: 'r-1',
      permissionCode: '',
      expectedEffectiveAt: null,
      expectedExpiresAt: Date.UTC(2026, 11, 1),
      reason: '月底结账',
      remark: '加急',
    })
    expect(toForm(request({
      roleId: null,
      roleCode: null,
      roleName: null,
      permissionCode: 'identity.users.read',
    }))).toMatchObject({ targetKind: 'permission', roleId: '', permissionCode: 'identity.users.read' })
  })
})

describe('usePermissionRequestEditor', () => {
  it('打开即取启用的角色与权限目录；发起走 create，审批未启用的 503 留在弹窗里', async () => {
    const calls = record((call) => {
      if (call.url === '/api/roles:list') {
        return json(200, {
          items: [
            {
              id: 'r-1',
              code: 'approver',
              name: 'Approver',
              isBuiltIn: false,
              status: 'Active',
            },
            {
              id: 'r-0',
              code: 'admin',
              name: 'Admin',
              isBuiltIn: true,
              status: 'Active',
            },
          ],
          total: 2,
          page: { index: 1, size: 500 },
        })
      }
      if (call.url === '/api/permissions') {
        return json(200, {
          items: [
            {
              code: 'identity.users.read',
              displayName: 'Read users',
              description: null,
              package: 'identity',
              resource: 'users',
              action: 'read',
              scope: 'Both',
              audited: false,
            },
          ],
        })
      }
      return json(503, {
        code: 'ACCESS_UNAVAILABLE_002',
        status: 503,
        detail: 'approval not enabled',
        traceId: 'x',
      }, 'application/problem+json')
    })
    const editor = usePermissionRequestEditor()
    await editor.start()
    expect(editor.open.value).toBe(true)
    expect(editor.editingId.value).toBeNull()
    expect(editor.roleOptions.value.map(o => o.value)).toEqual(['r-1', 'r-0'])
    expect(editor.permissionOptions.value).toEqual([
      { value: 'identity.users.read', label: 'Read users', description: 'identity.users.read' },
    ])
    expect(calls.find(c => c.url === '/api/roles:list')?.body).toMatchObject({
      filters: [
        { field: 'status', operator: 'eq', value: 'Active' },
      ],
    })

    Object.assign(editor.form, { roleId: 'r-1', reason: '月底结账' })
    expect(await editor.submit()).toBe(false)
    expect(calls.at(-1)).toMatchObject({ method: 'POST', url: '/api/permission-requests' })
    expect(editor.submitError.value).toBe('approval not enabled')
    expect(editor.open.value).toBe(true)
  })

  it('带申请打开即修改重提：回填并走 update，字段错误挂回字段', async () => {
    const calls = record((call) => {
      if (call.url === '/api/roles:list') {
        return json(200, {
          items: [],
          total: 0,
          page: { index: 1, size: 500 },
        })
      }
      if (call.url === '/api/permissions')
        return json(200, { items: [] })
      return json(400, {
        code: 'ACCESS_VALIDATION_001',
        status: 400,
        detail: 'invalid',
        traceId: 'x',
        errors: {
          expectedExpiresAt: ['too early'],
        },
      }, 'application/problem+json')
    })
    const editor = usePermissionRequestEditor()
    await editor.start(request())
    expect(editor.editingId.value).toBe('pr-1')
    expect(editor.form).toMatchObject({ targetKind: 'role', roleId: 'r-1', reason: '月底结账' })
    expect(await editor.submit()).toBe(false)
    expect(calls.at(-1)).toMatchObject({ method: 'PUT', url: '/api/permission-requests/pr-1' })
    // ID 走路径（客户端把路径参数从请求体里拿掉），请求体是新内容
    expect(calls.at(-1)!.body).toMatchObject({
      roleId: 'r-1',
      permissionCode: null,
      reason: '月底结账',
      expectedExpiresAt: '2026-12-01T00:00:00.000Z',
    })
    expect(editor.fieldError('expectedExpiresAt')).toBe('too early')
    expect(editor.submitError.value).toBeNull()
  })
})

describe('撤回与手工处理', () => {
  it('批量接口传单个 ID，逐项失败即报错', async () => {
    const calls = record((call) => {
      if (call.url === '/api/permission-requests:withdraw') {
        return json(200, {
          succeeded: [],
          failed: [
            { id: 'pr-1', code: 'ACCESS_CONFLICT_014', message: 'ACCESS_CONFLICT_014 的原因' },
          ],
        })
      }
      return json(200, {
        succeeded: ['pr-1'],
        failed: [],
      })
    })
    await expect(withdrawPermissionRequest('pr-1')).rejects.toThrow('ACCESS_CONFLICT_014 的原因')
    expect(calls[0]!.body).toEqual({
      ids: ['pr-1'],
    })

    const closer = usePermissionRequestCloser()
    closer.start(request())
    expect(closer.status.value).toBe('Rejected')
    closer.status.value = 'Withdrawn'
    closer.remark.value = '  重复申请  '
    expect(await closer.submit()).toBe(true)
    expect(calls.at(-1)).toMatchObject({
      method: 'POST',
      url: '/api/permission-requests:set-status',
      body: {
        ids: ['pr-1'],
        status: 'Withdrawn',
        remark: '重复申请',
      },
    })
    expect(closer.open.value).toBe(false)
  })
})

describe('审批单据组件', () => {
  it('审批业务登记的组件路径能解析到页面表里的单据组件（设计稿 D4）', () => {
    expect(() => resolveView('access/permission-request-approval/index')).not.toThrow()
  })
})
