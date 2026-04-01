import type { PageQuery } from '~/types'
import { useBaseApi } from '../base'
import { toId, toNumber } from '../helpers'

const api = useBaseApi('Config')

// -------- 类型 --------

export interface SysConfig {
  basicId: string
  configName: string
  configKey: string
  configValue?: string
  configType: number
  dataType: number
  status: number
  createTime?: string
  updateTime?: string
  remark?: string
}

export interface ConfigPageQuery extends PageQuery {
  configType?: number
  dataType?: number
  status?: number
}

// -------- 内部 --------

function toCreatePayload(data: Partial<SysConfig>) {
  return {
    configName: data.configName ?? '',
    configKey: data.configKey ?? '',
    configValue: data.configValue ?? '',
    configType: toNumber(data.configType, 0),
    dataType: toNumber(data.dataType, 0),
    remark: data.remark ?? '',
  }
}

function toUpdatePayload(id: string, data: Partial<SysConfig>) {
  return {
    ...toCreatePayload(data),
    status: toNumber(data.status, 1),
    basicId: toId(id),
  }
}

// -------- API --------

export const configApi = {
  page: (params: Record<string, any>) =>
    api.page(params, {
      keywordFields: ['ConfigName', 'ConfigKey', 'ConfigValue'],
      filterFieldMap: { configType: 'ConfigType', dataType: 'DataType', status: 'Status' },
    }),

  detail: (id: string) => api.detail(id),

  create: (data: Partial<SysConfig>) => api.create(toCreatePayload(data)),

  update: (id: string, data: Partial<SysConfig>) =>
    api.request.put(`${api.baseUrl}Update`, toUpdatePayload(id, data), { params: { id } }),

  delete: (id: string) => api.delete(id),

  getByKey: (configKey: string, tenantId?: number) =>
    api.request.get(`${api.baseUrl}ConfigByKey/${tenantId ?? 0}`, { params: { configKey } }),
}
