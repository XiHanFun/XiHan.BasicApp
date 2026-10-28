// 由 scripts/gen-contracts.mjs 从 contracts/openapi.json 生成；不要手改，改后端契约后执行 pnpm gen:contracts。

// 枚举文案由后端消息目录按语言提供（随 N1 的 metadata.enums.list 接入）；这里只有成员与类型。

export const DataScope = ['SelfOnly', 'DepartmentOnly', 'DepartmentAndChildren', 'All', 'Custom'] as const
export type DataScope = (typeof DataScope)[number]

export const MenuKind = ['Page', 'Directory', 'Link'] as const
export type MenuKind = (typeof MenuKind)[number]

export const OrganizationStatus = ['Active', 'Disabled'] as const
export type OrganizationStatus = (typeof OrganizationStatus)[number]

export const PermissionScope = ['Platform', 'Tenant', 'Both'] as const
export type PermissionScope = (typeof PermissionScope)[number]

export const RoleStatus = ['Active', 'Disabled'] as const
export type RoleStatus = (typeof RoleStatus)[number]

export const TenantMemberType = ['Member', 'Owner'] as const
export type TenantMemberType = (typeof TenantMemberType)[number]

export const TenantStatus = ['Active', 'Suspended'] as const
export type TenantStatus = (typeof TenantStatus)[number]

export const UserSettingScene = ['Preference', 'Page'] as const
export type UserSettingScene = (typeof UserSettingScene)[number]

export const UserStatus = ['Active', 'Disabled', 'Locked'] as const
export type UserStatus = (typeof UserStatus)[number]

/** 全部枚举：类型名 → 成员。 */
export const enums = {
  DataScope,
  MenuKind,
  OrganizationStatus,
  PermissionScope,
  RoleStatus,
  TenantMemberType,
  TenantStatus,
  UserSettingScene,
  UserStatus,
} as const

export type EnumName = keyof typeof enums

/** 枚举成员的显示文案；后端枚举文案（metadata.enums.list）接入前原样返回成员名，未知成员也原样返回。 */
export function enumLabel(_type: EnumName, value: string): string {
  return value
}
