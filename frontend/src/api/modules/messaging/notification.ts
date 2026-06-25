import type { ApiId, PageResult } from '../../types'
import type {
  NotificationCreateDto,
  NotificationDetailDto,
  NotificationListItemDto,
  NotificationPageQueryDto,
  NotificationPublishDto,
  NotificationPublishResultDto,
  NotificationReadStatsDto,
  NotificationUnreadUserDto,
  NotificationUnreadUserPageQueryDto,
  NotificationUpdateDto,
  UserNotificationDetailDto,
  UserNotificationListItemDto,
  UserNotificationPageQueryDto,
} from './notification.types'
import { createDynamicApiClient, formatDynamicApiRouteValue } from '../../base'

const notificationQueryApi = createDynamicApiClient('NotificationQuery')
const notificationCommandApi = createDynamicApiClient('Notification')

export const notificationApi = {
  create(input: NotificationCreateDto) {
    return notificationCommandApi.post<NotificationDetailDto, NotificationCreateDto>('Notification', input)
  },
  delete(id: ApiId) {
    return notificationCommandApi.delete(`Notification/${formatDynamicApiRouteValue(id)}`)
  },
  detail(id: NotificationDetailDto['basicId']) {
    return notificationQueryApi.get<NotificationDetailDto | null>(
      `NotificationDetail/${formatDynamicApiRouteValue(id)}`,
    )
  },
  page(input: NotificationPageQueryDto) {
    return notificationQueryApi.post<PageResult<NotificationListItemDto>>('NotificationPage', input)
  },
  publish(input: NotificationPublishDto) {
    return notificationCommandApi.post<NotificationPublishResultDto, NotificationPublishDto>('PublishNotification', input)
  },
  readStats(id: ApiId) {
    return notificationQueryApi.get<NotificationReadStatsDto>(`NotificationReadStats/${formatDynamicApiRouteValue(id)}`)
  },
  remind(id: ApiId) {
    // RemindAsync(long id)：非 CRUD 前缀 → 默认 POST，POST 不把 id 拼进路由段，
    // 故 id 走 query（同 export Cancel 模式），route 为 /Notification/Remind?id=
    return notificationCommandApi.post<NotificationPublishResultDto>('Remind', undefined, { params: { id } })
  },
  unreadUserPage(input: NotificationUnreadUserPageQueryDto) {
    return notificationQueryApi.post<PageResult<NotificationUnreadUserDto>>('NotificationUnreadUserPage', input)
  },
  update(input: NotificationUpdateDto) {
    return notificationCommandApi.put<NotificationDetailDto, NotificationUpdateDto>('Notification', input)
  },
  userDetail(id: UserNotificationDetailDto['basicId']) {
    return notificationQueryApi.get<UserNotificationDetailDto | null>(
      `UserNotificationDetail/${formatDynamicApiRouteValue(id)}`,
    )
  },
  userPage(input: UserNotificationPageQueryDto) {
    return notificationQueryApi.post<PageResult<UserNotificationListItemDto>>('UserNotificationPage', input)
  },
}
