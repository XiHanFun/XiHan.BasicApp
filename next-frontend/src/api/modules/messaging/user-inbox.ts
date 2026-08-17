import type { UserInboxItemDto, UserInboxUpdateDto } from './user-inbox.types'
import { createDynamicApiClient } from '../../base'

const userInboxDynamicApi = createDynamicApiClient('UserInbox')

export const userInboxApi = {
  banner() {
    return userInboxDynamicApi.get<UserInboxItemDto[]>('Banner')
  },
  confirm(basicId: string) {
    return userInboxDynamicApi.post<void, UserInboxUpdateDto>('Confirm', toUpdateDto(basicId))
  },
  list(unreadOnly = false) {
    return userInboxDynamicApi.get<UserInboxItemDto[]>('List', { unreadOnly })
  },
  mandatoryUnread() {
    return userInboxDynamicApi.get<UserInboxItemDto[]>('MandatoryUnread')
  },
  markAllRead() {
    return userInboxDynamicApi.post<void>('MarkAllRead')
  },
  markPopupShown(basicId: string) {
    return userInboxDynamicApi.post<void, UserInboxUpdateDto>('MarkPopupShown', toUpdateDto(basicId))
  },
  markRead(basicId: string) {
    return userInboxDynamicApi.post<void, UserInboxUpdateDto>('MarkRead', toUpdateDto(basicId))
  },
  popup() {
    return userInboxDynamicApi.get<UserInboxItemDto[]>('Popup')
  },
}

function toUpdateDto(basicId: string): UserInboxUpdateDto {
  return { basicId }
}
