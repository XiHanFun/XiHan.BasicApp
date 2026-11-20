/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import type { FileAccessLevel, ResponseOf } from '~/contracts'
import { files as filesOps } from '~/contracts'
import { $t } from '~/locales'
import { ApiError, endpointUrl, LONG_TIMEOUT_MS, parseProblem, uploadForm } from '~/request'
import { canHashForFastUpload, LocalStorage, sha256Hex } from '~/utils'

type FileRow = ResponseOf<'files.get'>
type UploadSession = ResponseOf<'files.start-upload'>

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

/** 超过这个大小（字节）的文件走分片上传，其余走一次请求的普通上传 */
export const MULTIPART_THRESHOLD = 16 * 1024 * 1024

/** 同时上传的分片数 */
const PART_CONCURRENCY = 3

/** 一次签出的分片地址数（与后端 `files.sign-upload-parts` 的上限一致） */
const SIGN_BATCH = 100

/** 一个分片最多 PUT 几次（含第一次） */
const PART_ATTEMPTS = 3

/** 第 n 次重试前等 `RETRY_BASE_MS * 2^(n-1)` 毫秒 */
const RETRY_BASE_MS = 1000

/** 记下没传完的分片上传，同一个文件以同样的附加信息再上传时续传 */
const PENDING_UPLOADS_KEY = 'xihan:files:pending-uploads'

/** 最多记几次没传完的上传，多了丢掉最早的 */
const PENDING_UPLOADS_LIMIT = 20

/**
 * 分片上传因网络或存储暂时不可用中断
 * 已传的分片留在服务端，同一个文件以同样的附加信息再上传时只补没传完的；原来的错误在 `cause`
 */
export class ResumableUploadError extends Error {
  constructor(cause: unknown) {
    super($t('file_upload.interrupted'), { cause })
    this.name = 'ResumableUploadError'
  }
}

/**
 * 上传一个文件，返回新文件
 * 先试秒传：这一侧已有内容相同的文件即直接得到新文件（见 {@link canHashForFastUpload}）
 * 小文件走普通端点 `POST /api/files/upload`（multipart 字段 `file`）
 * 超过 {@link MULTIPART_THRESHOLD} 的分片直传存储，后端拼好后回读校验；中断了可以续传（{@link ResumableUploadError}）
 */
export async function uploadFile(file: File, options: UploadFileOptions = {}): Promise<FileRow> {
  const fast = await tryFastUpload(file, options)
  if (fast) {
    options.onProgress?.(100)
    return fast
  }
  return file.size > MULTIPART_THRESHOLD
    ? uploadInParts(file, options)
    : uploadInOneRequest(file, options)
}

/** 附加信息，发起分片上传与秒传共用；空白的标签与备注不发 */
function detailsOf(options: UploadFileOptions) {
  return {
    accessLevel: options.accessLevel,
    isTemporary: options.isTemporary ?? false,
    expiresAt: options.expiresAt ?? null,
    tags: options.tags?.trim() || null,
    remark: options.remark?.trim() || null,
  }
}

/**
 * 秒传探测，没命中返回 null
 * 探测本身失败不阻断，照常上传，上传的失败照常抛出；中止照常抛出
 */
async function tryFastUpload(file: File, options: UploadFileOptions): Promise<FileRow | null> {
  if (!canHashForFastUpload(file))
    return null
  try {
    const { file: created } = await filesOps.fastUpload({
      hash: await sha256Hex(file),
      size: file.size,
      originalName: file.name,
      contentType: file.type || null,
      ...detailsOf(options),
    }, { signal: options.signal })
    return created ?? null
  }
  catch (error) {
    if (isAbortError(error))
      throw error
    return null
  }
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
 * 分片上传，各分片 PUT 到签名地址时不带令牌（地址即授权）
 * 同一个文件以同样的附加信息上传过、还没传完的，接着那次上传只补缺的分片
 * 每个分片失败了换新地址重试（{@link PART_ATTEMPTS}）；进度按已传的字节算，完成前停在 99
 * 网络或存储暂时不可用而中断的留着这次上传、抛 {@link ResumableUploadError}；中止与别的失败放弃上传
 */
async function uploadInParts(file: File, options: UploadFileOptions): Promise<FileRow> {
  const signal = options.signal
  const details = detailsOf(options)
  const key = pendingKey(file, details)
  const upload = await resumableUpload(key, file, signal) ?? await filesOps.startUpload({
    originalName: file.name,
    size: file.size,
    contentType: file.type || null,
    ...details,
  }, { signal })
  rememberUpload(key, upload)

  const uploaded = new Set(upload.uploadedParts)
  const sizeOf = (partNumber: number) => Math.min(upload.partSize, file.size - (partNumber - 1) * upload.partSize)
  const sent = Array.from({ length: upload.partCount }, (_, index) => uploaded.has(index + 1)
    ? sizeOf(index + 1)
    : 0)
  const report = () => options.onProgress?.(Math.min(99, Math.round((sent.reduce((total, bytes) => total + bytes, 0) / file.size) * 100)))
  report()
  const missing = Array.from({ length: upload.partCount }, (_, index) => index + 1).filter(partNumber => !uploaded.has(partNumber))
  const signer = partSigner(upload.id, missing, signal)
  try {
    await eachWithConcurrency(missing, PART_CONCURRENCY, async (partNumber) => {
      const start = (partNumber - 1) * upload.partSize
      const chunk = file.slice(start, start + sizeOf(partNumber))
      await withRetries(signal, async (attempt) => {
        // 重试时换新地址：前一个可能已经过期
        const url = await signer.urlFor(partNumber, attempt > 1)
        sent[partNumber - 1] = 0
        await putPart(endpointUrl(url), chunk, (loaded) => {
          sent[partNumber - 1] = loaded
          report()
        }, signal)
      })
      sent[partNumber - 1] = chunk.size
      report()
    })
  }
  catch (error) {
    if (!isAbortError(error) && isTransient(error))
      throw new ResumableUploadError(error)
    // 放弃这次上传，原来的错误照常抛出；放弃失败的由后端的过期作业兜底清理
    forgetUpload(key)
    filesOps.abortUpload({ uploadId: upload.id }).catch(() => undefined)
    throw error
  }
  // 完成失败的不在这里放弃：没拼成的上传服务端已记为失败，再上传时查到就重新发起
  const created = await filesOps.completeUpload({ uploadId: upload.id }, { signal, timeoutMs: LONG_TIMEOUT_MS })
  forgetUpload(key)
  options.onProgress?.(100)
  return created
}

/**
 * 记下的那次上传还能接着传就取回来（含已传的分片），否则忘掉它
 * 上传中的才能接着传；查不到（过期清掉了、换了账号）的重新发起
 */
async function resumableUpload(key: string, file: File, signal?: AbortSignal): Promise<UploadSession | null> {
  const saved = pendingUploads()[key]
  if (!saved)
    return null
  try {
    const session = await filesOps.uploadSession({ uploadId: saved.uploadId }, { signal })
    if (session.status === 'Uploading' && session.size === file.size)
      return session
  }
  catch (error) {
    if (!(error instanceof ApiError && error.status === 404))
      throw error
  }
  forgetUpload(key)
  return null
}

interface PendingUpload {
  uploadId: string
  /** ISO 8601，上传的截止时间 */
  expiresAt: string
}

/** 同一个文件（名称、大小、修改时间、类型）以同样的附加信息上传才算同一次 */
function pendingKey(file: File, details: ReturnType<typeof detailsOf>): string {
  return JSON.stringify([
    file.name,
    file.size,
    file.lastModified,
    file.type,
    details,
  ])
}

/** 还没过截止时间的记录；本地存储不可用时为空，只是不能跨次续传 */
function pendingUploads(): Record<string, PendingUpload> {
  const saved = LocalStorage.get<Record<string, PendingUpload>>(PENDING_UPLOADS_KEY) ?? {}
  const now = Date.now()
  return Object.fromEntries(Object.entries(saved).filter(([, entry]) => Date.parse(entry.expiresAt) > now))
}

function rememberUpload(key: string, upload: UploadSession) {
  const entries = Object.entries(pendingUploads()).filter(([saved]) => saved !== key)
  entries.push([
    key,
    { uploadId: upload.id, expiresAt: upload.expiresAt },
  ])
  LocalStorage.set(PENDING_UPLOADS_KEY, Object.fromEntries(entries.slice(-PENDING_UPLOADS_LIMIT)))
}

function forgetUpload(key: string) {
  const entries = Object.entries(pendingUploads()).filter(([saved]) => saved !== key)
  LocalStorage.set(PENDING_UPLOADS_KEY, entries.length > 0
    ? Object.fromEntries(entries)
    : undefined)
}

/**
 * 按需签分片地址：缺地址的分片连同后面还没签的一起签（一次至多 {@link SIGN_BATCH} 个），同一时刻只签一批
 * `renew` 为真时丢掉这一片原来的地址重签
 */
function partSigner(uploadId: string, order: readonly number[], signal?: AbortSignal) {
  const signed = new Map<number, string>()
  let signing: Promise<void> | null = null

  async function sign(partNumber: number) {
    const from = order.indexOf(partNumber)
    const batch = [
      partNumber,
      ...order.slice(from + 1).filter(next => !signed.has(next)),
    ].slice(0, SIGN_BATCH)
    const { parts } = await filesOps.signUploadParts({ uploadId, partNumbers: batch }, { signal })
    for (const part of parts)
      signed.set(part.partNumber, part.url)
  }

  return {
    async urlFor(partNumber: number, renew: boolean): Promise<string> {
      if (renew)
        signed.delete(partNumber)
      // 正在签的那批可能带着这一片，先等它签完；它的失败由发起它的那一片处理
      // 没在签时不能 await：让出去的这一下别的分片也会发起签名
      for (let pending = signing; pending && !signed.has(partNumber); pending = signing)
        await pending.catch(() => undefined)
      if (!signed.has(partNumber)) {
        const batch = sign(partNumber)
        signing = batch
        try {
          await batch
        }
        finally {
          if (signing === batch)
            signing = null
        }
      }
      const url = signed.get(partNumber)
      if (url === undefined)
        throw new Error(`files.sign-upload-parts returned no URL for part ${partNumber}`)
      return url
    },
  }
}

/** 换个地址再试可能就好的失败：网络不通、签名地址过期（403）、超时、限流与服务端错误 */
function isRetryable(error: unknown): boolean {
  return isTransient(error) || (error instanceof ApiError && error.status === 403)
}

/** 过一会儿再传可能就好的失败：网络不通、超时、限流与服务端错误 */
function isTransient(error: unknown): boolean {
  if (error instanceof TypeError)
    return true
  return error instanceof ApiError && (error.status === 408 || error.status === 429 || error.status >= 500)
}

function isAbortError(error: unknown): boolean {
  return error instanceof DOMException && error.name === 'AbortError'
}

/** 至多试 {@link PART_ATTEMPTS} 次，两次之间按指数退避；不可重试的失败与中止立即抛出 */
async function withRetries(signal: AbortSignal | undefined, attempt: (count: number) => Promise<void>): Promise<void> {
  for (let count = 1; ; count++) {
    try {
      await attempt(count)
      return
    }
    catch (error) {
      if (count >= PART_ATTEMPTS || isAbortError(error) || !isRetryable(error))
        throw error
      await delay(RETRY_BASE_MS * 2 ** (count - 1), signal)
    }
  }
}

/** 可中止的等待 */
function delay(ms: number, signal?: AbortSignal): Promise<void> {
  return new Promise((resolve, reject) => {
    const aborted = () => new DOMException('The upload was aborted.', 'AbortError')
    if (signal?.aborted) {
      reject(aborted())
      return
    }
    const timer = setTimeout(resolve, ms)
    signal?.addEventListener('abort', () => {
      clearTimeout(timer)
      reject(aborted())
    }, { once: true })
  })
}

/** 以至多 `limit` 个并发依次处理 `items`；任一失败即整体失败，其余并发不再取新的 */
async function eachWithConcurrency<T>(items: readonly T[], limit: number, worker: (item: T) => Promise<void>): Promise<void> {
  let next = 0
  let failed = false
  const lanes = Array.from({
    length: Math.min(limit, items.length),
  }, async () => {
    while (!failed && next < items.length) {
      const item = items[next++] as T
      try {
        await worker(item)
      }
      catch (error) {
        failed = true
        throw error
      }
    }
  })
  await Promise.all(lanes)
}

/**
 * PUT 一个分片到签名地址，用 XMLHttpRequest 是为了拿上传进度
 * 失败抛 `ApiError`（签名端点回 Problem Details）
 */
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
      const response = new Response(xhr.responseText || null, {
        status: xhr.status,
        headers: { 'Content-Type': xhr.getResponseHeader('Content-Type') ?? 'text/plain' },
      })
      parseProblem(response, '').then(reject, reject)
    }
    xhr.onerror = () => reject(new TypeError('Network request failed'))
    xhr.onabort = () => reject(new DOMException('The upload was aborted.', 'AbortError'))
    signal?.addEventListener('abort', () => xhr.abort(), { once: true })
    xhr.send(chunk)
  })
}
