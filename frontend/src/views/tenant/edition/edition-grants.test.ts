import type { PermissionListItemDto, TenantEditionPermissionListItemDto } from '@/api'
import { describe, expect, it } from 'vitest'
import { ValidityStatus } from '@/api'
import { diffEditionGrants, isEmptyEditionGrantDiff, mergeMappedIntoCatalog, validEditionPermissionIds } from './edition-grants'

function mapping(basicId: string, permissionId: string, status = ValidityStatus.Valid): TenantEditionPermissionListItemDto {
  return { basicId, permissionId, status, permissionCode: `saas:${permissionId}:read`, permissionName: permissionId.toUpperCase() } as TenantEditionPermissionListItemDto
}

function permission(basicId: string): PermissionListItemDto {
  return { basicId, permissionCode: `saas:${basicId}:read`, permissionName: basicId.toUpperCase() } as PermissionListItemDto
}

describe('validEditionPermissionIds', () => {
  it('右栏只放生效中的映射，停用的不在白名单里', () => {
    expect(validEditionPermissionIds([mapping('e1', 'p1'), mapping('e2', 'p2', ValidityStatus.Invalid)])).toEqual(['p1'])
  })
})

describe('diffEditionGrants', () => {
  const mappings = [mapping('e1', 'p1'), mapping('e2', 'p2', ValidityStatus.Invalid)]

  it('右栏与生效中的映射一致时没有变更', () => {
    const diff = diffEditionGrants(['p1'], mappings)

    expect(diff).toEqual({ grantPermissionIds: [], revokeEditionPermissionIds: [], statusChanges: [] })
    expect(isEmptyEditionGrantDiff(diff)).toBe(true)
  })

  it('没有映射的移到右栏是授予，生效的移回左栏是撤销', () => {
    expect(diffEditionGrants(['p3'], mappings)).toEqual({
      grantPermissionIds: ['p3'],
      revokeEditionPermissionIds: ['e1'],
      statusChanges: [],
    })
  })

  it('停用的移到右栏改回有效，不再重复授予', () => {
    const diff = diffEditionGrants(['p1', 'p2'], mappings)

    expect(diff).toEqual({
      grantPermissionIds: [],
      revokeEditionPermissionIds: [],
      statusChanges: [{ basicId: 'e2', status: ValidityStatus.Valid }],
    })
    expect(isEmptyEditionGrantDiff(diff)).toBe(false)
  })

  it('停用的留在左栏不动，不会被顺手撤销', () => {
    expect(diffEditionGrants([], mappings).revokeEditionPermissionIds).toEqual(['e1'])
  })
})

describe('mergeMappedIntoCatalog', () => {
  it('生效中却不在目录里的映射补进条目，停用的不补', () => {
    const outside = mapping('e8', 'p8')
    const items = mergeMappedIntoCatalog(
      [permission('p1')],
      [mapping('e1', 'p1'), outside, mapping('e7', 'p7', ValidityStatus.Invalid)],
    )

    expect(items.map(item => item.basicId)).toEqual(['p1', 'p8'])
    expect(items[1]).toMatchObject({ permissionCode: outside.permissionCode, permissionName: outside.permissionName })
  })
})
