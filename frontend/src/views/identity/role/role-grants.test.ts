import type { PermissionListItemDto, RolePermissionListItemDto } from '@/api'
import { describe, expect, it } from 'vitest'
import { ValidityStatus } from '@/api'
import { diffMenuGrants, mergeGrantedIntoCatalog } from './role-grants'

function grant(basicId: string, permissionId: string, status = ValidityStatus.Valid): RolePermissionListItemDto {
  return { basicId, permissionId, status, permissionCode: `saas:${permissionId}:read`, permissionName: permissionId.toUpperCase() } as RolePermissionListItemDto
}

function permission(basicId: string): PermissionListItemDto {
  return { basicId, permissionCode: `saas:${basicId}:read`, permissionName: basicId.toUpperCase() } as PermissionListItemDto
}

/** 菜单 m1→p1、m2→p2；p9 没挂任何菜单（撤销类/接口类权限） */
const menuPermIdById = new Map([['m1', 'p1'], ['m2', 'p2']])

describe('diffMenuGrants', () => {
  it('没挂菜单的已授权限不在菜单树的管辖内，保存菜单授权不能顺手收回', () => {
    const grants = [grant('g1', 'p1'), grant('g9', 'p9')]

    expect(diffMenuGrants(['m1'], menuPermIdById, grants)).toEqual({ grantPermissionIds: [], revokeRolePermissionIds: [] })
  })

  it('勾上的菜单授予其权限，取消勾选的菜单收回其权限', () => {
    const grants = [grant('g1', 'p1'), grant('g9', 'p9')]

    expect(diffMenuGrants(['m2'], menuPermIdById, grants)).toEqual({ grantPermissionIds: ['p2'], revokeRolePermissionIds: ['g1'] })
  })

  it('已撤销（失效）的历史行不算已授权，也不重复撤销', () => {
    const grants = [grant('g1', 'p1', ValidityStatus.Invalid)]

    expect(diffMenuGrants(['m1'], menuPermIdById, grants)).toEqual({ grantPermissionIds: ['p1'], revokeRolePermissionIds: [] })
    expect(diffMenuGrants([], menuPermIdById, grants)).toEqual({ grantPermissionIds: [], revokeRolePermissionIds: [] })
  })

  it('两个菜单共用一个权限时，只要还有一个勾着就不收回', () => {
    const shared = new Map([['m1', 'p1'], ['m3', 'p1']])

    expect(diffMenuGrants(['m3'], shared, [grant('g1', 'p1')])).toEqual({ grantPermissionIds: [], revokeRolePermissionIds: [] })
  })
})

describe('mergeGrantedIntoCatalog', () => {
  it('目录外的有效授权补进条目，失效的历史行不补', () => {
    const items = mergeGrantedIntoCatalog(
      [permission('p1')],
      [grant('g1', 'p1'), grant('g8', 'p8'), grant('g7', 'p7', ValidityStatus.Invalid)],
    )

    expect(items.map(item => item.basicId)).toEqual(['p1', 'p8'])
    expect(items[1]).toMatchObject({ basicId: 'p8', permissionName: 'P8' })
  })
})
