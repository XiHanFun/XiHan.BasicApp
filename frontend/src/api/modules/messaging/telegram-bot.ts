import type { ApiId, PageResult } from '../../types'
import type {
  TelegramBotCreateDto,
  TelegramBotDetailDto,
  TelegramBotListItemDto,
  TelegramBotPageQueryDto,
  TelegramBotStatusUpdateDto,
  TelegramBotUpdateDto,
} from './telegram-bot.types'
import { createDynamicApiClient, formatDynamicApiRouteValue } from '../../base'

const telegramBotQueryApi = createDynamicApiClient('TelegramBotQuery')
const telegramBotCommandApi = createDynamicApiClient('TelegramBot')

export const telegramBotApi = {
  create(input: TelegramBotCreateDto) {
    return telegramBotCommandApi.post<TelegramBotDetailDto, TelegramBotCreateDto>('TelegramBot', input)
  },
  delete(id: ApiId) {
    return telegramBotCommandApi.delete(`TelegramBot/${formatDynamicApiRouteValue(id)}`)
  },
  detail(id: ApiId) {
    return telegramBotQueryApi.get<TelegramBotDetailDto | null>(
      `TelegramBotDetail/${formatDynamicApiRouteValue(id)}`,
    )
  },
  page(input: TelegramBotPageQueryDto) {
    return telegramBotQueryApi.post<PageResult<TelegramBotListItemDto>>('TelegramBotPage', input)
  },
  update(input: TelegramBotUpdateDto) {
    return telegramBotCommandApi.put<TelegramBotDetailDto, TelegramBotUpdateDto>('TelegramBot', input)
  },
  updateStatus(input: TelegramBotStatusUpdateDto) {
    return telegramBotCommandApi.put<TelegramBotDetailDto, TelegramBotStatusUpdateDto>('TelegramBotStatus', input)
  },
}
