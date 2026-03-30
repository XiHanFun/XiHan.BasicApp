import type { ConfigPageQuery, PageResult, SysConfig } from '~/types'
import { buildPageRequest, normalizePageResult, toId, toNumber } from '../helpers'
import requestClient from '../request'

const CONFIG_API = '/api/Config'

function normalizeConfig(raw: Record<string, any>): SysConfig {
  return {
    basicId: toId(raw.basicId),
    configName: raw.configName ?? '',
    configKey: raw.configKey ?? '',
    configValue: raw.configValue ?? '',
    configType: toNumber(raw.configType, 0),
    dataType: toNumber(raw.dataType, 0),
    status: toNumber(raw.status, 1),
    createTime: raw.createTime ?? raw.creationTime ?? raw.createdTime ?? undefined,
    updateTime: raw.updateTime ?? raw.lastModificationTime ?? undefined,
    remark: raw.remark ?? undefined,
  }
}

function toConfigCreatePayload(data: Partial<SysConfig>) {
  return {
    configName: data.configName ?? '',
    configKey: data.configKey ?? '',
    configValue: data.configValue ?? '',
    configType: toNumber(data.configType, 0),
    dataType: toNumber(data.dataType, 0),
    remark: data.remark ?? '',
  }
}

function toConfigUpdatePayload(id: string, data: Partial<SysConfig>) {
  return {
    ...toConfigCreatePayload(data),
    status: toNumber(data.status, 1),
    basicId: toId(id),
  }
}

export async function getConfigPageApi(params: ConfigPageQuery): Promise<PageResult<SysConfig>> {
  const data = await requestClient.post<any>(
    `${CONFIG_API}/Page`,
    buildPageRequest(params, {
      keywordFields: ['ConfigName', 'ConfigKey', 'ConfigValue'],
      filterFieldMap: {
        configType: 'ConfigType',
        dataType: 'DataType',
        status: 'Status',
      },
    }),
  )
  return normalizePageResult(data, normalizeConfig)
}

export function getConfigDetailApi(id: string) {
  return requestClient
    .get<any>(`${CONFIG_API}/ById`, { params: { id } })
    .then(raw => normalizeConfig(raw))
}

export function createConfigApi(data: Partial<SysConfig>) {
  return requestClient.post<void>(`${CONFIG_API}/Create`, toConfigCreatePayload(data))
}

export function updateConfigApi(id: string, data: Partial<SysConfig>) {
  return requestClient.put<void>(`${CONFIG_API}/Update`, toConfigUpdatePayload(id, data), {
    params: { id },
  })
}

export function deleteConfigApi(id: string) {
  return requestClient.delete<void>(`${CONFIG_API}/Delete`, {
    params: { id },
  })
}

export function getConfigByKeyApi(configKey: string, tenantId?: number) {
  return requestClient
    .get<any>(`${CONFIG_API}/ConfigByKey/${tenantId ?? 0}`, {
      params: { configKey },
    })
    .then(raw => (raw ? normalizeConfig(raw) : null))
}
