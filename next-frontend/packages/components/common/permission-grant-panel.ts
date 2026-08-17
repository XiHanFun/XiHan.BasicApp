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
