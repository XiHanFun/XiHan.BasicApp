import type { DictPageQuery, PageResult, SysDict, SysDictItem } from '~/types'
import { buildPageRequest, normalizePageResult, toId, toNumber } from '../helpers'
import requestClient from '../request'

const DICT_API = '/api/Dict'

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

export async function getDictPageApi(params: DictPageQuery): Promise<PageResult<SysDict>> {
  const data = await requestClient.post<any>(
    `${DICT_API}/Page`,
    buildPageRequest(params, {
      keywordFields: ['DictCode', 'DictName', 'DictType', 'DictDescription'],
      filterFieldMap: {
        dictType: 'DictType',
        status: 'Status',
      },
    }),
  )
  return normalizePageResult(data, normalizeDict)
}

export function getDictDetailApi(id: string) {
  return requestClient
    .get<any>(`${DICT_API}/ById`, { params: { id } })
    .then(raw => normalizeDict(raw))
}

export function createDictApi(data: Partial<SysDict>) {
  return requestClient.post<void>(`${DICT_API}/Create`, toDictCreatePayload(data))
}

export function updateDictApi(id: string, data: Partial<SysDict>) {
  return requestClient.put<void>(`${DICT_API}/Update`, toDictUpdatePayload(id, data), {
    params: { id },
  })
}

export function deleteDictApi(id: string) {
  return requestClient.delete<void>(`${DICT_API}/Delete`, {
    params: { id },
  })
}

export function getDictByCodeApi(dictCode: string, tenantId?: number) {
  return requestClient
    .get<any>(`${DICT_API}/DictByCode/${tenantId ?? 0}`, {
      params: { dictCode },
    })
    .then(raw => (raw ? normalizeDict(raw) : null))
}

export function getDictItemsApi(dictId: string, tenantId?: number) {
  return requestClient
    .get<any[]>(`${DICT_API}/DictItems/${dictId}/${tenantId ?? 0}`)
    .then(list => (Array.isArray(list) ? list.map(item => normalizeDictItem(item)) : []))
}

export function createDictItemApi(data: Partial<SysDictItem>) {
  return requestClient.post<void>(`${DICT_API}/Item`, toDictItemCreatePayload(data))
}

export function updateDictItemApi(dictItemId: string, data: Partial<SysDictItem>) {
  return requestClient.put<void>(`${DICT_API}/Item/${dictItemId}`, toDictItemUpdatePayload(dictItemId, data))
}

export function deleteDictItemApi(dictItemId: string) {
  return requestClient.delete<void>(`${DICT_API}/Item/${dictItemId}`)
}
