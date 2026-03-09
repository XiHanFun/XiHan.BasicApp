import type { FilePageQuery, PageResult, SysFile } from '~/types'
import { buildPageRequest, normalizePageResult, toId, toNumber } from '../helpers'
import requestClient from '../request'

type FilePayload = Partial<SysFile> & { accessPermissions?: string }

const FILE_API = '/api/File'

function normalizeFile(raw: Record<string, any>): SysFile {
  return {
    basicId: toId(raw.basicId),
    fileName: raw.fileName ?? '',
    originalName: raw.originalName ?? '',
    fileExtension: raw.fileExtension ?? undefined,
    fileType: toNumber(raw.fileType, 0),
    mimeType: raw.mimeType ?? undefined,
    fileSize: toNumber(raw.fileSize, 0),
    fileHash: raw.fileHash ?? undefined,
    isPublic: Boolean(raw.isPublic),
    requireAuth: Boolean(raw.requireAuth),
    isTemporary: Boolean(raw.isTemporary),
    expiresAt: raw.expiresAt ?? undefined,
    status: toNumber(raw.status, 0),
    tags: raw.tags ?? undefined,
    createTime: raw.createTime ?? raw.creationTime ?? raw.createdTime ?? undefined,
    updateTime: raw.updateTime ?? raw.lastModificationTime ?? undefined,
    remark: raw.remark ?? undefined,
  }
}

function toFileCreatePayload(data: FilePayload) {
  return {
    fileName: data.fileName ?? '',
    originalName: data.originalName ?? '',
    fileExtension: data.fileExtension ?? '',
    fileType: toNumber(data.fileType, 0),
    mimeType: data.mimeType ?? '',
    fileSize: toNumber(data.fileSize, 0),
    fileHash: data.fileHash ?? '',
    isPublic: Boolean(data.isPublic),
    requireAuth: Boolean(data.requireAuth),
    accessPermissions: data.accessPermissions ?? '',
    isTemporary: Boolean(data.isTemporary),
    expiresAt: data.expiresAt ?? null,
    tags: data.tags ?? '',
    remark: data.remark ?? '',
  }
}

function toFileUpdatePayload(id: string, data: FilePayload) {
  return {
    ...toFileCreatePayload(data),
    status: toNumber(data.status, 0),
    basicId: toNumber(id, 0),
  }
}

export async function getFilePageApi(params: FilePageQuery): Promise<PageResult<SysFile>> {
  const data = await requestClient.post<any>(
    `${FILE_API}/Page`,
    buildPageRequest(params, {
      keywordFields: ['FileName', 'OriginalName', 'FileHash'],
      filterFieldMap: {
        status: 'Status',
        fileType: 'FileType',
      },
    }),
  )
  return normalizePageResult(data, normalizeFile)
}

export function getFileDetailApi(id: string) {
  return requestClient
    .get<any>(`${FILE_API}/ById`, { params: { id } })
    .then(raw => normalizeFile(raw))
}

export function createFileApi(data: FilePayload) {
  return requestClient.post<void>(`${FILE_API}/Create`, toFileCreatePayload(data))
}

export function updateFileApi(id: string, data: FilePayload) {
  return requestClient.put<void>(`${FILE_API}/Update`, toFileUpdatePayload(id, data), {
    params: { id },
  })
}

export function deleteFileApi(id: string) {
  return requestClient.delete<void>(`${FILE_API}/Delete`, {
    params: { id },
  })
}

export function getFileByHashApi(fileHash: string, tenantId?: number) {
  return requestClient
    .get<any>(`${FILE_API}/ByFileHash/${tenantId ?? 0}`, {
      params: { fileHash },
    })
    .then(raw => (raw ? normalizeFile(raw) : null))
}
