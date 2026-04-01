import type { PageQuery } from '~/types'
import { useBaseApi } from '../base'
import { toId, toNumber } from '../helpers'

const api = useBaseApi('File')

export interface SysFile {
  basicId: string
  fileName: string
  originalName: string
  fileExtension?: string
  fileType: number
  mimeType?: string
  fileSize: number
  fileHash?: string
  isPublic: boolean
  requireAuth: boolean
  isTemporary: boolean
  expiresAt?: string
  status: number
  tags?: string
  createTime?: string
  updateTime?: string
  remark?: string
}

export interface FilePageQuery extends PageQuery {
  status?: number
  fileType?: number
}

type FilePayload = Partial<SysFile> & { accessPermissions?: string }

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

function toCreatePayload(data: FilePayload) {
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

function toUpdatePayload(id: string, data: FilePayload) {
  return {
    ...toCreatePayload(data),
    status: toNumber(data.status, 0),
    basicId: toId(id),
  }
}

export const fileApi = {
  page: (params: Record<string, any>) =>
    api.page(params, {
      keywordFields: ['FileName', 'OriginalName', 'FileHash'],
      filterFieldMap: { status: 'Status', fileType: 'FileType' },
    }),

  detail: (id: string) =>
    api.request.get<any>(`${api.baseUrl}ById`, { params: { id } }).then(normalizeFile),

  create: (data: FilePayload) => api.create(toCreatePayload(data)),

  update: (id: string, data: FilePayload) =>
    api.request.put(`${api.baseUrl}Update`, toUpdatePayload(id, data), { params: { id } }),

  delete: (id: string) => api.delete(id),

  getByHash: (fileHash: string, tenantId?: number) =>
    api.request
      .get<any>(`${api.baseUrl}ByFileHash/${tenantId ?? 0}`, { params: { fileHash } })
      .then((raw: any) => (raw ? normalizeFile(raw) : null)),
}
