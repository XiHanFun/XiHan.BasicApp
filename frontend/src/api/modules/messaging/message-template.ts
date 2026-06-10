import type { ApiId, PageResult } from '../../types'
import type {
  MessageTemplateCreateDto,
  MessageTemplateDetailDto,
  MessageTemplateListItemDto,
  MessageTemplatePageQueryDto,
  MessageTemplateStatusUpdateDto,
  MessageTemplateUpdateDto,
} from './message-template.types'
import {
  appendDynamicApiParam,
  createDynamicApiClient,
  createPageRequestParams,
  formatDynamicApiRouteValue,
} from '../../base'

const messageTemplateQueryApi = createDynamicApiClient('MessageTemplateQuery')
const messageTemplateCommandApi = createDynamicApiClient('MessageTemplate')

export const messageTemplateApi = {
  create(input: MessageTemplateCreateDto) {
    return messageTemplateCommandApi.post<MessageTemplateDetailDto, MessageTemplateCreateDto>('MessageTemplate', input)
  },
  delete(id: ApiId) {
    return messageTemplateCommandApi.delete(`MessageTemplate/${formatDynamicApiRouteValue(id)}`)
  },
  detail(id: ApiId) {
    return messageTemplateQueryApi.get<MessageTemplateDetailDto | null>(
      `MessageTemplateDetail/${formatDynamicApiRouteValue(id)}`,
    )
  },
  page(input: MessageTemplatePageQueryDto) {
    return messageTemplateQueryApi.get<PageResult<MessageTemplateListItemDto>>(
      'MessageTemplatePage',
      toMessageTemplatePageParams(input),
    )
  },
  update(input: MessageTemplateUpdateDto) {
    return messageTemplateCommandApi.put<MessageTemplateDetailDto, MessageTemplateUpdateDto>('MessageTemplate', input)
  },
  updateStatus(input: MessageTemplateStatusUpdateDto) {
    return messageTemplateCommandApi.put<MessageTemplateDetailDto, MessageTemplateStatusUpdateDto>('MessageTemplateStatus', input)
  },
}

function toMessageTemplatePageParams(input: MessageTemplatePageQueryDto) {
  const params = createPageRequestParams(input)
  appendDynamicApiParam(params, 'Channel', input.channel)
  appendDynamicApiParam(params, 'Keyword', input.keyword)
  appendDynamicApiParam(params, 'Status', input.status)
  return params
}
