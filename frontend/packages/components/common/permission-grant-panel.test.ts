import type { PermissionGrantItem } from './permission-grant-panel'
import { describe, expect, it } from 'vitest'
import { groupPermissions } from './permission-grant-panel'

function item(basicId: string, permissionCode: string, permissionName: string, groupCode: string, groupName = ''): PermissionGrantItem {
  return { basicId, permissionCode, permissionName, groupCode, groupName }
}

describe('groupPermissions', () => {
  it('后端给了组名就用组名', () => {
    const groups = groupPermissions([item('1', 'saas:tenant:read', '租户查看', 'tenant', '租户')])

    expect(groups.map(group => group.name)).toEqual(['租户'])
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
