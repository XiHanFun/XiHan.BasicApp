import type { PageQuery } from '~/types'
import { useBaseApi } from '../base'
import { toId, toNumber } from '../helpers'

const api = useBaseApi('Dict')

// -------- 类型 --------

export interface SysDict {
  basicId: string
  dictCode: string
  dictName: string
  dictType: string
  dictDescription?: string
  status: number
  sort: number
  createTime?: string
  updateTime?: string
  remark?: string
}

export interface SysDictItem {
  basicId: string
  dictId: string
  dictCode: string
  parentId?: string
  itemCode: string
  itemName: string
  itemValue?: string
  status: number
  sort: number
  createTime?: string
  updateTime?: string
  remark?: string
}

export interface DictPageQuery extends PageQuery {
  dictType?: string
  status?: number
}

// -------- 内部 --------

function normalizeDict(raw: Record<string, any>): SysDict {
  return {
    basicId: toId(raw.basicId),
    dictCode: raw.dictCode ?? '',
    dictName: raw.dictName ?? '',
    dictType: raw.dictType ?? '',
    dictDescription: raw.dictDescription ?? raw.remark ?? '',
    status: toNumber(raw.status, 1),
    sort: toNumber(raw.sort, 0),
    createTime: raw.createTime ?? raw.creationTime ?? raw.createdTime ?? undefined,
    updateTime: raw.updateTime ?? raw.lastModificationTime ?? undefined,
    remark: raw.remark ?? undefined,
  }
}

function normalizeDictItem(raw: Record<string, any>): SysDictItem {
  return {
    basicId: toId(raw.basicId),
    dictId: toId(raw.dictId),
    dictCode: raw.dictCode ?? '',
    parentId: raw.parentId === null || raw.parentId === undefined ? undefined : toId(raw.parentId),
    itemCode: raw.itemCode ?? '',
    itemName: raw.itemName ?? '',
    itemValue: raw.itemValue ?? '',
    status: toNumber(raw.status, 1),
    sort: toNumber(raw.sort, 0),
    createTime: raw.createTime ?? raw.creationTime ?? raw.createdTime ?? undefined,
    updateTime: raw.updateTime ?? raw.lastModificationTime ?? undefined,
    remark: raw.remark ?? undefined,
  }
}

function toDictCreatePayload(data: Partial<SysDict>) {
  return {
    dictCode: data.dictCode ?? '',
    dictName: data.dictName ?? '',
    dictType: data.dictType ?? '',
    dictDescription: data.dictDescription ?? '',
    sort: toNumber(data.sort, 0),
  }
}

function toDictUpdatePayload(id: string, data: Partial<SysDict>) {
  return {
    ...toDictCreatePayload(data),
    status: toNumber(data.status, 1),
    remark: data.remark ?? '',
    basicId: toId(id),
  }
}

function toDictItemCreatePayload(data: Partial<SysDictItem>) {
  return {
    dictId: toId(data.dictId),
    dictCode: data.dictCode ?? '',
    parentId: data.parentId ? toId(data.parentId) : null,
    itemCode: data.itemCode ?? '',
    itemName: data.itemName ?? '',
    itemValue: data.itemValue ?? '',
    sort: toNumber(data.sort, 0),
  }
}

function toDictItemUpdatePayload(id: string, data: Partial<SysDictItem>) {
  return {
    ...toDictItemCreatePayload(data),
    status: toNumber(data.status, 1),
    remark: data.remark ?? '',
    basicId: toId(id),
  }
}

// -------- API --------

export const dictApi = {
  page: (params: Record<string, any>) =>
    api.page(params, {
      keywordFields: ['DictCode', 'DictName', 'DictType', 'DictDescription'],
      filterFieldMap: { dictType: 'DictType', status: 'Status' },
    }),

  detail: (id: string) =>
    api.request.get<any>(`${api.baseUrl}ById`, { params: { id } }).then(normalizeDict),

  create: (data: Partial<SysDict>) => api.create(toDictCreatePayload(data)),

  update: (id: string, data: Partial<SysDict>) =>
    api.request.put(`${api.baseUrl}Update`, toDictUpdatePayload(id, data), { params: { id } }),

  delete: (id: string) => api.delete(id),

  getByCode: (dictCode: string, tenantId?: number) =>
    api.request
      .get<any>(`${api.baseUrl}DictByCode/${tenantId ?? 0}`, { params: { dictCode } })
      .then((raw: any) => (raw ? normalizeDict(raw) : null)),

  getItems: (dictId: string, tenantId?: number) =>
    api.request
      .get<any[]>(`${api.baseUrl}DictItems/${dictId}/${tenantId ?? 0}`)
      .then((list: any[]) => (Array.isArray(list) ? list.map(normalizeDictItem) : [])),

  createItem: (data: Partial<SysDictItem>) =>
    api.request.post(`${api.baseUrl}Item`, toDictItemCreatePayload(data)),

  updateItem: (id: string, data: Partial<SysDictItem>) =>
    api.request.put(`${api.baseUrl}Item/${id}`, toDictItemUpdatePayload(id, data)),

  deleteItem: (id: string) => api.request.delete(`${api.baseUrl}Item/${id}`),
}
