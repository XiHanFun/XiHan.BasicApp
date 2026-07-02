import type { ApiId, PageResult } from '../../types'
import type {
  EmailConfigCreateDto,
  EmailConfigDefaultUpdateDto,
  EmailConfigDetailDto,
  EmailConfigListItemDto,
  EmailConfigPageQueryDto,
  EmailConfigStatusUpdateDto,
  EmailConfigUpdateDto,
} from './email-config.types'
import { createDynamicApiClient, formatDynamicApiRouteValue } from '../../base'

const emailConfigQueryApi = createDynamicApiClient('EmailConfigQuery')
const emailConfigCommandApi = createDynamicApiClient('EmailConfig')

export const emailConfigApi = {
  create(input: EmailConfigCreateDto) {
    return emailConfigCommandApi.post<EmailConfigDetailDto, EmailConfigCreateDto>('EmailConfig', input)
  },
  delete(id: ApiId) {
    return emailConfigCommandApi.delete(`EmailConfig/${formatDynamicApiRouteValue(id)}`)
  },
  detail(id: ApiId) {
    return emailConfigQueryApi.get<EmailConfigDetailDto | null>(
      `EmailConfigDetail/${formatDynamicApiRouteValue(id)}`,
    )
  },
  page(input: EmailConfigPageQueryDto) {
    return emailConfigQueryApi.post<PageResult<EmailConfigListItemDto>>('EmailConfigPage', input)
  },
  setDefault(input: EmailConfigDefaultUpdateDto) {
    // 后端 SetDefaultEmailConfigAsync：Set 前缀不在动词约定表，保留完整方法名，默认 POST
    return emailConfigCommandApi.post<EmailConfigDetailDto, EmailConfigDefaultUpdateDto>(
      'SetDefaultEmailConfig',
      input,
    )
  },
  update(input: EmailConfigUpdateDto) {
    return emailConfigCommandApi.put<EmailConfigDetailDto, EmailConfigUpdateDto>('EmailConfig', input)
  },
  updateStatus(input: EmailConfigStatusUpdateDto) {
    return emailConfigCommandApi.put<EmailConfigDetailDto, EmailConfigStatusUpdateDto>('EmailConfigStatus', input)
  },
}
