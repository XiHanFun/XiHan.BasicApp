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
  appendDynamicApiParam,
  createDynamicApiClient,
  createPageRequestParams,
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
    return configQueryApi.get<PageResult<ConfigListItemDto>>('ConfigPage', toConfigPageParams(input))
  },
  update(input: ConfigUpdateDto) {
    return configCommandApi.put<ConfigDetailDto, ConfigUpdateDto>('Config', input)
  },
  updateStatus(input: ConfigStatusUpdateDto) {
    return configCommandApi.put<ConfigDetailDto, ConfigStatusUpdateDto>('ConfigStatus', input)
  },
}

function toConfigPageParams(input: ConfigPageQueryDto) {
  const params = createPageRequestParams(input)
  appendDynamicApiParam(params, 'ConfigGroup', input.configGroup)
  appendDynamicApiParam(params, 'ConfigType', input.configType)
  appendDynamicApiParam(params, 'DataType', input.dataType)
  appendDynamicApiParam(params, 'IsBuiltIn', input.isBuiltIn)
  appendDynamicApiParam(params, 'IsEncrypted', input.isEncrypted)
  appendDynamicApiParam(params, 'IsGlobal', input.isGlobal)
  appendDynamicApiParam(params, 'Keyword', input.keyword)
  appendDynamicApiParam(params, 'Status', input.status)
  return params
}
