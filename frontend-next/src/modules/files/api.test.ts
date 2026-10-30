import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '~/request'
import { MULTIPART_THRESHOLD, uploadFile } from './api'

const ops = vi.hoisted(() => ({
  startUpload: vi.fn(),
  signUploadParts: vi.fn(),
  completeUpload: vi.fn(),
  abortUpload: vi.fn(),
}))
const formUpload = vi.hoisted(() => vi.fn())

vi.mock('~/contracts', async importOriginal => ({
  ...await importOriginal<typeof import('~/contracts')>(),
  files: ops,
}))

vi.mock('~/request', async importOriginal => ({
  ...await importOriginal<typeof import('~/request')>(),
  uploadForm: formUpload,
}))

const MiB = 1024 * 1024

/** 只记下 PUT 了什么；状态码按地址里的分片编号决定（缺省 200）。 */
class FakeXhr {
  static sent: Array<{ url: string, size: number }> = []
  static failing = new Set<string>()

  upload: { onprogress: ((event: { loaded: number }) => void) | null } = { onprogress: null }
  status = 0
  responseText = ''
  onload: (() => void) | null = null
  onerror: (() => void) | null = null
  onabort: (() => void) | null = null
  private url = ''

  open(_method: string, url: string) {
    this.url = url
  }

  getResponseHeader() {
    return 'application/problem+json'
  }

  send(body: Blob) {
    FakeXhr.sent.push({ url: this.url, size: body.size })
    queueMicrotask(() => {
      this.upload.onprogress?.({ loaded: body.size })
      const failed = FakeXhr.failing.has(this.url)
      this.status = failed ? 403 : 200
      this.responseText = failed ? JSON.stringify({ code: 'OBJECTSTORAGE_DENIED_001', detail: 'The signed URL is invalid.', status: 403 }) : ''
      this.onload?.()
    })
  }

  abort() {
    this.onabort?.()
  }
}

function fileOf(size: number): File {
  return new File([new Uint8Array(size)], 'big.bin', { type: 'application/octet-stream' })
}

beforeEach(() => {
  FakeXhr.sent = []
  FakeXhr.failing = new Set()
  vi.stubGlobal('XMLHttpRequest', FakeXhr)
  ops.startUpload.mockResolvedValue({ id: 'u1', partSize: 8 * MiB, partCount: 3 })
  ops.signUploadParts.mockImplementation(async ({ partNumbers }: { partNumbers: number[] }) => ({
    parts: partNumbers.map(n => ({ partNumber: n, url: `/blobs/files/k?x-part=${n}`, expiresAt: '2026-10-30T00:30:00Z' })),
  }))
  ops.completeUpload.mockResolvedValue({ id: 'f1' })
  ops.abortUpload.mockResolvedValue({ id: 'u1', status: 'Aborted' })
  formUpload.mockResolvedValue({ id: 'small' })
})

afterEach(() => {
  vi.unstubAllGlobals()
  vi.clearAllMocks()
})

describe('uploadFile', () => {
  it('sends small files in one form request', async () => {
    expect(await uploadFile(fileOf(1024))).toEqual({ id: 'small' })
    expect(formUpload).toHaveBeenCalledOnce()
    expect(ops.startUpload).not.toHaveBeenCalled()
  })

  it('splits large files into signed part uploads and completes them', async () => {
    const progress: number[] = []
    const created = await uploadFile(fileOf(MULTIPART_THRESHOLD + 10), { tags: ' a ', onProgress: p => progress.push(p) })

    expect(created).toEqual({ id: 'f1' })
    expect(ops.startUpload).toHaveBeenCalledWith(expect.objectContaining({ originalName: 'big.bin', size: MULTIPART_THRESHOLD + 10, tags: 'a' }), expect.anything())
    expect(ops.signUploadParts).toHaveBeenCalledWith({ uploadId: 'u1', partNumbers: [1, 2, 3] }, expect.anything())
    expect(FakeXhr.sent.map(s => s.size).sort((a, b) => a - b)).toEqual([10, 8 * MiB, 8 * MiB])
    expect(FakeXhr.sent.every(s => s.url.startsWith('/blobs/files/k'))).toBe(true)
    expect(ops.completeUpload).toHaveBeenCalledWith({ uploadId: 'u1' }, expect.anything())
    expect(progress.at(-1)).toBe(100)
    expect(progress.slice(0, -1).every(p => p <= 99)).toBe(true)
    expect(ops.abortUpload).not.toHaveBeenCalled()
  })

  it('aborts the upload and rethrows when a part is rejected', async () => {
    FakeXhr.failing.add('/blobs/files/k?x-part=2')

    const failure = await uploadFile(fileOf(MULTIPART_THRESHOLD + 10)).catch((error: unknown) => error)

    expect(failure).toBeInstanceOf(ApiError)
    expect((failure as ApiError).code).toBe('OBJECTSTORAGE_DENIED_001')
    expect(ops.completeUpload).not.toHaveBeenCalled()
    expect(ops.abortUpload).toHaveBeenCalledWith({ uploadId: 'u1' })
  })
})
