/**
 * 租户相关的静态选项。
 *
 * 枚举取自 `~/types/enums`（packages 自带的一份，零依赖），而非 `src/api`——
 * 控制中心 / 个人中心「我的租户」属 packages 层 shell 页，不能反向依赖 src。
 *
 * 这里的中文标签只是**兜底**：真源是后端枚举元数据，运行时由 `useEnumOptions('TenantMemberType')`
 * 拉取并覆盖（可本地化、切语言响应式重取）。仅在元数据未加载/未部署时才会看到这份。
 */
import { TenantMemberType } from '~/types/enums'

export const MEMBER_TYPE_OPTIONS = [
  { label: '租户所有者', value: TenantMemberType.Owner },
  { label: '租户管理员', value: TenantMemberType.Admin },
  { label: '普通成员', value: TenantMemberType.Member },
  { label: '外部协作者', value: TenantMemberType.External },
  { label: '访客', value: TenantMemberType.Guest },
  { label: '顾问', value: TenantMemberType.Consultant },
  { label: '平台管理员', value: TenantMemberType.PlatformAdmin },
]
