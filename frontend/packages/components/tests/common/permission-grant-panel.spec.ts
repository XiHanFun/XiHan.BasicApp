import type { PermissionGrantItem } from '~/components/common/permission-grant-panel'
import { describe, expect, it } from 'vitest'
import { groupPermissions } from '~/components/common/permission-grant-panel'

function item(basicId: string, permissionCode: string, permissionName: string, groupCode: string, groupName = ''): PermissionGrantItem {
  return {
    basicId,
    permissionCode,
    permissionName,
    groupCode,
    groupName,
  }
}

describe('groupPermissions', () => {
  it('后端给了组名就用组名，组码作为次要信息', () => {
    const groups = groupPermissions([
      item('1', 'access.permission-requests.create', '提交授权申请', 'access.permission-requests', '授权申请'),
      item('2', 'access.permission-requests.withdraw', '撤回授权申请', 'access.permission-requests', '授权申请'),
    ])

    expect(groups.map(group => [group.key, group.name, group.description])).toEqual([
      ['access.permission-requests', '授权申请', 'access.permission-requests'],
    ])
  })

  it('组名就是组码（目录外、没有显示名）时不重复显示组码', () => {
    const groups = groupPermissions([
      item('1', 'custom.unknown.read', 'custom.unknown.read', 'custom.unknown', 'custom.unknown'),
    ])

    expect(groups.map(group => [group.name, group.description])).toEqual([
      ['custom.unknown', undefined],
    ])
  })

  it('没有组名时按组内权限名的公共前缀命名，不显示组码', () => {
    const groups = groupPermissions([
      item('1', 'chat:read', '聊天查看', 'chat'),
      item('2', 'chat:send', '聊天发送', 'chat'),
      item('3', 'chat:manage', '聊天会话管理', 'chat'),
      item('4', 'print-template:read', '打印模板查看', 'print-template'),
      item('5', 'print-template:use', '打印模板使用', 'print-template'),
    ])

    expect(groups.map(group => [group.key, group.name])).toEqual([
      ['chat', '聊天'],
      ['print-template', '打印模板'],
    ])
  })
})
