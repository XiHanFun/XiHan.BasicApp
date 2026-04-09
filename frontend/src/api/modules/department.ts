import type { PageQuery } from '~/types'
import { useBaseApi } from '../base'
import { buildPageRequest, normalizePageResult, toId, toNumber } from '../helpers'

const api = useBaseApi('Department')

// -------- 类型 --------

export interface SysDepartment {
  basicId: string
  parentId?: string | null
  departmentName: string
  departmentCode?: string
  departmentType?: number
  leaderId?: string
  leader?: string
  phone?: string
  email?: string
  address?: string
  tenantId?: number
  remark?: string
  createTime?: string
  updateTime?: string
  sort?: number
  status?: number
  children?: SysDepartment[]
}

export interface DepartmentPageQuery extends PageQuery {
  status?: number
  departmentType?: number
  parentId?: string
}

// -------- 内部 --------

const PAGE_OPTIONS = {
  keywordFields: ['DepartmentName', 'DepartmentCode', 'Phone', 'Email'],
  filterFieldMap: { status: 'Status', departmentType: 'DepartmentType', parentId: 'ParentId' },
}

const DEPARTMENT_TYPE_MAP: Record<string, number> = {
  Corporation: 0,
  Headquarters: 1,
  Company: 2,
  Branch: 3,
  Division: 4,
  Center: 5,
  Department: 6,
  Section: 7,
  Team: 8,
  Group: 9,
  Project: 10,
  Workgroup: 11,
  Virtual: 12,
  Office: 13,
  Subsidiary: 14,
  Other: 99,
}

const STATUS_MAP: Record<string, number> = {
  No: 0,
  Yes: 1,
}

function resolveEnum(value: unknown, map: Record<string, number>, fallback: number): number {
  if (value === undefined || value === null) {
    return fallback
  }
  if (typeof value === 'number') {
    return value
  }
  if (typeof value === 'string') {
    return map[value] ?? toNumber(value, fallback)
  }
  return fallback
}

function normalizeDepartment(raw: Record<string, any>): SysDepartment {
  const leaderId = raw.leaderId === null || raw.leaderId === undefined
    ? (raw.LeaderId === null || raw.LeaderId === undefined ? undefined : toId(raw.LeaderId))
    : toId(raw.leaderId)
  return {
    basicId: toId(raw.basicId ?? raw.BasicId),
    parentId: raw.parentId === null || raw.parentId === undefined
      ? (raw.ParentId === null || raw.ParentId === undefined ? null : toId(raw.ParentId))
      : toId(raw.parentId),
    departmentName: raw.departmentName ?? raw.DepartmentName ?? '',
    departmentCode: raw.departmentCode ?? raw.DepartmentCode ?? '',
    departmentType: resolveEnum(raw.departmentType ?? raw.DepartmentType, DEPARTMENT_TYPE_MAP, 6),
    leaderId,
    leader: raw.leaderName ?? raw.LeaderName ?? raw.leader ?? raw.Leader ?? undefined,
    phone: raw.phone ?? raw.Phone ?? '',
    email: raw.email ?? raw.Email ?? '',
    address: raw.address ?? raw.Address ?? undefined,
    tenantId: raw.tenantId === null || raw.tenantId === undefined
      ? (raw.TenantId === null || raw.TenantId === undefined ? undefined : toNumber(raw.TenantId, 0))
      : toNumber(raw.tenantId, 0),
    sort: toNumber(raw.sort ?? raw.Sort, 0),
    status: resolveEnum(raw.status ?? raw.Status, STATUS_MAP, 1),
    remark: raw.remark ?? raw.Remark ?? undefined,
    createTime: raw.createTime ?? raw.creationTime ?? raw.createdTime ?? raw.CreatedTime ?? undefined,
    updateTime: raw.updateTime ?? raw.lastModificationTime ?? raw.modifiedTime ?? raw.ModifiedTime ?? undefined,
  }
}

function buildTree(list: SysDepartment[]): SysDepartment[] {
  const map = new Map<string, SysDepartment>()
  const roots: SysDepartment[] = []
  list.forEach(item => map.set(item.basicId, { ...item, children: [] }))
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

function toCreatePayload(data: Partial<SysDepartment>) {
  return {
    parentId: data.parentId === undefined || data.parentId === null || data.parentId === '' ? null : toId(data.parentId),
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

function toUpdatePayload(id: string, data: Partial<SysDepartment>) {
  return {
    ...toCreatePayload(data),
    status: toNumber(data.status, 1),
    basicId: toId(id),
  }
}

async function queryDepartmentPage(params: Record<string, any>) {
  const data = await api.request.post<any>(
    `${api.baseUrl}Page`,
    buildPageRequest(params, PAGE_OPTIONS),
  )
  return normalizePageResult(data, normalizeDepartment)
}

// -------- API --------

export const departmentApi = {
  page: (params: Record<string, any>) => queryDepartmentPage(params),

  tree: async (params: Partial<DepartmentPageQuery> = {}) => {
    const data = await api.request.post<any>(
      `${api.baseUrl}Page`,
      buildPageRequest({ page: 1, pageSize: 9999, ...params }, { disablePaging: true, ...PAGE_OPTIONS }),
    )
    return buildTree(normalizePageResult(data, normalizeDepartment).items)
  },

  detail: (id: string) =>
    api.request.get<any>(`${api.baseUrl}ById`, { params: { id } }).then(normalizeDepartment),

  create: (data: Partial<SysDepartment>) => api.create(toCreatePayload(data)),

  update: (id: string, data: Partial<SysDepartment>) =>
    api.request.put(`${api.baseUrl}Update`, toUpdatePayload(id, data), { params: { id } }),

  delete: (id: string) => api.delete(id),
}

export async function getDepartmentPageApi(params: DepartmentPageQuery) {
  return queryDepartmentPage(params as Record<string, any>)
}

export const getDepartmentTreeApi = departmentApi.tree
export const getDepartmentDetailApi = departmentApi.detail
export const createDepartmentApi = departmentApi.create
export const updateDepartmentApi = departmentApi.update
export const deleteDepartmentApi = departmentApi.delete
