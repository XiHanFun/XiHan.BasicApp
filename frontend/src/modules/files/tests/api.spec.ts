import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { downloadFile, MULTIPART_THRESHOLD, uploadFile } from '@/modules/files/api'
import { $t } from '~/locales'
import { ApiError } from '~/request'

const ops = vi.hoisted(() => ({
  presign: vi.fn(),
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

/** 只记下 PUT 了什么；状态码按地址里的分片编号决定（缺省 200） */
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
      this.responseText = failed
        ? JSON.stringify({ code: 'OBJECTSTORAGE_DENIED_001', detail: 'The signed URL is invalid.', status: 403 })
        : ''
      this.onload?.()
    })
  }

  abort() {
    this.onabort?.()
  }
}

function fileOf(size: number): File {
  return new File([
    new Uint8Array(size),
  ], 'big.bin', { type: 'application/octet-stream' })
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
    const created = await uploadFile(fileOf(MULTIPART_THRESHOLD + 10), {
      tags: ' a ',
      onProgress: p => progress.push(p),
    })

    expect(created).toEqual({ id: 'f1' })
    expect(ops.startUpload).toHaveBeenCalledWith(expect.objectContaining({ originalName: 'big.bin', size: MULTIPART_THRESHOLD + 10, tags: 'a' }), expect.anything())
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
