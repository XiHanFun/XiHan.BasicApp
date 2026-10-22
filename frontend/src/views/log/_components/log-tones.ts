/**
 * 各日志的结果、类型、等级配色（列表单元格与详情抬头共用，同一个值在两处颜色一致）。
 */
import type { Tone } from '@xihan-ui/core'
import {
  AccessResult,
  AuditRiskLevel,
  LoginResult,
  OperationExecuteResult,
  OperationType,
  PermissionChangeType,
} from '@/api'

/** 操作结果：成功绿、部分成功黄、失败红 */
export function operationResultTone(result: unknown): Tone {
  switch (result) {
    case OperationExecuteResult.Success: return 'success'
    case OperationExecuteResult.PartialSuccess: return 'warning'
    default: return 'danger'
  }
}

/** 按操作的影响分色：写入绿、改动蓝、删除红、审核审批黄、发起执行品牌色，只读与其他中性 */
export function operationTypeTone(type: unknown): Tone {
  switch (type) {
    case OperationType.Create:
    case OperationType.Restore: return 'success'
    case OperationType.Update:
    case OperationType.Import:
    case OperationType.Export: return 'info'
    case OperationType.Delete: return 'danger'
    case OperationType.Review:
    case OperationType.Approve: return 'warning'
    case OperationType.StartTask:
    case OperationType.Execute: return 'brand'
    default: return 'neutral'
  }
}

/** 访问结果：成功绿、失败红、未授权与禁止黄，其余中性 */
export function accessResultTone(result: unknown): Tone {
  switch (result) {
    case AccessResult.Success: return 'success'
    case AccessResult.Failed: return 'danger'
    case AccessResult.Forbidden:
    case AccessResult.Unauthorized: return 'warning'
    default: return 'neutral'
  }
}

/** 登录结果：成功绿、凭据或二次验证失败红、会话类事件蓝、账号状态与安全设置变动黄 */
export function loginResultTone(result: unknown): Tone {
  switch (result) {
    case LoginResult.Success: return 'success'
    case LoginResult.Logout:
    case LoginResult.TokenRefreshed:
    case LoginResult.TenantSwitched: return 'info'
    case LoginResult.InvalidCredentials:
    case LoginResult.TwoFactorFailed:
    case LoginResult.Failed: return 'danger'
    case LoginResult.AccountLocked:
    case LoginResult.AccountDisabled:
    case LoginResult.RequiresTwoFactor:
    case LoginResult.PasswordChanged:
    case LoginResult.PasswordReset:
    case LoginResult.MfaBound:
    case LoginResult.MfaUnbound:
    case LoginResult.SessionRevoked:
    case LoginResult.ImpersonationStarted:
    case LoginResult.ImpersonationEnded: return 'warning'
    default: return 'neutral'
  }
}

/** 异常严重程度：低蓝、中黄、高及以上红 */
export function severityTone(level: unknown): Tone {
  switch (level) {
    case 1: return 'info'
    case 2: return 'warning'
    case 3:
    case 4:
    case 5: return 'danger'
    default: return 'neutral'
  }
}

/** 审计风险等级：极高与严重红、高黄、中蓝、低中性 */
export function riskLevelTone(level: unknown): Tone {
  if (level === AuditRiskLevel.Critical || level === AuditRiskLevel.VeryHigh)
    return 'danger'
  if (level === AuditRiskLevel.High)
    return 'warning'
  if (level === AuditRiskLevel.Medium)
    return 'info'
  return 'neutral'
}

/** 权限变更：拒绝红、撤销与移除黄、条件变更蓝、授予与分配绿 */
export function permissionChangeTone(type: unknown): Tone {
  switch (type) {
    case PermissionChangeType.UserDenyPermission:
    case PermissionChangeType.RoleDenyPermission:
      return 'danger'
    case PermissionChangeType.RoleRevokePermission:
    case PermissionChangeType.UserRevokePermission:
    case PermissionChangeType.UserRemoveRole:
    case PermissionChangeType.RoleRemoveParent:
      return 'warning'
    case PermissionChangeType.ConditionChange:
      return 'info'
    default:
      return 'success'
  }
}

/** 布尔成败：真绿、假红 */
export function booleanTone(value: unknown): Tone {
  return value ? 'success' : 'danger'
}
