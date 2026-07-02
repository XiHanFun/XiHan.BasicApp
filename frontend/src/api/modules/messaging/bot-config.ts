import type { ApiId, PageResult } from '../../types'
import type {
  BotConfigCreateDto,
  BotConfigDefaultUpdateDto,
  BotConfigDetailDto,
  BotConfigListItemDto,
  BotConfigPageQueryDto,
  BotConfigStatusUpdateDto,
  BotConfigUpdateDto,
} from './bot-config.types'
import { createDynamicApiClient, formatDynamicApiRouteValue } from '../../base'

const botConfigQueryApi = createDynamicApiClient('BotConfigQuery')
const botConfigCommandApi = createDynamicApiClient('BotConfig')

export const botConfigApi = {
  create(input: BotConfigCreateDto) {
    return botConfigCommandApi.post<BotConfigDetailDto, BotConfigCreateDto>('BotConfig', input)
  },
  delete(id: ApiId) {
    return botConfigCommandApi.delete(`BotConfig/${formatDynamicApiRouteValue(id)}`)
  },
  detail(id: ApiId) {
    return botConfigQueryApi.get<BotConfigDetailDto | null>(
      `BotConfigDetail/${formatDynamicApiRouteValue(id)}`,
    )
  },
  page(input: BotConfigPageQueryDto) {
    return botConfigQueryApi.post<PageResult<BotConfigListItemDto>>('BotConfigPage', input)
  },
  setDefault(input: BotConfigDefaultUpdateDto) {
    // 后端 SetDefaultBotConfigAsync：Set 前缀不在动词约定表，保留完整方法名，默认 POST
    return botConfigCommandApi.post<BotConfigDetailDto, BotConfigDefaultUpdateDto>(
      'SetDefaultBotConfig',
      input,
    )
  },
  update(input: BotConfigUpdateDto) {
    return botConfigCommandApi.put<BotConfigDetailDto, BotConfigUpdateDto>('BotConfig', input)
  },
  updateStatus(input: BotConfigStatusUpdateDto) {
    return botConfigCommandApi.put<BotConfigDetailDto, BotConfigStatusUpdateDto>('BotConfigStatus', input)
  },
}
