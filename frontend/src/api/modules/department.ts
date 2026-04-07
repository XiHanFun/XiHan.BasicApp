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

function normalizeDepartment(raw: Record<string, any>): SysDepartment {
  const leaderId = raw.leaderId === null || raw.leaderId === undefined ? undefined : toId(raw.leaderId)
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

// -------- API --------

export const departmentApi = {
  page: (params: Record<string, any>) => api.page(params, PAGE_OPTIONS),

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
  const data = await api.request.post<any>(
    `${api.baseUrl}Page`,
    buildPageRequest(params as Record<string, any>, PAGE_OPTIONS),
  )
  return normalizePageResult(data, normalizeDepartment)
}

export const getDepartmentTreeApi = departmentApi.tree
export const getDepartmentDetailApi = departmentApi.detail
export const createDepartmentApi = departmentApi.create
export const updateDepartmentApi = departmentApi.update
export const deleteDepartmentApi = departmentApi.delete
