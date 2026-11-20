import { afterEach, describe, expect, it, vi } from 'vitest'
import { downloadFile } from '@/modules/files/api'
import { $t } from '~/locales'
import { ApiError } from '~/request'

const ops = vi.hoisted(() => ({
  presign: vi.fn(),
}))

vi.mock('~/contracts', async importOriginal => ({
  ...await importOriginal<typeof import('~/contracts')>(),
  files: ops,
}))

afterEach(() => {
  vi.unstubAllGlobals()
  vi.clearAllMocks()
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
