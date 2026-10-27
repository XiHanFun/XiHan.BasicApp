/**
 * 平台在租户目录里的 ID（后端 `TenantIds.Platform`）：平台账号的注册地租户。
 * 会话与切换请求里的 `tenantId` 为空即平台；这个值只用来判断「注册地是否平台」。
 */
export const PLATFORM_TENANT_ID = 'platform'
