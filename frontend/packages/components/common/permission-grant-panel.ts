/** 权限授权面板条目最小契约：权限目录项与各类已授予项都满足 */
export interface PermissionGrantItem {
  basicId: number | string
  permissionCode: string
  permissionName: string
  groupCode?: null | string
  groupName?: null | string
  moduleCode?: null | string
  resourceName?: null | string
}

/** 一组权限按资源聚出来的段 */
export interface PermissionGroup<T extends PermissionGrantItem> {
  key: string
  name: string
  items: T[]
}

/** 取权限码的资源段作为分组键：saas:{resource}:{action} → resource */
function resourceKey(code: string): string {
  const parts = (code ?? '').split(':')
  return parts.length >= 3 ? parts[1]! : (parts[0] ?? '')
}

/** 一组权限名的公共前缀，作为功能块显示名 */
function commonPrefix(names: string[]): string {
  if (names.length === 0) {
    return ''
  }
  let prefix = names[0]!
  for (const name of names) {
    let i = 0
    while (i < prefix.length && i < name.length && prefix[i] === name[i]) {
      i++
    }
    prefix = prefix.slice(0, i)
    if (!prefix) {
      break
    }
  }
  return prefix
}

/**
 * 按资源分组：组码优先用后端 groupCode，缺省回退资源段推导。
 *
 * 勾选面板与权限穿梭框共用这一份口径——各推一遍的话，同一批权限在两个界面里会分出
 * 不同的段，换用另一个界面时看起来像数据变了。
 */
export function groupPermissions<T extends PermissionGrantItem>(
  items: readonly T[],
  otherGroupLabel = 'other',
): PermissionGroup<T>[] {
  const map = new Map<string, T[]>()
  for (const item of items) {
    const key = item.groupCode
      || item.resourceName
      || resourceKey(item.permissionCode)
      || item.moduleCode
      || otherGroupLabel
    const list = map.get(key)
    if (list) {
      list.push(item)
    }
    else {
      map.set(key, [item])
    }
  }
  return [...map.entries()].map(([key, groupItems]) => ({
    key,
    name: groupItems[0]?.groupName
      || commonPrefix(groupItems.map(groupItem => groupItem.permissionName))
      || key,
    items: groupItems,
  }))
}
