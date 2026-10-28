/**
 * 各日志的结果、类型配色（列表单元格与详情抬头共用，同一个值在两处颜色一致）。
 */
import type { Tone } from '@xihan-ui/core'
import type { AccessResult, ConditionHitAction, DiffKind, LoginEvent, PermissionChangeType, TraceItemStatus } from '~/contracts'

/** 访问结果：成功绿、失败与服务端故障红、未认证与越权黄，找不到中性 */
export function accessResultTone(result: unknown): Tone {
  switch (result as AccessResult) {
    case 'Success': return 'success'
    case 'Failed':
    case 'ServerError': return 'danger'
    case 'Forbidden':
    case 'Unauthorized': return 'warning'
    default: return 'neutral'
  }
}

/** 登录事件：登录绿、会话类（刷新、退出、切换租户）蓝、模仿与改密黄 */
export function loginEventTone(event: unknown): Tone {
  switch (event as LoginEvent) {
    case 'Login': return 'success'
    case 'Refresh':
    case 'Logout':
    case 'SwitchTenant': return 'info'
    case 'ImpersonationStart':
    case 'ImpersonationStop':
    case 'ChangePassword': return 'warning'
    default: return 'neutral'
  }
}

/** 数据变更：新增绿、修改蓝、删除红 */
export function diffKindTone(kind: unknown): Tone {
  switch (kind as DiffKind) {
    case 'Insert': return 'success'
    case 'Update': return 'info'
    case 'Delete': return 'danger'
    default: return 'neutral'
  }
}

/** 权限变更：授予与加入绿、撤销与移出黄、删除角色红、范围与套餐蓝 */
export function permissionChangeTone(type: unknown): Tone {
  switch (type as PermissionChangeType) {
    case 'RoleGrant':
    case 'RoleAssign': return 'success'
    case 'RoleRevoke':
    case 'RoleUnassign': return 'warning'
    case 'RoleDelete': return 'danger'
    default: return 'info'
  }
}

/** 命中规则的动作：拒绝红、放行绿、只记录中性 */
export function conditionActionTone(action: unknown): Tone {
  switch (action as ConditionHitAction) {
    case 'Deny': return 'danger'
    case 'Allow': return 'success'
    default: return 'neutral'
  }
}

/** 时间线条目状态：与后端归并的口径一致 */
export function traceStatusTone(status: unknown): Tone {
  switch (status as TraceItemStatus) {
    case 'Success': return 'success'
    case 'Warning': return 'warning'
    case 'Failure': return 'danger'
    default: return 'info'
  }
}

/** 布尔成败：真绿、假红 */
export function booleanTone(value: unknown): Tone {
  return value ? 'success' : 'danger'
}
