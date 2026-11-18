/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import type { ImageCropperRect } from '@xihan-ui/headless'
import { cropToCanvas } from '@xihan-ui/headless'

/** 待裁剪的图片：选中的文件、解码好的图片与它的对象地址 */
export interface ImageCropSource {
  file: File
  image: HTMLImageElement
  url: string
}

/** 出图选项 */
export interface ImageCropExportOptions {
  /** 出图宽度的上限（像素）：裁切框比它窄时按原尺寸，不放大 */
  maxWidth: number
  /** 一律出 JPEG（照片类图片控制体积）；缺省时原图是 JPEG 才出 JPEG，其余出 PNG 保住透明 */
  opaque?: boolean
}

const JPEG_QUALITY = 0.92

/** 解码选中的图片。解不出来（文件损坏或不是图片）即拒绝，对象地址随之回收 */
export async function loadImageCropSource(file: File): Promise<ImageCropSource> {
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
export function releaseImageCropSource(source: ImageCropSource): void {
  URL.revokeObjectURL(source.url)
}

/** 初始裁切框：锁了比例取居中的最大框，不锁取整张图 */
export function initialCropRect(image: Pick<HTMLImageElement, 'naturalWidth' | 'naturalHeight'>, aspectRatio?: number): ImageCropperRect {
  const { naturalWidth: width, naturalHeight: height } = image
  if (!aspectRatio)
    return { x: 0, y: 0, width, height }
  const cropWidth = Math.min(width, height * aspectRatio)
  const cropHeight = cropWidth / aspectRatio
  return {
    x: (width - cropWidth) / 2,
    y: (height - cropHeight) / 2,
    width: cropWidth,
    height: cropHeight,
  }
}

/**
 * 按裁切框出一张矩形图
 * 裁切器里的圆形只是取景遮罩：头像在别处也按方形显示，存下来的是整块矩形
 */
export async function exportCroppedImage(source: ImageCropSource, rect: ImageCropperRect, options: ImageCropExportOptions): Promise<File | null> {
  const type = options.opaque || source.file.type === 'image/jpeg'
    ? 'image/jpeg'
    : 'image/png'
  const canvas = cropToCanvas(source.image, rect, {
    width: Math.min(options.maxWidth, Math.round(rect.width)),
    // JPEG 没有透明通道，透明像素会被编码成黑块，先铺白底
    background: type === 'image/jpeg'
      ? '#fff'
      : undefined,
  })
  if (!canvas)
    return null
  const blob = await new Promise<Blob | null>(resolve => canvas.toBlob(resolve, type, JPEG_QUALITY))
  return blob
    ? new File([blob], source.file.name, { type })
    : null
}
