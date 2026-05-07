import type { ApiId, BasicDto, DateTimeString, PageRequest } from '../../types'

export enum StatisticsPeriod {
  Today = 0,
  Yesterday = 1,
  ThisWeek = 2,
  LastWeek = 3,
  ThisMonth = 4,
  LastMonth = 5,
  ThisYear = 6,
  LastYear = 7,
  Custom = 99,
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
