import type { DepartmentPageQuery, PageResult, SysDepartment } from '~/types'
import { buildPageRequest, normalizePageResult, toId, toNumber } from '../helpers'
import requestClient from '../request'

const DEPARTMENT_API = '/api/Department'

function normalizeDepartment(raw: Record<string, any>): SysDepartment {
  const leaderId
    = raw.leaderId === null || raw.leaderId === undefined ? undefined : toId(raw.leaderId)

  return {
    basicId: toId(raw.basicId),
    parentId: raw.parentId === null || raw.parentId === undefined ? null : toId(raw.parentId),
    departmentName: raw.departmentName ?? '',
    departmentCode: raw.departmentCode ?? '',
    departmentType: toNumber(raw.departmentType, 6),
    leaderId,
    leader: raw.leaderName ?? raw.leader ?? (leaderId === undefined ? undefined : leaderId),
    phone: raw.phone ?? '',
    email: raw.email ?? '',
    address: raw.address ?? undefined,
    tenantId: raw.tenantId === null || raw.tenantId === undefined ? undefined : toNumber(raw.tenantId, 0),
    sort: toNumber(raw.sort, 0),
    status: toNumber(raw.status, 1),
    remark: raw.remark ?? undefined,
    createTime: raw.createTime ?? raw.creationTime ?? raw.createdTime ?? undefined,
    updateTime: raw.updateTime ?? raw.lastModificationTime ?? undefined,
  }
}

function toDepartmentCreatePayload(data: Partial<SysDepartment>) {
  return {
    parentId:
      data.parentId === undefined || data.parentId === null || data.parentId === ''
        ? null
        : toId(data.parentId),
    departmentName: data.departmentName ?? '',
    departmentCode: data.departmentCode ?? '',
    departmentType: toNumber(data.departmentType, 6),
    leaderId: data.leaderId === undefined ? null : toId(data.leaderId),
    phone: data.phone ?? '',
    email: data.email ?? '',
    address: data.address ?? '',
    sort: toNumber(data.sort, 0),
    tenantId: data.tenantId === undefined ? null : toNumber(data.tenantId, 0),
    remark: data.remark ?? '',
  }
}

function toDepartmentUpdatePayload(id: string, data: Partial<SysDepartment>) {
  return {
    ...toDepartmentCreatePayload(data),
    status: toNumber(data.status, 1),
    basicId: toId(id),
  }
}

function buildDepartmentTree(list: SysDepartment[]): SysDepartment[] {
  const map = new Map<string, SysDepartment>()
  const roots: SysDepartment[] = []
  list.forEach((item) => {
    map.set(item.basicId, { ...item, children: [] })
  })

  map.forEach((item) => {
    const parentId = item.parentId ? String(item.parentId) : ''
    if (parentId && map.has(parentId)) {
      map.get(parentId)!.children!.push(item)
    }
    else {
      roots.push(item)
    }
  })

  return roots
}

export async function getDepartmentPageApi(
  params: DepartmentPageQuery,
): Promise<PageResult<SysDepartment>> {
  const data = await requestClient.post<any>(
    `${DEPARTMENT_API}/Page`,
    buildPageRequest(params, {
      keywordFields: ['DepartmentName', 'DepartmentCode', 'Phone', 'Email'],
      filterFieldMap: {
        status: 'Status',
        departmentType: 'DepartmentType',
        parentId: 'ParentId',
      },
    }),
  )
  return normalizePageResult(data, normalizeDepartment)
}

export async function getDepartmentTreeApi(params: Partial<DepartmentPageQuery> = {}) {
  const data = await requestClient.post<any>(
    `${DEPARTMENT_API}/Page`,
    buildPageRequest(
      { page: 1, pageSize: 9999, ...params },
      {
        disablePaging: true,
        keywordFields: ['DepartmentName', 'DepartmentCode', 'Phone', 'Email'],
        filterFieldMap: {
          status: 'Status',
          departmentType: 'DepartmentType',
          parentId: 'ParentId',
        },
      },
    ),
  )
  const list = normalizePageResult(data, normalizeDepartment).items
  return buildDepartmentTree(list)
}

export function getDepartmentDetailApi(id: string) {
  return requestClient
    .get<any>(`${DEPARTMENT_API}/ById`, { params: { id } })
    .then(raw => normalizeDepartment(raw))
}

export function createDepartmentApi(data: Partial<SysDepartment>) {
  return requestClient.post<void>(`${DEPARTMENT_API}/Create`, toDepartmentCreatePayload(data))
}

export function updateDepartmentApi(id: string, data: Partial<SysDepartment>) {
  return requestClient.put<void>(`${DEPARTMENT_API}/Update`, toDepartmentUpdatePayload(id, data), {
    params: { id },
  })
}

export function deleteDepartmentApi(id: string) {
  return requestClient.delete<void>(`${DEPARTMENT_API}/Delete`, {
    params: { id },
  })
}
