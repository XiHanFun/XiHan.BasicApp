import type { ApiId, PageResult } from '../../types'
import type {
  ConfigCreateDto,
  ConfigDetailDto,
  ConfigListItemDto,
  ConfigPageQueryDto,
  ConfigStatusUpdateDto,
  ConfigUpdateDto,
} from './config.types'
import {
  createDynamicApiClient,
  createReadApi,
  formatDynamicApiRouteValue,
} from '../../base'

const configQueryApi = createDynamicApiClient('ConfigQuery')
const configCommandApi = createDynamicApiClient('Config')
const configReadApi = createReadApi<ConfigListItemDto, ConfigDetailDto, ConfigPageQueryDto>('ConfigQuery', 'Config')

export const configApi = {
  create(input: ConfigCreateDto) {
    return configCommandApi.post<ConfigDetailDto, ConfigCreateDto>('Config', input)
  },
  delete(id: ApiId) {
    return configCommandApi.delete(`Config/${formatDynamicApiRouteValue(id)}`)
  },
  detail(id: ApiId) {
    return configReadApi.detail(id)
  },
  page(input: ConfigPageQueryDto) {
    return configQueryApi.post<PageResult<ConfigListItemDto>>('ConfigPage', input)
  },
  update(input: ConfigUpdateDto) {
    return configCommandApi.put<ConfigDetailDto, ConfigUpdateDto>('Config', input)
  },
  updateStatus(input: ConfigStatusUpdateDto) {
    return configCommandApi.put<ConfigDetailDto, ConfigStatusUpdateDto>('ConfigStatus', input)
  },
}
