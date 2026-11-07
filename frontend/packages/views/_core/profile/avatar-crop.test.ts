/**
 * 头像裁剪的解码与出图（avatar-crop.ts）：
 * 初始裁切框是居中的最大正方形；出图是整块正方形（不带圆形）、边长封顶 512、格式随原图，JPEG 先铺白底；
 * 解不出来的图片拒绝并回收对象地址
 */
import type { AvatarCropSource } from './avatar-crop'
import { cropToCanvas } from '@xihan-ui/headless'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { centeredSquare, exportAvatar, loadAvatarSource, releaseAvatarSource } from './avatar-crop'

vi.mock('@xihan-ui/headless', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@xihan-ui/headless')>()
  return {
    ...actual,
    cropToCanvas: vi.fn(),
  }
})

/** 假画布：toBlob 按要的格式回一块数据，encodable 为假时回 null */
function canvasOf(encodable = true) {
  return {
    toBlob: (done: (blob: Blob | null) => void, type: string) => done(encodable
      ? new Blob(['pixels'], { type })
      : null),
  } as unknown as HTMLCanvasElement
}

function sourceOf(name: string, type: string): AvatarCropSource {
  return {
    file: new File(['raw'], name, { type }),
    image: { naturalWidth: 2000, naturalHeight: 1000 } as HTMLImageElement,
    url: 'blob:avatar',
  }
}

beforeEach(() => {
  vi.mocked(cropToCanvas).mockReset()
})

afterEach(() => {
  vi.unstubAllGlobals()
  vi.restoreAllMocks()
})

describe('头像裁剪', () => {
  it('初始裁切框是居中的最大正方形', () => {
    expect(centeredSquare({ naturalWidth: 800, naturalHeight: 600 })).toEqual({
      x: 100,
      y: 0,
      width: 600,
      height: 600,
    })
    expect(centeredSquare({ naturalWidth: 300, naturalHeight: 900 })).toEqual({
      x: 0,
      y: 300,
      width: 300,
      height: 300,
    })
  })

  it('出图是整块正方形：边长封顶 512、不放大小图，PNG 不铺底色，文件名与格式随原图', async () => {
    vi.mocked(cropToCanvas).mockReturnValue(canvasOf())
    const source = sourceOf('me.png', 'image/png')

    const large = await exportAvatar(source, {
      x: 500,
      y: 0,
      width: 1000,
      height: 1000,
    })
    expect(cropToCanvas).toHaveBeenLastCalledWith(source.image, {
      x: 500,
      y: 0,
      width: 1000,
      height: 1000,
    }, { width: 512, background: undefined })
    expect(large).toMatchObject({ name: 'me.png', type: 'image/png' })

    await exportAvatar(source, {
      x: 0,
      y: 0,
      width: 120.4,
      height: 120.4,
    })
    expect(cropToCanvas).toHaveBeenLastCalledWith(source.image, expect.anything(), { width: 120, background: undefined })
  })

  it('原图是 JPEG 时先铺白底再出图，仍出 JPEG', async () => {
    vi.mocked(cropToCanvas).mockReturnValue(canvasOf())
    const source = sourceOf('me.jpg', 'image/jpeg')

    const file = await exportAvatar(source, { x: 0, y: 0, width: 600, height: 600 })
    expect(cropToCanvas).toHaveBeenLastCalledWith(source.image, expect.anything(), { width: 512, background: '#fff' })
    expect(file).toMatchObject({ name: 'me.jpg', type: 'image/jpeg' })
  })

  it('画不出来或编码不出来都回 null', async () => {
    const source = sourceOf('me.png', 'image/png')
    vi.mocked(cropToCanvas).mockReturnValue(null)
    expect(await exportAvatar(source, { x: 0, y: 0, width: 600, height: 600 })).toBeNull()
    vi.mocked(cropToCanvas).mockReturnValue(canvasOf(false))
    expect(await exportAvatar(source, { x: 0, y: 0, width: 600, height: 600 })).toBeNull()
  })

  it('解码成功回文件、图片与对象地址；解不出来或尺寸为 0 即拒绝并回收对象地址', async () => {
    const createObjectURL = vi.fn(() => 'blob:avatar')
    const revokeObjectURL = vi.fn()
    vi.stubGlobal('URL', Object.assign(URL, { createObjectURL, revokeObjectURL }))
    const file = new File(['raw'], 'me.png', { type: 'image/png' })
    const size = { width: 640, height: 480 }
    vi.spyOn(HTMLImageElement.prototype, 'naturalWidth', 'get').mockImplementation(() => size.width)
    vi.spyOn(HTMLImageElement.prototype, 'naturalHeight', 'get').mockImplementation(() => size.height)
    const decode = vi.fn(async () => {})
    HTMLImageElement.prototype.decode = decode

    const source = await loadAvatarSource(file)
    expect(source).toMatchObject({ file, url: 'blob:avatar' })
    expect(source.image.naturalWidth).toBe(640)
    expect(revokeObjectURL).not.toHaveBeenCalled()
    releaseAvatarSource(source)
    expect(revokeObjectURL).toHaveBeenCalledWith('blob:avatar')

    revokeObjectURL.mockClear()
    size.width = 0
    await expect(loadAvatarSource(file)).rejects.toThrow()
    expect(revokeObjectURL).toHaveBeenCalledWith('blob:avatar')

    revokeObjectURL.mockClear()
    size.width = 640
    decode.mockRejectedValueOnce(new Error('cannot decode'))
    await expect(loadAvatarSource(file)).rejects.toThrow('cannot decode')
    expect(revokeObjectURL).toHaveBeenCalledWith('blob:avatar')
  })
})
