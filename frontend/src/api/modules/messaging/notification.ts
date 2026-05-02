import type { PageResult } from '../../types'
import type {
  NotificationDetailDto,
  NotificationListItemDto,
  NotificationPageQueryDto,
  UserNotificationDetailDto,
  UserNotificationListItemDto,
  UserNotificationPageQueryDto,
} from './types'
import {
  appendDynamicApiParam,
  createDynamicApiClient,
  createPageRequestParams,
  formatDynamicApiRouteValue,
} from '../../base'

const notificationQueryApi = createDynamicApiClient('NotificationQuery')

export const notificationApi = {
  detail(id: NotificationDetailDto['basicId']) {
    return notificationQueryApi.get<NotificationDetailDto | null>(
      `NotificationDetail/${formatDynamicApiRouteValue(id)}`,
    )
  },
  page(input: NotificationPageQueryDto) {
    return notificationQueryApi.get<PageResult<NotificationListItemDto>>(
      'NotificationPage',
      toNotificationPageParams(input),
    )
  },
  userDetail(id: UserNotificationDetailDto['basicId']) {
    return notificationQueryApi.get<UserNotificationDetailDto | null>(
      `UserNotificationDetail/${formatDynamicApiRouteValue(id)}`,
    )
  },
  userPage(input: UserNotificationPageQueryDto) {
    return notificationQueryApi.get<PageResult<UserNotificationListItemDto>>(
      'UserNotificationPage',
      toUserNotificationPageParams(input),
    )
  },
}

function toNotificationPageParams(input: NotificationPageQueryDto) {
  const params = createPageRequestParams(input)

  appendDynamicApiParam(params, 'BusinessId', input.businessId)
  appendDynamicApiParam(params, 'BusinessType', input.businessType)
  appendDynamicApiParam(params, 'ExpireTimeEnd', input.expireTimeEnd)
  appendDynamicApiParam(params, 'ExpireTimeStart', input.expireTimeStart)
  appendDynamicApiParam(params, 'IsBroadcast', input.isBroadcast)
  appendDynamicApiParam(params, 'IsPublished', input.isPublished)
  appendDynamicApiParam(params, 'Keyword', input.keyword)
  appendDynamicApiParam(params, 'NeedConfirm', input.needConfirm)
  appendDynamicApiParam(params, 'NotificationType', input.notificationType)
  appendDynamicApiParam(params, 'SendTimeEnd', input.sendTimeEnd)
  appendDynamicApiParam(params, 'SendTimeStart', input.sendTimeStart)
  appendDynamicApiParam(params, 'SendUserId', input.sendUserId)

  return params
}

function toUserNotificationPageParams(input: UserNotificationPageQueryDto) {
  const params = createPageRequestParams(input)

  appendDynamicApiParam(params, 'ConfirmTimeEnd', input.confirmTimeEnd)
  appendDynamicApiParam(params, 'ConfirmTimeStart', input.confirmTimeStart)
  appendDynamicApiParam(params, 'NotificationId', input.notificationId)
  appendDynamicApiParam(params, 'NotificationStatus', input.notificationStatus)
  appendDynamicApiParam(params, 'ReadTimeEnd', input.readTimeEnd)
  appendDynamicApiParam(params, 'ReadTimeStart', input.readTimeStart)
  appendDynamicApiParam(params, 'UserId', input.userId)

  return params
}
