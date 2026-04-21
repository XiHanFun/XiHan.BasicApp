import type { PageQuery } from '~/types'
import { useBaseApi } from '../base'
import { toId, toNumber } from '../helpers'

const api = useBaseApi('Tenant')

// -------- 类型 --------

export interface SysTenant {
  basicId: string
  tenantCode: string
  tenantName: string
  editionId?: string
  tenantShortName?: string
  contactPerson?: string
  contactName?: string
  contactPhone?: string
  contactEmail?: string
  address?: string
  logo?: string
  domain?: string
  isolationMode?: number
  databaseType?: number
  databaseSchema?: string
  configStatus?: number
  tenantStatus?: number
  userLimit?: number
  storageLimit?: number
  expireTime?: string
  expiredTime?: string
  sort?: number
  createTime?: string
  updateTime?: string
  remark?: string
}

export interface TenantPageQuery extends PageQuery {
  status?: number
  tenantStatus?: number
  isolationMode?: number
}

// -------- 内部 --------

function toCreatePayload(data: Partial<SysTenant>) {
  return {
    tenantCode: data.tenantCode ?? '',
    tenantName: data.tenantName ?? '',
    editionId: data.editionId ? toId(data.editionId) : null,
    tenantShortName: data.tenantShortName ?? '',
    contactPerson: data.contactPerson ?? data.contactName ?? '',
    contactPhone: data.contactPhone ?? '',
    contactEmail: data.contactEmail ?? '',
    address: data.address ?? '',
    logo: data.logo ?? '',
    domain: data.domain ?? '',
    isolationMode: toNumber(data.isolationMode, 0),
    databaseType: data.databaseType === undefined ? null : toNumber(data.databaseType, 0),
    databaseSchema: data.databaseSchema ?? '',
    configStatus: toNumber(data.configStatus, 0),
    tenantStatus: toNumber(data.tenantStatus, 0),
    userLimit: data.userLimit === undefined ? null : toNumber(data.userLimit, 0),
    storageLimit: data.storageLimit === undefined ? null : toNumber(data.storageLimit, 0),
    expireTime: data.expireTime ?? data.expiredTime ?? null,
    sort: toNumber(data.sort, 0),
    remark: data.remark ?? '',
  }
}

function toUpdatePayload(id: string, data: Partial<SysTenant>) {
  return {
    editionId: data.editionId ? toId(data.editionId) : null,
    tenantName: data.tenantName ?? '',
    tenantShortName: data.tenantShortName ?? '',
    contactPerson: data.contactPerson ?? data.contactName ?? '',
    contactPhone: data.contactPhone ?? '',
    contactEmail: data.contactEmail ?? '',
    address: data.address ?? '',
    logo: data.logo ?? '',
    domain: data.domain ?? '',
    databaseType: data.databaseType === undefined ? null : toNumber(data.databaseType, 0),
    databaseSchema: data.databaseSchema ?? '',
    configStatus: toNumber(data.configStatus, 0),
    expireTime: data.expireTime ?? data.expiredTime ?? null,
    tenantStatus: toNumber(data.tenantStatus, 0),
    userLimit: data.userLimit === undefined ? null : toNumber(data.userLimit, 0),
    storageLimit: data.storageLimit === undefined ? null : toNumber(data.storageLimit, 0),
    sort: toNumber(data.sort, 0),
    remark: data.remark ?? '',
    basicId: toId(id),
  }
}

// -------- API --------

export const tenantApi = {
  page: (params: Record<string, any>) =>
    api.page(params, {
      keywordFields: ['TenantName', 'TenantCode', 'ContactPerson', 'ContactPhone'],
      filterFieldMap: { status: 'Status', tenantStatus: 'TenantStatus', isolationMode: 'IsolationMode' },
    }),

  detail: (id: string) => api.detail(id),

  create: (data: Partial<SysTenant>) => api.create(toCreatePayload(data)),

  update: (id: string, data: Partial<SysTenant>) =>
    api.request.put(`${api.baseUrl}Update`, toUpdatePayload(id, data), { params: { id } }),

  delete: (id: string) => api.delete(id),

  changeStatus: (id: string, tenantStatus: number) =>
    api.request.post(`${api.baseUrl}ChangeStatus`, {
      tenantId: toId(id),
      tenantStatus: toNumber(tenantStatus, 0),
    }),
}
