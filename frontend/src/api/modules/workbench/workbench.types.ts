import type { DateTimeString } from '../../types'
import type { StatisticsPeriod } from '../identity'
import type { UserInboxItemDto } from '../messaging'

export interface WorkbenchDashboardStatisticsDto {
  accessCount: number
  apiCallCount: number
  errorOperationCount: number
  lastAccessTime?: DateTimeString | null
  lastLoginTime?: DateTimeString | null
  lastOperationTime?: DateTimeString | null
  loginCount: number
  onlineTime: number
  operationCount: number
  period: StatisticsPeriod
  statisticsDate: DateTimeString
}

export interface WorkbenchInboxSummaryDto {
  latestItems: UserInboxItemDto[]
  pendingConfirmCount: number
  unreadCount: number
}

export interface WorkbenchDashboardSummaryDto {
  generatedTime: DateTimeString
  inbox: WorkbenchInboxSummaryDto
  statistics: WorkbenchDashboardStatisticsDto
}
