import type { ApiId, PageResult } from '../../types'
import type {
  SmsConfigCreateDto,
  SmsConfigDefaultUpdateDto,
  SmsConfigDetailDto,
  SmsConfigListItemDto,
  SmsConfigPageQueryDto,
  SmsConfigStatusUpdateDto,
  SmsConfigUpdateDto,
} from './sms-config.types'
import { createDynamicApiClient, formatDynamicApiRouteValue } from '../../base'

const smsConfigQueryApi = createDynamicApiClient('SmsConfigQuery')
const smsConfigCommandApi = createDynamicApiClient('SmsConfig')

export const smsConfigApi = {
  create(input: SmsConfigCreateDto) {
    return smsConfigCommandApi.post<SmsConfigDetailDto, SmsConfigCreateDto>('SmsConfig', input)
  },
  delete(id: ApiId) {
    return smsConfigCommandApi.delete(`SmsConfig/${formatDynamicApiRouteValue(id)}`)
  },
  detail(id: ApiId) {
    return smsConfigQueryApi.get<SmsConfigDetailDto | null>(
      `SmsConfigDetail/${formatDynamicApiRouteValue(id)}`,
    )
  },
  page(input: SmsConfigPageQueryDto) {
    return smsConfigQueryApi.post<PageResult<SmsConfigListItemDto>>('SmsConfigPage', input)
  },
  setDefault(input: SmsConfigDefaultUpdateDto) {
    // 后端 SetDefaultSmsConfigAsync：Set 前缀不在动词约定表，保留完整方法名，默认 POST
    return smsConfigCommandApi.post<SmsConfigDetailDto, SmsConfigDefaultUpdateDto>(
      'SetDefaultSmsConfig',
      input,
    )
  },
  update(input: SmsConfigUpdateDto) {
    return smsConfigCommandApi.put<SmsConfigDetailDto, SmsConfigUpdateDto>('SmsConfig', input)
  },
  updateStatus(input: SmsConfigStatusUpdateDto) {
    return smsConfigCommandApi.put<SmsConfigDetailDto, SmsConfigStatusUpdateDto>('SmsConfigStatus', input)
  },
}
