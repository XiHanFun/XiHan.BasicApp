/**
 * 图片裁剪的解码与出图（image-crop.ts）：
 * 初始裁切框锁比例时取居中的最大框、不锁取整张图；出图是整块矩形、宽度封顶不放大，
 * 原图是 JPEG 或要求不透明时出 JPEG 并先铺白底，其余出 PNG；解不出来的图片拒绝并回收对象地址
 */
import type { ImageCropSource } from '~/components/common/image-crop'
import { cropToCanvas } from '@xihan-ui/headless'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { exportCroppedImage, initialCropRect, loadImageCropSource, releaseImageCropSource } from '~/components/common/image-crop'

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

function sourceOf(name: string, type: string): ImageCropSource {
  return {
    file: new File(['raw'], name, { type }),
    image: { naturalWidth: 2000, naturalHeight: 1000 } as HTMLImageElement,
    url: 'blob:image',
  }
}

beforeEach(() => {
  vi.mocked(cropToCanvas).mockReset()
})

afterEach(() => {
  vi.unstubAllGlobals()
  vi.restoreAllMocks()
})

describe('初始裁切框', () => {
  it('锁 1:1 时是居中的最大正方形', () => {
    expect(initialCropRect({ naturalWidth: 800, naturalHeight: 600 }, 1)).toEqual({
      x: 100,
      y: 0,
      width: 600,
      height: 600,
    })
    expect(initialCropRect({ naturalWidth: 300, naturalHeight: 900 }, 1)).toEqual({
      x: 0,
      y: 300,
      width: 300,
      height: 300,
    })
  })

  it('锁别的比例时按比例取居中的最大框', () => {
    expect(initialCropRect({ naturalWidth: 1000, naturalHeight: 1000 }, 2)).toEqual({
      x: 0,
      y: 250,
      width: 1000,
      height: 500,
    })
  })

  it('不锁比例时是整张图', () => {
    expect(initialCropRect({ naturalWidth: 1600, naturalHeight: 900 })).toEqual({
      x: 0,
      y: 0,
      width: 1600,
      height: 900,
    })
  })
})

describe('出图', () => {
  it('宽度封顶、不放大小图，PNG 不铺底色，文件名与格式随原图', async () => {
    vi.mocked(cropToCanvas).mockReturnValue(canvasOf())
    const source = sourceOf('me.png', 'image/png')

    const large = await exportCroppedImage(source, {
      x: 500,
      y: 0,
      width: 1000,
      height: 1000,
    }, { maxWidth: 512 })
    expect(cropToCanvas).toHaveBeenLastCalledWith(source.image, {
      x: 500,
      y: 0,
      width: 1000,
      height: 1000,
    }, { width: 512, background: undefined })
    expect(large).toMatchObject({ name: 'me.png', type: 'image/png' })

    await exportCroppedImage(source, {
      x: 0,
      y: 0,
      width: 120.4,
      height: 120.4,
    }, { maxWidth: 512 })
    expect(cropToCanvas).toHaveBeenLastCalledWith(source.image, expect.anything(), { width: 120, background: undefined })
  })

  it('原图是 JPEG 时先铺白底再出图，仍出 JPEG', async () => {
    vi.mocked(cropToCanvas).mockReturnValue(canvasOf())
    const source = sourceOf('me.jpg', 'image/jpeg')

    const file = await exportCroppedImage(source, { x: 0, y: 0, width: 600, height: 600 }, { maxWidth: 512 })
    expect(cropToCanvas).toHaveBeenLastCalledWith(source.image, expect.anything(), { width: 512, background: '#fff' })
    expect(file).toMatchObject({ name: 'me.jpg', type: 'image/jpeg' })
  })

  it('要求不透明时 PNG 原图也铺白底出 JPEG', async () => {
    vi.mocked(cropToCanvas).mockReturnValue(canvasOf())
    const source = sourceOf('bg.png', 'image/png')

    const file = await exportCroppedImage(source, {
      x: 0,
      y: 0,
      width: 3000,
      height: 1500,
    }, { maxWidth: 1920, opaque: true })
    expect(cropToCanvas).toHaveBeenLastCalledWith(source.image, expect.anything(), { width: 1920, background: '#fff' })
    expect(file).toMatchObject({ type: 'image/jpeg' })
  })

  it('画不出来或编码不出来都回 null', async () => {
    const source = sourceOf('me.png', 'image/png')
    vi.mocked(cropToCanvas).mockReturnValue(null)
    expect(await exportCroppedImage(source, { x: 0, y: 0, width: 600, height: 600 }, { maxWidth: 512 })).toBeNull()
    vi.mocked(cropToCanvas).mockReturnValue(canvasOf(false))
    expect(await exportCroppedImage(source, { x: 0, y: 0, width: 600, height: 600 }, { maxWidth: 512 })).toBeNull()
  })
})

describe('解码', () => {
  it('解码成功回文件、图片与对象地址；解不出来或尺寸为 0 即拒绝并回收对象地址', async () => {
    const createObjectURL = vi.fn(() => 'blob:image')
    const revokeObjectURL = vi.fn()
    vi.stubGlobal('URL', Object.assign(URL, { createObjectURL, revokeObjectURL }))
    const file = new File(['raw'], 'me.png', { type: 'image/png' })
    const size = { width: 640, height: 480 }
    vi.spyOn(HTMLImageElement.prototype, 'naturalWidth', 'get').mockImplementation(() => size.width)
    vi.spyOn(HTMLImageElement.prototype, 'naturalHeight', 'get').mockImplementation(() => size.height)
    const decode = vi.fn(async () => {})
    HTMLImageElement.prototype.decode = decode

    const source = await loadImageCropSource(file)
    expect(source).toMatchObject({ file, url: 'blob:image' })
    expect(source.image.naturalWidth).toBe(640)
    expect(revokeObjectURL).not.toHaveBeenCalled()
    releaseImageCropSource(source)
    expect(revokeObjectURL).toHaveBeenCalledWith('blob:image')

    revokeObjectURL.mockClear()
    size.width = 0
    await expect(loadImageCropSource(file)).rejects.toThrow()
    expect(revokeObjectURL).toHaveBeenCalledWith('blob:image')

    revokeObjectURL.mockClear()
    size.width = 640
    decode.mockRejectedValueOnce(new Error('cannot decode'))
    await expect(loadImageCropSource(file)).rejects.toThrow('cannot decode')
    expect(revokeObjectURL).toHaveBeenCalledWith('blob:image')
  })
})
