import type { ResponseOf } from '~/contracts'
import { files as filesOps } from '~/contracts'
import { endpointUrl, uploadForm } from '~/request'

export type FileRow = ResponseOf<'files.get'>
export type FileAccessLevel = FileRow['accessLevel']

/** 上传时的附加信息（与后端上传端点的表单字段同名） */
export interface UploadFileOptions {
  accessLevel?: FileAccessLevel
  isTemporary?: boolean
  /** ISO 8601 */
  expiresAt?: string | null
  tags?: string | null
  remark?: string | null
  onProgress?: (percent: number) => void
  signal?: AbortSignal
}

/** 上传一个文件（普通端点 `POST /api/files/upload`，multipart 字段 `file`），返回新文件。 */
export function uploadFile(file: File, options: UploadFileOptions = {}): Promise<FileRow> {
  const form = new FormData()
  form.append('file', file, file.name)
  if (options.accessLevel)
    form.append('accessLevel', options.accessLevel)
  if (options.isTemporary)
    form.append('isTemporary', 'true')
  if (options.expiresAt)
    form.append('expiresAt', options.expiresAt)
  if (options.tags?.trim())
    form.append('tags', options.tags.trim())
  if (options.remark?.trim())
    form.append('remark', options.remark.trim())
  return uploadForm<FileRow>('/api/files/upload', form, { onProgress: options.onProgress, signal: options.signal })
}

/**
 * 换取签名下载地址（完整地址，给图片、链接与新窗口用；有效期内不再校验身份）。
 * `download` 为真时以附件下载（带原始文件名），否则按内容类型内联显示。
 */
export async function fileLink(id: string, download = false): Promise<string> {
  const { url } = await filesOps.presign({ id })
  return endpointUrl(download ? `${url}?download=true` : url)
}

/** 读文件内容（预览用）：经签名地址取，不带令牌。 */
export async function fetchFileBlob(id: string): Promise<Blob> {
  const response = await fetch(await fileLink(id))
  if (!response.ok)
    throw new Error(`HTTP ${response.status}`)
  return response.blob()
}

/** 以附件下载：经签名地址让浏览器自己下载（带原始文件名），不把内容读进内存。 */
export async function downloadFile(id: string): Promise<void> {
  const anchor = document.createElement('a')
  anchor.href = await fileLink(id, true)
  anchor.rel = 'noopener'
  document.body.append(anchor)
  anchor.click()
  anchor.remove()
}
