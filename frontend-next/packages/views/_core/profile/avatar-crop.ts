import type { ImageCropperRect } from '@xihan-ui/headless'
import { cropToCanvas } from '@xihan-ui/headless'

/** 待裁剪的头像：选中的文件、解码好的图片与它的对象地址 */
export interface AvatarCropSource {
  file: File
  image: HTMLImageElement
  url: string
}

/** 导出头像的边长上限（像素）：够各处显示用，文件也远小于上传上限 */
const AVATAR_OUTPUT_SIZE = 512
const AVATAR_JPEG_QUALITY = 0.92

/** 解码选中的图片。解不出来（文件损坏或不是图片）即拒绝，对象地址随之回收 */
export async function loadAvatarSource(file: File): Promise<AvatarCropSource> {
  const url = URL.createObjectURL(file)
  const image = new Image()
  image.src = url
  try {
    await image.decode()
    if (!(image.naturalWidth > 0) || !(image.naturalHeight > 0))
      throw new Error('empty image')
  }
  catch (error) {
    URL.revokeObjectURL(url)
    throw error
  }
  return { file, image, url }
}

/** 裁剪结束（上传完成或取消）后回收对象地址 */
export function releaseAvatarSource(source: AvatarCropSource): void {
  URL.revokeObjectURL(source.url)
}

/** 初始裁切框：居中的最大正方形 */
export function centeredSquare(image: Pick<HTMLImageElement, 'naturalWidth' | 'naturalHeight'>): ImageCropperRect {
  const side = Math.min(image.naturalWidth, image.naturalHeight)
  return { x: (image.naturalWidth - side) / 2, y: (image.naturalHeight - side) / 2, width: side, height: side }
}

/**
 * 按裁切框出一张正方形头像，格式随原图
 * 裁切器里的圆形只是取景遮罩：头像在别处也按方形显示，存下来的是整块正方形
 */
export async function exportAvatar(source: AvatarCropSource, rect: ImageCropperRect): Promise<File | null> {
  const type = source.file.type === 'image/png' ? 'image/png' : 'image/jpeg'
  const canvas = cropToCanvas(source.image, rect, {
    width: Math.min(AVATAR_OUTPUT_SIZE, Math.round(rect.width)),
    // JPEG 没有透明通道，透明像素会被编码成黑块，先铺白底
    background: type === 'image/jpeg' ? '#fff' : undefined,
  })
  if (!canvas)
    return null
  const blob = await new Promise<Blob | null>(resolve => canvas.toBlob(resolve, type, AVATAR_JPEG_QUALITY))
  return blob ? new File([blob], source.file.name, { type }) : null
}
