import type { DictPageQuery, PageResult, SysDict, SysDictItem } from '~/types'
import { API_CONTRACT } from '../contract'
import { normalizePageResult } from '../helpers'
import requestClient from '../request'

export async function getDictPageApi(params: DictPageQuery): Promise<PageResult<SysDict>> {
  const data = await requestClient.get<any>(API_CONTRACT.system.dicts, { params })
  return normalizePageResult<SysDict>(data)
}

export function getDictDetailApi(id: string) {
  return requestClient.get<SysDict>(`${API_CONTRACT.system.dicts}/${id}`)
}

export function createDictApi(data: Partial<SysDict>) {
  return requestClient.post<void>(API_CONTRACT.system.dicts, data)
}

export function updateDictApi(id: string, data: Partial<SysDict>) {
  return requestClient.put<void>(`${API_CONTRACT.system.dicts}/${id}`, data)
}

export function deleteDictApi(id: string) {
  return requestClient.delete<void>(`${API_CONTRACT.system.dicts}/${id}`)
}

export function getDictByCodeApi(dictCode: string, tenantId?: number) {
  return requestClient.get<SysDict | null>(`${API_CONTRACT.system.dicts}/by-code`, {
    params: { dictCode, tenantId },
  })
}

export function getDictItemsApi(dictId: string, tenantId?: number) {
  return requestClient.get<SysDictItem[]>(`${API_CONTRACT.system.dicts}/${dictId}/items`, {
    params: { tenantId },
  })
}

export function createDictItemApi(data: Partial<SysDictItem>) {
  return requestClient.post<void>(`${API_CONTRACT.system.dicts}/items`, data)
}

export function updateDictItemApi(dictItemId: string, data: Partial<SysDictItem>) {
  return requestClient.put<void>(`${API_CONTRACT.system.dicts}/items/${dictItemId}`, data)
}

export function deleteDictItemApi(dictItemId: string) {
  return requestClient.delete<void>(`${API_CONTRACT.system.dicts}/items/${dictItemId}`)
}
