import type { PermissionRequestRow } from '@/modules/access/views/delegations/use-permission-requests'
import { mount } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it } from 'vitest'
import PermissionRequestApproval from '@/modules/access/views/permission-request-approval/index.vue'
import { i18n } from '~/locales'

const originalLocale = i18n.global.locale.value

beforeEach(() => {
  i18n.global.locale.value = 'zh-CN'
})

afterEach(() => {
  i18n.global.locale.value = originalLocale
})

function data(extra: Partial<PermissionRequestRow> = {}): PermissionRequestRow {
  return {
    id: 'pr-1',
    requestUserId: 'u-1',
    requestUserName: 'Alice',
    roleId: 'r-1',
    roleCode: 'approver',
    roleName: '审批员',
    permissionCode: null,
    permissionName: null,
    status: 'Pending',
    reason: '月底结账',
    expectedEffectiveAt: null,
    expectedExpiresAt: '2026-10-01T00:00:00Z',
    isExpectedExpired: false,
    workflowInstanceId: 'wi-1',
    workflow: null,
    approvalEnabled: true,
    remark: null,
    createdAt: '2026-09-01T00:00:00Z',
    ...extra,
  }
}

function render(row: PermissionRequestRow) {
  return mount(PermissionRequestApproval, {
    props: {
      businessId: row.id,
      data: row,
      mode: 'approve',
      nodeId: 'admin-approve',
    },
    global: {
      plugins: [i18n],
    },
  })
}

describe('授权申请单据（审批中心里的只读单据）', () => {
  it('写出申请人、状态、申请对象（名字带编码）与理由，没有备注不出备注', () => {
    const text = render(data()).text()
    expect(text).toContain('Alice')
    expect(text).toContain('待审批')
    expect(text).toContain('角色')
    expect(text).toContain('审批员 (approver)')
    expect(text).toContain('月底结账')
    expect(text).not.toContain('备注')
    expect(text).not.toContain('已过期')
  })

  it('同时申请角色与权限时两样都列出，期望失效时间已过标「已过期」，有备注出备注', () => {
    const text = render(data({
      permissionCode: 'identity.users.read',
      permissionName: '读取用户',
      isExpectedExpired: true,
      remark: '加急',
    })).text()
    expect(text).toContain('审批员 (approver)')
    expect(text).toContain('读取用户 (identity.users.read)')
    expect(text).toContain('已过期')
    expect(text).toContain('加急')
  })
})
