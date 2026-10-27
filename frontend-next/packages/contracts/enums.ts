// 由 scripts/gen-contracts.mjs 从 contracts/openapi.json 生成；不要手改，改后端契约后执行 pnpm gen:contracts。

// 枚举文案由后端消息目录按语言提供（随 N1 的 metadata.enums.list 接入）；这里只有成员与类型。

export const UserStatus = ['Active', 'Disabled', 'Locked'] as const
export type UserStatus = (typeof UserStatus)[number]

/** 全部枚举：类型名 → 成员。 */
export const enums = {
  UserStatus,
} as const

export type EnumName = keyof typeof enums

/** 枚举成员的显示文案；后端枚举文案（metadata.enums.list）接入前原样返回成员名，未知成员也原样返回。 */
export function enumLabel(_type: EnumName, value: string): string {
  return value
}
