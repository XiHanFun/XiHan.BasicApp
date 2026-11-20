import { afterEach, describe, expect, it, vi } from 'vitest'
import { canHashForFastUpload, FAST_UPLOAD_MAX_BYTES, sha256Hex } from '~/utils/content-hash'

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('sha256Hex', () => {
  it('matches the standard SHA-256 vectors in lowercase hex', async () => {
    expect(await sha256Hex(new Blob(['abc']))).toBe('ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad')
    expect(await sha256Hex(new Blob([]))).toBe('e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855')
  })
})

describe('canHashForFastUpload', () => {
  /** 只看大小，不真的分配内容 */
  function sized(size: number): Blob {
    return { size } as Blob
  }

  it('takes non-empty blobs up to the limit', () => {
    expect(canHashForFastUpload(sized(1))).toBe(true)
    expect(canHashForFastUpload(sized(FAST_UPLOAD_MAX_BYTES))).toBe(true)
    expect(canHashForFastUpload(sized(0))).toBe(false)
    expect(canHashForFastUpload(sized(FAST_UPLOAD_MAX_BYTES + 1))).toBe(false)
  })

  it('needs Web Crypto, which only secure contexts have', () => {
    vi.stubGlobal('crypto', {})
    expect(canHashForFastUpload(sized(1))).toBe(false)
  })
})
