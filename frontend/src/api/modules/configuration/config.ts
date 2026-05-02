import type { ApiId, PageResult } from '../../types'
import type { ConfigDetailDto, ConfigListItemDto, ConfigPageQueryDto } from './config.types'
import {
  appendDynamicApiParam,
  createDynamicApiClient,
  createPageRequestParams,
  createReadApi,
} from '../../base'

const configQueryApi = createDynamicApiClient('ConfigQuery')
const configReadApi = createReadApi<ConfigListItemDto, ConfigDetailDto, ConfigPageQueryDto>('ConfigQuery', 'Config')

export const configApi = {
  detail(id: ApiId) {
    return configReadApi.detail(id)
  },
  page(input: ConfigPageQueryDto) {
    return configQueryApi.get<PageResult<ConfigListItemDto>>('ConfigPage', toConfigPageParams(input))
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
