import type { PageResult } from '../../types'
import type {
  EmailDetailDto,
  EmailListItemDto,
  EmailPageQueryDto,
  SmsDetailDto,
  SmsListItemDto,
  SmsPageQueryDto,
} from './message.types'
import {
  appendDynamicApiParam,
  createDynamicApiClient,
  createPageRequestParams,
  formatDynamicApiRouteValue,
} from '../../base'

const messageQueryApi = createDynamicApiClient('MessageQuery')

export const messageApi = {
  emailDetail(id: EmailDetailDto['basicId']) {
    return messageQueryApi.get<EmailDetailDto | null>(`EmailDetail/${formatDynamicApiRouteValue(id)}`)
  },
  emailPage(input: EmailPageQueryDto) {
    return messageQueryApi.get<PageResult<EmailListItemDto>>(
      'EmailPage',
      toEmailPageParams(input),
    )
  },
  smsDetail(id: SmsDetailDto['basicId']) {
    return messageQueryApi.get<SmsDetailDto | null>(`SmsDetail/${formatDynamicApiRouteValue(id)}`)
  },
  smsPage(input: SmsPageQueryDto) {
    return messageQueryApi.get<PageResult<SmsListItemDto>>(
      'SmsPage',
      toSmsPageParams(input),
    )
  },
}

function toEmailPageParams(input: EmailPageQueryDto) {
  const params = createPageRequestParams(input)

  appendDynamicApiParam(params, 'BusinessId', input.businessId)
  appendDynamicApiParam(params, 'BusinessType', input.businessType)
  appendDynamicApiParam(params, 'EmailStatus', input.emailStatus)
  appendDynamicApiParam(params, 'EmailType', input.emailType)
  appendDynamicApiParam(params, 'Keyword', input.keyword)
  appendDynamicApiParam(params, 'ReceiveUserId', input.receiveUserId)
  appendDynamicApiParam(params, 'ScheduledTimeEnd', input.scheduledTimeEnd)
  appendDynamicApiParam(params, 'ScheduledTimeStart', input.scheduledTimeStart)
  appendDynamicApiParam(params, 'SendTimeEnd', input.sendTimeEnd)
  appendDynamicApiParam(params, 'SendTimeStart', input.sendTimeStart)
  appendDynamicApiParam(params, 'SendUserId', input.sendUserId)
  appendDynamicApiParam(params, 'TemplateId', input.templateId)

  return params
}

function toSmsPageParams(input: SmsPageQueryDto) {
  const params = createPageRequestParams(input)

  appendDynamicApiParam(params, 'BusinessId', input.businessId)
  appendDynamicApiParam(params, 'BusinessType', input.businessType)
  appendDynamicApiParam(params, 'Keyword', input.keyword)
  appendDynamicApiParam(params, 'Provider', input.provider)
  appendDynamicApiParam(params, 'ReceiverId', input.receiverId)
  appendDynamicApiParam(params, 'ScheduledTimeEnd', input.scheduledTimeEnd)
  appendDynamicApiParam(params, 'ScheduledTimeStart', input.scheduledTimeStart)
  appendDynamicApiParam(params, 'SendTimeEnd', input.sendTimeEnd)
  appendDynamicApiParam(params, 'SendTimeStart', input.sendTimeStart)
  appendDynamicApiParam(params, 'SenderId', input.senderId)
  appendDynamicApiParam(params, 'SmsStatus', input.smsStatus)
  appendDynamicApiParam(params, 'SmsType', input.smsType)
  appendDynamicApiParam(params, 'TemplateId', input.templateId)

  return params
}
