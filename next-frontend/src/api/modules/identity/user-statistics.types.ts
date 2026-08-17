import type { ApiId, BasicDto, DateTimeString, PageRequest } from '../../types'

/** 与后端 JsonStringEnumConverter 序列化值一致 */
export enum StatisticsPeriod {
  Today = 'Today',
  Yesterday = 'Yesterday',
  ThisWeek = 'ThisWeek',
  LastWeek = 'LastWeek',
  ThisMonth = 'ThisMonth',
  LastMonth = 'LastMonth',
  ThisYear = 'ThisYear',
  LastYear = 'LastYear',
  Custom = 'Custom',
}

export interface UserStatisticsPageQueryDto extends PageRequest {
  period?: StatisticsPeriod | null
  statisticsDateEnd?: DateTimeString | null
  statisticsDateStart?: DateTimeString | null
  userId?: ApiId | null
}

export interface UserStatisticsListItemDto extends BasicDto {
  accessCount: number
  apiCallCount: number
  createdTime: DateTimeString
  errorOperationCount: number
  lastAccessTime?: DateTimeString | null
  lastLoginTime?: DateTimeString | null
  lastOperationTime?: DateTimeString | null
  loginCount: number
  modifiedTime?: DateTimeString | null
  nickName?: string | null
  onlineTime: number
  operationCount: number
  period: StatisticsPeriod
  realName?: string | null
  statisticsDate: DateTimeString
  userId: ApiId
  userName?: string | null
}

export interface UserStatisticsDetailDto extends UserStatisticsListItemDto {
  createdBy?: string | null
  createdId?: ApiId | null
  emailSentCount: number
  fileDownloadCount: number
  fileUploadCount: number
  modifiedBy?: string | null
  modifiedId?: ApiId | null
  notificationReceivedCount: number
  notificationSentCount: number
  remark?: string | null
  smsSentCount: number
}
