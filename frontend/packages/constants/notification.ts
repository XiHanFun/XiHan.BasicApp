/**
 * 通知相关的静态选项
 *
 * 枚举取自 `~/types/enums`（packages 自带的一份，零依赖），而非 `src/api`——
 * 通知组件属于 packages 层，不能反向依赖 src
 */
import { NotificationType } from '~/types/enums'

export const NOTIFICATION_TYPE_OPTIONS = [
  { label: '系统公告', value: NotificationType.System },
  { label: '安全通知', value: NotificationType.Security },
  { label: '业务通知', value: NotificationType.Business },
  { label: '待办通知', value: NotificationType.Todo },
  { label: '紧急通知', value: NotificationType.Emergency },
]
