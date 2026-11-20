import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { downloadFile, MULTIPART_THRESHOLD, ResumableUploadError, uploadFile } from '@/modules/files/api'
import { $t } from '~/locales'
import { ApiError } from '~/request'

const ops = vi.hoisted(() => ({
  presign: vi.fn(),
  fastUpload: vi.fn(),
  startUpload: vi.fn(),
  uploadSession: vi.fn(),
  signUploadParts: vi.fn(),
  completeUpload: vi.fn(),
  abortUpload: vi.fn(),
}))
const formUpload = vi.hoisted(() => vi.fn())
const hashing = vi.hoisted(() => ({
  canHashForFastUpload: vi.fn(),
  sha256Hex: vi.fn(),
}))

vi.mock('~/contracts', async importOriginal => ({
  ...await importOriginal<typeof import('~/contracts')>(),
  files: ops,
}))

vi.mock('~/request', async importOriginal => ({
  ...await importOriginal<typeof import('~/request')>(),
  uploadForm: formUpload,
}))

// 摘要本身由 utils 的用例覆盖，这里只看什么时候探测、带了什么
vi.mock('~/utils', async importOriginal => ({
  ...await importOriginal<typeof import('~/utils')>(),
  ...hashing,
}))

const MiB = 1024 * 1024
const HASH = 'a'.repeat(64)
const PENDING_KEY = 'xihan:files:pending-uploads'

type Failure = 'network' | 403

/** 记下 PUT 了什么；按分片编号排好的失败依次消耗（缺省成功） */
class FakeXhr {
  static sent: Array<{ url: string, part: number, size: number }> = []
  static failures = new Map<number, Failure[]>()

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
    const part = Number(new URL(this.url, 'http://localhost').searchParams.get('x-part'))
    FakeXhr.sent.push({ url: this.url, part, size: body.size })
    queueMicrotask(() => {
      const failure = FakeXhr.failures.get(part)?.shift()
      if (failure === 'network') {
        this.onerror?.()
        return
      }
      this.upload.onprogress?.({ loaded: body.size })
      this.status = failure === 403
        ? 403
        : 200
      this.responseText = failure === 403
        ? JSON.stringify({ code: 'OBJECTSTORAGE_DENIED_001', detail: 'The signed URL is invalid.', status: 403 })
        : ''
      this.onload?.()
    })
  }

  abort() {
    this.onabort?.()
  }
}

/** 重试的等待立即结束，记下等了多久 */
function instantRetries(): number[] {
  const waits: number[] = []
  vi.spyOn(globalThis, 'setTimeout').mockImplementation(((handler: () => void, ms?: number) => {
    waits.push(ms ?? 0)
    queueMicrotask(handler)
    return 0
  }) as unknown as typeof setTimeout)
  return waits
}

function fileOf(size: number, name = 'big.bin'): File {
  return new File([
    new Uint8Array(size),
  ], name, { type: 'application/octet-stream', lastModified: 1_700_000_000_000 })
}

/** 三片：8 MiB、8 MiB、10 字节 */
const BIG = MULTIPART_THRESHOLD + 10

let signatures = 0

beforeEach(() => {
  FakeXhr.sent = []
  FakeXhr.failures = new Map()
  signatures = 0
  localStorage.clear()
  vi.stubGlobal('XMLHttpRequest', FakeXhr)
  hashing.canHashForFastUpload.mockReturnValue(true)
  hashing.sha256Hex.mockResolvedValue(HASH)
  ops.fastUpload.mockResolvedValue({ file: null })
  ops.startUpload.mockResolvedValue({
    id: 'u1',
    partSize: 8 * MiB,
    partCount: 3,
    size: BIG,
    status: 'Uploading',
    expiresAt: '2999-01-01T00:00:00Z',
    uploadedParts: [],
  })
  ops.signUploadParts.mockImplementation(async ({ partNumbers }: { partNumbers: number[] }) => ({
    parts: partNumbers.map(n => ({ partNumber: n, url: `/blobs/files/k?x-part=${n}&sig=${++signatures}`, expiresAt: '2999-01-01T00:30:00Z' })),
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
  it('sends small files in one form request when the fast upload misses', async () => {
    expect(await uploadFile(fileOf(1024))).toEqual({ id: 'small' })
    expect(ops.fastUpload).toHaveBeenCalledOnce()
    expect(formUpload).toHaveBeenCalledOnce()
    expect(ops.startUpload).not.toHaveBeenCalled()
  })

  it('takes the fast upload when the side already has the same content', async () => {
    ops.fastUpload.mockResolvedValue({
      file: { id: 'copy' },
    })
    const progress: number[] = []

    const created = await uploadFile(fileOf(BIG), {
      tags: ' a ',
      isTemporary: true,
      onProgress: p => progress.push(p),
    })

    expect(created).toEqual({ id: 'copy' })
    expect(ops.fastUpload).toHaveBeenCalledWith(expect.objectContaining({
      hash: HASH,
      size: BIG,
      originalName: 'big.bin',
      contentType: 'application/octet-stream',
      tags: 'a',
      isTemporary: true,
    }), expect.anything())
    expect(progress).toEqual([100])
    expect(formUpload).not.toHaveBeenCalled()
    expect(ops.startUpload).not.toHaveBeenCalled()
  })

  it('uploads as usual when the fast upload cannot be probed or the probe fails', async () => {
    hashing.canHashForFastUpload.mockReturnValue(false)
    expect(await uploadFile(fileOf(1024))).toEqual({ id: 'small' })
    expect(ops.fastUpload).not.toHaveBeenCalled()

    hashing.canHashForFastUpload.mockReturnValue(true)
    ops.fastUpload.mockRejectedValue(new TypeError('Failed to fetch'))
    expect(await uploadFile(fileOf(1024))).toEqual({ id: 'small' })
    expect(formUpload).toHaveBeenCalledTimes(2)
  })

  it('splits large files into signed part uploads and completes them', async () => {
    const progress: number[] = []
    const created = await uploadFile(fileOf(BIG), {
      tags: ' a ',
      onProgress: p => progress.push(p),
    })

    expect(created).toEqual({ id: 'f1' })
    expect(ops.startUpload).toHaveBeenCalledWith(expect.objectContaining({ originalName: 'big.bin', size: BIG, tags: 'a' }), expect.anything())
    expect(ops.signUploadParts).toHaveBeenCalledOnce()
    expect(ops.signUploadParts).toHaveBeenCalledWith({
      uploadId: 'u1',
      partNumbers: [1, 2, 3],
    }, expect.anything())
    expect(FakeXhr.sent.map(s => s.size).sort((a, b) => a - b)).toEqual([10, 8 * MiB, 8 * MiB])
    expect(FakeXhr.sent.every(s => s.url.startsWith('/blobs/files/k'))).toBe(true)
    expect(ops.completeUpload).toHaveBeenCalledWith({ uploadId: 'u1' }, expect.anything())
    expect(progress.at(-1)).toBe(100)
    expect(progress.slice(0, -1).every(p => p <= 99)).toBe(true)
    expect(ops.abortUpload).not.toHaveBeenCalled()
    // 传完了不再记着
    expect(localStorage.getItem(PENDING_KEY)).toBeNull()
  })

  it('retries a failed part on a freshly signed URL with exponential backoff', async () => {
    const waits = instantRetries()
    FakeXhr.failures.set(2, ['network', 403])

    expect(await uploadFile(fileOf(BIG))).toEqual({ id: 'f1' })

    const second = FakeXhr.sent.filter(s => s.part === 2).map(s => s.url)
    expect(second).toHaveLength(3)
    expect(new Set(second).size).toBe(3)
    expect(waits).toEqual([1000, 2000])
    expect(ops.signUploadParts).toHaveBeenCalledWith({
      uploadId: 'u1',
      partNumbers: [2],
    }, expect.anything())
    expect(ops.abortUpload).not.toHaveBeenCalled()
  })

  it('keeps an interrupted upload and resumes it with only the missing parts', async () => {
    instantRetries()
    FakeXhr.failures.set(2, ['network', 'network', 'network'])
    const file = fileOf(BIG)

    const failure = await uploadFile(file).catch((error: unknown) => error)

    expect(failure).toBeInstanceOf(ResumableUploadError)
    expect((failure as Error).message).toBe($t('files.upload_interrupted'))
    expect((failure as Error).cause).toBeInstanceOf(TypeError)
    expect(ops.abortUpload).not.toHaveBeenCalled()
    expect(ops.completeUpload).not.toHaveBeenCalled()

    // 同一个文件再传：取回那次上传，只补第 2 片
    FakeXhr.sent = []
    ops.uploadSession.mockResolvedValue({
      id: 'u1',
      partSize: 8 * MiB,
      partCount: 3,
      size: BIG,
      status: 'Uploading',
      expiresAt: '2999-01-01T00:00:00Z',
      uploadedParts: [1, 3],
    })
    const progress: number[] = []

    expect(await uploadFile(file, {
      onProgress: p => progress.push(p),
    })).toEqual({ id: 'f1' })

    expect(ops.uploadSession).toHaveBeenCalledWith({ uploadId: 'u1' }, expect.anything())
    expect(ops.startUpload).toHaveBeenCalledOnce()
    expect(FakeXhr.sent.map(s => s.part)).toEqual([2])
    expect(progress[0]).toBe(Math.round(((8 * MiB + 10) / BIG) * 100))
    expect(localStorage.getItem(PENDING_KEY)).toBeNull()
  })

  it('starts over when the remembered upload is gone, ended, or was started with other details', async () => {
    instantRetries()
    FakeXhr.failures.set(1, ['network', 'network', 'network'])
    const file = fileOf(BIG)
    await uploadFile(file).catch(() => undefined)

    // 别的附加信息不是同一次
    await uploadFile(file, { remark: 'other' })
    expect(ops.uploadSession).not.toHaveBeenCalled()
    expect(ops.startUpload).toHaveBeenCalledTimes(2)

    // 记着的那次已经过期
    ops.uploadSession.mockResolvedValueOnce({
      id: 'u1',
      partSize: 8 * MiB,
      partCount: 3,
      size: BIG,
      status: 'Expired',
      expiresAt: '2999-01-01T00:00:00Z',
      uploadedParts: [],
    })
    await uploadFile(file)
    expect(ops.uploadSession).toHaveBeenCalledOnce()
    expect(ops.startUpload).toHaveBeenCalledTimes(3)

    // 再中断一次，这回记着的查不到了
    FakeXhr.failures.set(1, ['network', 'network', 'network'])
    await uploadFile(file).catch(() => undefined)
    ops.uploadSession.mockRejectedValueOnce(new ApiError(404, { code: 'FILES_NOTFOUND_002', detail: 'gone' }))
    await uploadFile(file)
    expect(ops.uploadSession).toHaveBeenCalledTimes(2)
    expect(ops.startUpload).toHaveBeenCalledTimes(5)
    expect(localStorage.getItem(PENDING_KEY)).toBeNull()
  })

  it('abandons the upload and rethrows when a part keeps being rejected', async () => {
    instantRetries()
    FakeXhr.failures.set(2, [403, 403, 403])

    const failure = await uploadFile(fileOf(BIG)).catch((error: unknown) => error)

    expect(failure).toBeInstanceOf(ApiError)
    expect((failure as ApiError).code).toBe('OBJECTSTORAGE_DENIED_001')
    expect(FakeXhr.sent.filter(s => s.part === 2)).toHaveLength(3)
    expect(ops.completeUpload).not.toHaveBeenCalled()
    expect(ops.abortUpload).toHaveBeenCalledWith({ uploadId: 'u1' })
    expect(localStorage.getItem(PENDING_KEY)).toBeNull()
  })

  it('abandons the upload when it is aborted', async () => {
    const controller = new AbortController()
    ops.signUploadParts.mockImplementation(async () => {
      controller.abort()
      throw new DOMException('The operation was aborted.', 'AbortError')
    })

    const failure = await uploadFile(fileOf(BIG), { signal: controller.signal }).catch((error: unknown) => error)

    expect((failure as DOMException).name).toBe('AbortError')
    expect(ops.abortUpload).toHaveBeenCalledWith({ uploadId: 'u1' })
    expect(localStorage.getItem(PENDING_KEY)).toBeNull()
  })
})

describe('downloadFile', () => {
  /** 记下探测请求与浏览器下载 */
  function setup(respond: () => Promise<Response>) {
    ops.presign.mockResolvedValue({ url: '/blobs/files/k?sig=1', expiresAt: '2026-11-21T09:00:00Z' })
    const probes: Array<{ url: string, range: string | null }> = []
    vi.stubGlobal('fetch', vi.fn(async (url: string, init?: RequestInit) => {
      probes.push({
        url,
        range: new Headers(init?.headers).get('Range'),
      })
      return respond()
    }))
    const clicks: string[] = []
    vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(function (this: HTMLAnchorElement) {
      clicks.push(this.getAttribute('href') ?? '')
    })
    return { probes, clicks }
  }

  it('probes the first byte of the signed link, then lets the browser download it', async () => {
    const { probes, clicks } = setup(async () => new Response('x', { status: 206 }))

    await downloadFile('f1')

    expect(ops.presign).toHaveBeenCalledWith({ id: 'f1', download: true })
    expect(probes).toEqual([
      { url: '/blobs/files/k?sig=1', range: 'bytes=0-0' },
    ])
    expect(clicks).toEqual(['/blobs/files/k?sig=1'])
  })

  it('reports the localized problem of the local blob endpoint and does not navigate', async () => {
    const { clicks } = setup(async () => new Response(JSON.stringify({ code: 'FILES_NOTFOUND_001', status: 404, detail: '文件不存在' }), {
      status: 404,
      headers: { 'Content-Type': 'application/problem+json' },
    }))

    const failure = await downloadFile('f1').catch((error: unknown) => error)

    expect(failure).toBeInstanceOf(ApiError)
    expect(failure).toMatchObject({ code: 'FILES_NOTFOUND_001', message: '文件不存在' })
    expect(clicks).toEqual([])
  })

  it('reports the file as unavailable when the storage answers without a problem body or the network fails', async () => {
    const xml = setup(async () => new Response('<Error><Code>AccessDenied</Code></Error>', {
      status: 403,
      headers: { 'Content-Type': 'application/xml' },
    }))
    await expect(downloadFile('f1')).rejects.toThrow($t('files.download_unavailable'))
    expect(xml.clicks).toEqual([])

    vi.restoreAllMocks()
    const offline = setup(async () => {
      throw new TypeError('Failed to fetch')
    })
    await expect(downloadFile('f1')).rejects.toThrow($t('files.download_unavailable'))
    expect(offline.clicks).toEqual([])
  })
})
