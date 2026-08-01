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
  createDynamicApiClient,
} from '../../base'

const messageQueryApi = createDynamicApiClient('MessageQuery')
const messageCommandApi = createDynamicApiClient('Message')

export const messageApi = {
  // Query
  emailDetail(id: EmailDetailDto['basicId']) {
    return messageQueryApi.get<EmailDetailDto | null>('EmailDetail', { id })
  },
  emailPage(input: EmailPageQueryDto) {
    return messageQueryApi.post<PageResult<EmailListItemDto>>('EmailPage', input)
  },
  smsDetail(id: SmsDetailDto['basicId']) {
    return messageQueryApi.get<SmsDetailDto | null>('SmsDetail', { id })
  },
  smsPage(input: SmsPageQueryDto) {
    return messageQueryApi.post<PageResult<SmsListItemDto>>('SmsPage', input)
  },
  // Email commands
  createEmail(input: EmailCreateDto) {
    return messageCommandApi.post<EmailDetailDto, EmailCreateDto>('Email', input)
  },
  deleteEmail(id: EmailDetailDto['basicId']) {
    return messageCommandApi.delete('Email', { id })
  },
  updateEmail(input: EmailUpdateDto) {
    return messageCommandApi.put<EmailDetailDto, EmailUpdateDto>('Email', input)
  },
  updateEmailStatus(input: EmailStatusUpdateDto) {
    return messageCommandApi.put<EmailDetailDto, EmailStatusUpdateDto>('EmailStatus', input)
  },
  // SMS commands
  createSms(input: SmsCreateDto) {
    return messageCommandApi.post<SmsDetailDto, SmsCreateDto>('Sms', input)
  },
  deleteSms(id: SmsDetailDto['basicId']) {
    return messageCommandApi.delete('Sms', { id })
  },
  updateSms(input: SmsUpdateDto) {
    return messageCommandApi.put<SmsDetailDto, SmsUpdateDto>('Sms', input)
  },
  updateSmsStatus(input: SmsStatusUpdateDto) {
    return messageCommandApi.put<SmsDetailDto, SmsStatusUpdateDto>('SmsStatus', input)
  },
}
