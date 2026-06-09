import type { UserInboxItemDto, UserInboxUpdateDto } from './user-inbox.types'
import { createDynamicApiClient } from '../../base'

const userInboxDynamicApi = createDynamicApiClient('UserInbox')

export const userInboxApi = {
  confirm(basicId: string) {
    return userInboxDynamicApi.post<void, UserInboxUpdateDto>('Confirm', toUpdateDto(basicId))
  },
  list(unreadOnly = false) {
    return userInboxDynamicApi.get<UserInboxItemDto[]>('List', { unreadOnly })
  },
  markAllRead() {
    return userInboxDynamicApi.post<void>('MarkAllRead')
  },
  markRead(basicId: string) {
    return userInboxDynamicApi.post<void, UserInboxUpdateDto>('MarkRead', toUpdateDto(basicId))
  },
}

function toUpdateDto(basicId: string): UserInboxUpdateDto {
  return { basicId }
}
