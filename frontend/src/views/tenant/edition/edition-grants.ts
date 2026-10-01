import type { ApiId, PermissionListItemDto, TenantEditionPermissionBatchUpdateDto, TenantEditionPermissionListItemDto } from '@/api'
import type { PermissionGrantItem } from '~/components/common/permission-grant-panel'
import { ValidityStatus } from '@/api'

/** 版本权限穿梭框条目：目录项，或生效中却不在目录里的映射 */
export type EditionPermissionItem = PermissionGrantItem & { basicId: ApiId }

/** 版本权限的变更：字段与批量接口一致，提交时补上版本主键 */
export type EditionGrantDiff = Omit<TenantEditionPermissionBatchUpdateDto, 'editionId'>

/** 生效中的映射：租户白名单只认这些，也是穿梭框右栏的初始值 */
export function validEditionPermissionIds(mappings: readonly TenantEditionPermissionListItemDto[]): ApiId[] {
  return mappings
    .filter(mapping => mapping.status === ValidityStatus.Valid)
    .map(mapping => mapping.permissionId)
}

/**
 * 权限目录再补上生效中却不在目录里的映射（目录不列平台侧权限，旧数据里可能挂着）。
 * 穿梭框只认条目里有的键，不补进来，一动穿梭框它们就从草稿里掉出去，保存时被当成撤销
 */
export function mergeMappedIntoCatalog(
  catalog: readonly PermissionListItemDto[],
  mappings: readonly TenantEditionPermissionListItemDto[],
): EditionPermissionItem[] {
  const known = new Set(catalog.map(permission => permission.basicId))
  const extra = mappings
    .filter(mapping => mapping.status === ValidityStatus.Valid && !known.has(mapping.permissionId))
    .map(mapping => ({
      basicId: mapping.permissionId,
      permissionCode: mapping.permissionCode ?? '',
      permissionName: mapping.permissionName ?? String(mapping.permissionId),
      moduleCode: mapping.moduleCode,
    }))
  return [...catalog, ...extra]
}

/**
 * 右栏（生效中）与现有映射的差量。停用的映射不进白名单，与未授予同在左栏：
 * - 没有映射的移到右栏：授予
 * - 停用的移到右栏：改回有效（再授予会被当成已绑定而跳过）
 * - 生效的移回左栏：撤销
 * 停用的留在左栏不动，不顺手删掉
 */
export function diffEditionGrants(
  checked: readonly ApiId[],
  mappings: readonly TenantEditionPermissionListItemDto[],
): EditionGrantDiff {
  const target = new Set(checked)
  const byPermissionId = new Map(mappings.map(mapping => [mapping.permissionId, mapping] as const))
  const diff: EditionGrantDiff = { grantPermissionIds: [], revokeEditionPermissionIds: [], statusChanges: [] }
  for (const permissionId of checked) {
    const mapping = byPermissionId.get(permissionId)
    if (!mapping) {
      diff.grantPermissionIds.push(permissionId)
    }
    else if (mapping.status !== ValidityStatus.Valid) {
      diff.statusChanges.push({ basicId: mapping.basicId, status: ValidityStatus.Valid })
    }
  }
  for (const mapping of mappings) {
    if (mapping.status === ValidityStatus.Valid && !target.has(mapping.permissionId)) {
      diff.revokeEditionPermissionIds.push(mapping.basicId)
    }
  }
  return diff
}

/** 差量是否为空：脏态与「无变化」提示共用 */
export function isEmptyEditionGrantDiff(diff: EditionGrantDiff) {
  return diff.grantPermissionIds.length === 0
    && diff.revokeEditionPermissionIds.length === 0
    && diff.statusChanges.length === 0
}
