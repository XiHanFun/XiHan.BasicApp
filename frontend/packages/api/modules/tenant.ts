import type { PageResult, SysTenant, TenantPageQuery } from '~/types'
import { buildPageRequest, normalizePageResult, toId, toNumber } from '../helpers'
import requestClient from '../request'

const TENANT_API = '/api/Tenant'

function normalizeTenant(raw: Record<string, any>): SysTenant {
  const contactPerson = raw.contactPerson ?? raw.contactName ?? ''

  return {
    basicId: toId(raw.basicId),
    tenantName: raw.tenantName ?? '',
    tenantCode: raw.tenantCode ?? '',
    tenantShortName: raw.tenantShortName ?? undefined,
    contactPerson,
    contactName: contactPerson,
    contactPhone: raw.contactPhone ?? '',
    contactEmail: raw.contactEmail ?? '',
    isolationMode: toNumber(raw.isolationMode, 0),
    tenantStatus: toNumber(raw.tenantStatus, 0),
    status: toNumber(raw.status, 1),
    expireTime: raw.expireTime ?? raw.expiredTime ?? undefined,
    expiredTime: raw.expiredTime ?? raw.expireTime ?? undefined,
    createTime: raw.createTime ?? raw.creationTime ?? raw.createdTime ?? undefined,
    updateTime: raw.updateTime ?? raw.lastModificationTime ?? undefined,
    remark: raw.remark ?? undefined,
  }
}

function toTenantCreatePayload(data: Partial<SysTenant>) {
  return {
    tenantCode: data.tenantCode ?? '',
    tenantName: data.tenantName ?? '',
    tenantShortName: data.tenantShortName ?? '',
    contactPerson: data.contactPerson ?? data.contactName ?? '',
    contactPhone: data.contactPhone ?? '',
    contactEmail: data.contactEmail ?? '',
    isolationMode: toNumber(data.isolationMode, 0),
  }
}

function toTenantUpdatePayload(id: string, data: Partial<SysTenant>) {
  return {
    tenantName: data.tenantName ?? '',
    tenantShortName: data.tenantShortName ?? '',
    contactPerson: data.contactPerson ?? data.contactName ?? '',
    contactPhone: data.contactPhone ?? '',
    contactEmail: data.contactEmail ?? '',
    expireTime: data.expireTime ?? data.expiredTime ?? null,
    status: toNumber(data.status, 1),
    remark: data.remark ?? '',
    basicId: toNumber(id, 0),
  }
}

export async function getTenantPageApi(
  params: TenantPageQuery,
): Promise<PageResult<SysTenant>> {
  const data = await requestClient.post<any>(
    `${TENANT_API}/Page`,
    buildPageRequest(params, {
      keywordFields: ['TenantName', 'TenantCode', 'ContactPerson', 'ContactPhone'],
      filterFieldMap: {
        status: 'Status',
        tenantStatus: 'TenantStatus',
        isolationMode: 'IsolationMode',
      },
    }),
  )
  return normalizePageResult(data, normalizeTenant)
}

export function getTenantDetailApi(id: string) {
  return requestClient
    .get<any>(`${TENANT_API}/ById`, { params: { id } })
    .then(raw => normalizeTenant(raw))
}

export function createTenantApi(data: Partial<SysTenant>) {
  return requestClient.post<void>(`${TENANT_API}/Create`, toTenantCreatePayload(data))
}

export function updateTenantApi(id: string, data: Partial<SysTenant>) {
  return requestClient.put<void>(`${TENANT_API}/Update`, toTenantUpdatePayload(id, data), {
    params: { id },
  })
}

export function deleteTenantApi(id: string) {
  return requestClient.delete<void>(`${TENANT_API}/Delete`, {
    params: { id },
  })
}

export function changeTenantStatusApi(id: string, tenantStatus: number) {
  return requestClient.post<void>(`${TENANT_API}/ChangeStatus`, {
    tenantId: toNumber(id, 0),
    tenantStatus: toNumber(tenantStatus, 0),
  })
}
