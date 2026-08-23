import type { DateTimeString } from '../../types'
import type { StatisticsPeriod } from '../identity'

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

export interface WorkbenchDashboardSummaryDto {
  generatedTime: DateTimeString
  statistics: WorkbenchDashboardStatisticsDto
}
