import type { FilePageQuery, PageResult, SysFile } from '~/types'
import { API_CONTRACT } from '../contract'
import { normalizePageResult } from '../helpers'
import requestClient from '../request'

type FilePayload = Partial<SysFile> & { accessPermissions?: string }

export async function getFilePageApi(params: FilePageQuery): Promise<PageResult<SysFile>> {
  const data = await requestClient.get<any>(API_CONTRACT.system.files, { params })
  return normalizePageResult<SysFile>(data)
}

export function getFileDetailApi(id: string) {
  return requestClient.get<SysFile>(`${API_CONTRACT.system.files}/${id}`)
}

export function createFileApi(data: FilePayload) {
  return requestClient.post<void>(API_CONTRACT.system.files, data)
}

export function updateFileApi(id: string, data: FilePayload) {
  return requestClient.put<void>(`${API_CONTRACT.system.files}/${id}`, data)
}

export function deleteFileApi(id: string) {
  return requestClient.delete<void>(`${API_CONTRACT.system.files}/${id}`)
}

export function getFileByHashApi(fileHash: string, tenantId?: number) {
  return requestClient.get<SysFile | null>(`${API_CONTRACT.system.files}/by-hash`, {
    params: { fileHash, tenantId },
  })
}
