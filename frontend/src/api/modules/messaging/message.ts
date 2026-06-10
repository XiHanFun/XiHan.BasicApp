import type { PageResult } from '../../types'
import type {
  EmailCreateDto,
  EmailDetailDto,
  EmailListItemDto,
  EmailPageQueryDto,
  EmailStatusUpdateDto,
  EmailUpdateDto,
  SmsCreateDto,
  SmsDetailDto,
  SmsListItemDto,
  SmsPageQueryDto,
  SmsStatusUpdateDto,
  SmsUpdateDto,
} from './message.types'
import {
  appendDynamicApiParam,
  createDynamicApiClient,
  createPageRequestParams,
  formatDynamicApiRouteValue,
} from '../../base'

const messageQueryApi = createDynamicApiClient('MessageQuery')
const messageCommandApi = createDynamicApiClient('Message')

export const messageApi = {
  // Query
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
  // Email commands
  createEmail(input: EmailCreateDto) {
    return messageCommandApi.post<EmailDetailDto, EmailCreateDto>('CreateEmail', input)
  },
  deleteEmail(id: EmailDetailDto['basicId']) {
    return messageCommandApi.delete(`DeleteEmail/${formatDynamicApiRouteValue(id)}`)
  },
  updateEmail(input: EmailUpdateDto) {
    return messageCommandApi.put<EmailDetailDto, EmailUpdateDto>('UpdateEmail', input)
  },
  updateEmailStatus(input: EmailStatusUpdateDto) {
    return messageCommandApi.put<EmailDetailDto, EmailStatusUpdateDto>('UpdateEmailStatus', input)
  },
  // SMS commands
  createSms(input: SmsCreateDto) {
    return messageCommandApi.post<SmsDetailDto, SmsCreateDto>('CreateSms', input)
  },
  deleteSms(id: SmsDetailDto['basicId']) {
    return messageCommandApi.delete(`DeleteSms/${formatDynamicApiRouteValue(id)}`)
  },
  updateSms(input: SmsUpdateDto) {
    return messageCommandApi.put<SmsDetailDto, SmsUpdateDto>('UpdateSms', input)
  },
  updateSmsStatus(input: SmsStatusUpdateDto) {
    return messageCommandApi.put<SmsDetailDto, SmsStatusUpdateDto>('UpdateSmsStatus', input)
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
  appendDynamicApiParam(params, 'TemplateCode', input.templateCode)

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
  appendDynamicApiParam(params, 'TemplateCode', input.templateCode)

  return params
}
