import type { ResponseOf } from '~/contracts'
import { files as filesOps } from '~/contracts'
import { endpointUrl, parseProblem, uploadForm } from '~/request'

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

/** 超过这个大小（字节）的文件走分片上传，其余走一次请求的普通上传。 */
export const MULTIPART_THRESHOLD = 16 * 1024 * 1024

/** 同时上传的分片数。 */
const PART_CONCURRENCY = 3

/** 一次签出的分片地址数（与后端 `files.sign-upload-parts` 的上限一致）。 */
const SIGN_BATCH = 100

/**
 * 上传一个文件，返回新文件：小文件走普通端点 `POST /api/files/upload`（multipart 字段 `file`），
 * 超过 {@link MULTIPART_THRESHOLD} 的走分片上传（各分片经签名地址直传存储，后端拼好后回读校验）。
 */
export function uploadFile(file: File, options: UploadFileOptions = {}): Promise<FileRow> {
  return file.size > MULTIPART_THRESHOLD ? uploadInParts(file, options) : uploadInOneRequest(file, options)
}

function uploadInOneRequest(file: File, options: UploadFileOptions): Promise<FileRow> {
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
 * 分片上传：发起 → 按批签出分片地址 → 并发 PUT 各分片（不带令牌，地址即授权）→ 完成。
 * 进度按已发出的字节算，完成前停在 99；失败或中止时放弃这次上传，丢弃已上传的分片。
 */
async function uploadInParts(file: File, options: UploadFileOptions): Promise<FileRow> {
  const signal = options.signal
  const upload = await filesOps.startUpload({
    originalName: file.name,
    size: file.size,
    contentType: file.type || null,
    accessLevel: options.accessLevel,
    isTemporary: options.isTemporary ?? false,
    expiresAt: options.expiresAt ?? null,
    tags: options.tags?.trim() || null,
    remark: options.remark?.trim() || null,
  }, { signal })
  const sent = Array.from<number>({ length: upload.partCount }).fill(0)
  const report = () => options.onProgress?.(Math.min(99, Math.round((sent.reduce((total, bytes) => total + bytes, 0) / file.size) * 100)))
  try {
    for (let first = 1; first <= upload.partCount; first += SIGN_BATCH) {
      const numbers = Array.from({ length: Math.min(SIGN_BATCH, upload.partCount - first + 1) }, (_, index) => first + index)
      const { parts } = await filesOps.signUploadParts({ uploadId: upload.id, partNumbers: numbers }, { signal })
      await eachWithConcurrency(parts, PART_CONCURRENCY, async (part) => {
        const start = (part.partNumber - 1) * upload.partSize
        const chunk = file.slice(start, Math.min(start + upload.partSize, file.size))
        await putPart(endpointUrl(part.url), chunk, (loaded) => {
          sent[part.partNumber - 1] = loaded
          report()
        }, signal)
        sent[part.partNumber - 1] = chunk.size
        report()
      })
    }
    const created = await filesOps.completeUpload({ uploadId: upload.id }, { signal })
    options.onProgress?.(100)
    return created
  }
  catch (error) {
    // 放弃这次上传，原来的错误照常抛出；放弃失败的由后端的过期作业兜底清理
    filesOps.abortUpload({ uploadId: upload.id }).catch(() => undefined)
    throw error
  }
}

/** 以至多 `limit` 个并发依次处理 `items`；任一失败即整体失败。 */
async function eachWithConcurrency<T>(items: readonly T[], limit: number, worker: (item: T) => Promise<void>): Promise<void> {
  let next = 0
  const lanes = Array.from({ length: Math.min(limit, items.length) }, async () => {
    while (next < items.length) {
      const item = items[next++] as T
      await worker(item)
    }
  })
  await Promise.all(lanes)
}

/** PUT 一个分片到签名地址；用 XMLHttpRequest 是为了拿上传进度。失败抛 `ApiError`（签名端点回 Problem Details）。 */
function putPart(url: string, chunk: Blob, onProgress: (loaded: number) => void, signal?: AbortSignal): Promise<void> {
  return new Promise((resolve, reject) => {
    if (signal?.aborted) {
      reject(new DOMException('The upload was aborted.', 'AbortError'))
      return
    }
    const xhr = new XMLHttpRequest()
    xhr.open('PUT', url)
    xhr.upload.onprogress = (event) => {
      onProgress(event.loaded)
    }
    xhr.onload = () => {
      if (xhr.status >= 200 && xhr.status < 300) {
        resolve()
        return
      }
      const response = new Response(xhr.responseText || null, { status: xhr.status, headers: { 'Content-Type': xhr.getResponseHeader('Content-Type') ?? 'text/plain' } })
      parseProblem(response, '').then(reject, reject)
    }
    xhr.onerror = () => reject(new TypeError('Network request failed'))
    xhr.onabort = () => reject(new DOMException('The upload was aborted.', 'AbortError'))
    signal?.addEventListener('abort', () => xhr.abort(), { once: true })
    xhr.send(chunk)
  })
}

/**
 * 换取签名下载地址（完整地址，给图片、链接与新窗口用；有效期内不再校验身份）。
 * 地址由对象存储签出、带签名参数，原样使用：本地存储是后端宿主的 `/blobs/…`，S3 兼容存储直连存储。
 * `download` 为真时以附件下载（带原始文件名），否则按内容类型内联显示。
 */
export async function fileLink(id: string, download = false): Promise<string> {
  const { url } = await filesOps.presign(download ? { id, download } : { id })
  return endpointUrl(url)
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
