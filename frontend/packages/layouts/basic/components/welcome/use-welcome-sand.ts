/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

/**
 * 欢迎页的沙：问候语、标志与版本号都由沙粒聚成，风一吹就散
 * 画面是组件库背景包的 sand 效果；这里只管三件事——把文字与标志取成点云并上色、
 * 把形状摆到屏幕上指定的位置、让两张画面轮流上场（一张吹散时另一张正在聚拢）
 */
import type { BackgroundSurface, PointCloud } from '@xihan-ui/backgrounds'
import { boundsOf, createBackgroundSurface, drawableToCloud, loadImage, sandEffect, textToCloud } from '@xihan-ui/backgrounds'
import { nextTick, onScopeDispose, ref, shallowRef } from 'vue'

/** 聚成一个形状用多久（秒） */
export const SAND_GATHER_SECONDS = 2
/** 吹散一个形状用多久（秒） */
export const SAND_DISPERSE_SECONDS = 2.8

/** 形状摆在哪：墨迹的中心，与墨迹不能超出的宽高，都是相对画面左上角的 CSS 像素 */
export interface SandBox {
  centerX: number
  centerY: number
  maxWidth: number
  maxHeight: number
}

type Rgb = readonly [number, number, number]

const EMPTY: PointCloud = {
  count: 0,
  positions: new Float32Array(0),
  colors: new Float32Array(0),
}

/** 沙粒每档深浅：聚成的字有颗粒感，不是一块平涂 */
const SHADES = [0.8, 0.97, 1.14] as const

/** 标志取不出配色时，文字改取这几个语义令牌的颜色 */
const FALLBACK_TOKENS = [
  '--xh-fg-brand',
  '--xh-fg-info',
  '--xh-fg-danger',
  '--xh-fg-warning',
] as const

function clamp(value: number, min: number, max: number): number {
  return Math.min(Math.max(value, min), max)
}

/** 借一张 1×1 画布把任意 CSS 颜色解析成 RGB（令牌可能是 oklch） */
function parseColor(color: string): Rgb | null {
  const canvas = document.createElement('canvas')
  canvas.width = 1
  canvas.height = 1
  const context = canvas.getContext('2d', { willReadFrequently: true })
  if (!context || !color)
    return null
  context.fillStyle = color
  context.fillRect(0, 0, 1, 1)
  const [r = 0, g = 0, b = 0] = context.getImageData(0, 0, 1, 1).data
  return [r, g, b]
}

/** 把标志缩到一张小画布上读像素；跨源且没有 CORS 头的图读不了，返回 null */
function readPixels(image: HTMLImageElement, side: number): ImageData | null {
  const canvas = document.createElement('canvas')
  canvas.width = side
  canvas.height = side
  const context = canvas.getContext('2d', { willReadFrequently: true })
  if (!context)
    return null
  try {
    context.drawImage(image, 0, 0, side, side)
    return context.getImageData(0, 0, side, side)
  }
  catch {
    return null
  }
}

/** 色相分几档：同一条里先分档，再取最响的那一档 */
const HUE_BINS = 18

/** RGB 的色相落在第几档（0 ~ HUE_BINS - 1） */
function hueBin(r: number, g: number, b: number): number {
  const max = Math.max(r, g, b)
  const delta = max - Math.min(r, g, b)
  if (delta === 0)
    return 0
  let hue = max === r
    ? ((g - b) / delta) % 6
    : max === g
      ? (b - r) / delta + 2
      : (r - g) / delta + 4
  if (hue < 0)
    hue += 6
  return Math.min(Math.floor(hue / 6 * HUE_BINS), HUE_BINS - 1)
}

/**
 * 从标志里取一条横向的配色：把像素按色相分档、各档按鲜艳程度计分，取得分最高的几档，再按它们在图里从左到右的位置排
 * 直接平均会把几种色相和成灰的；分档之后取出来的是标志给人的那几种颜色，顺序也与标志一致
 * 文字的沙沿横向按它渐变，背景的光斑也取它，换了标志配色跟着换
 */
function paletteOf(pixels: ImageData, count: number): Rgb[] {
  const { data, width, height } = pixels
  // 每档：红绿蓝的加权和、权重、横向位置的加权和
  const bins = Array.from({ length: HUE_BINS }, () => [0, 0, 0, 0, 0])
  for (let y = 0; y < height; y++) {
    for (let x = 0; x < width; x++) {
      const at = (y * width + x) * 4
      const alpha = (data[at + 3] ?? 0) / 255
      const r = data[at] ?? 0
      const g = data[at + 1] ?? 0
      const b = data[at + 2] ?? 0
      // 近白的底不算标志的颜色
      if (alpha < 0.5 || (r > 236 && g > 236 && b > 236))
        continue
      const strongest = Math.max(r, g, b)
      const saturation = strongest === 0
        ? 0
        : (strongest - Math.min(r, g, b)) / strongest
      // 饱和度的平方乘明度：灰的、暗的像素几乎不计
      const weight = saturation * saturation * (strongest / 255)
      const bin = bins[hueBin(r, g, b)]!
      bin[0]! += r * weight
      bin[1]! += g * weight
      bin[2]! += b * weight
      bin[3]! += weight
      bin[4]! += x * weight
    }
  }
  const loudest = Math.max(...bins.map(bin => bin[3]!))
  return bins
    // 分量太小的档不算：一两个杂色像素不该进配色
    .filter(bin => bin[3]! > loudest * 0.08)
    .sort((a, b) => b[3]! - a[3]!)
    .slice(0, count)
    .sort((a, b) => a[4]! / a[3]! - b[4]! / b[3]!)
    .map(bin => [bin[0]! / bin[3]!, bin[1]! / bin[3]!, bin[2]! / bin[3]!] as const)
}

/** 标志是不是没有透明底（四角都不透明）：这种图得按明暗而不是按透明度取形 */
function isOpaque(pixels: ImageData): boolean {
  const { data, width, height } = pixels
  const corners = [
    0,
    width - 1,
    (height - 1) * width,
    height * width - 1,
  ]
  return corners.every(index => (data[index * 4 + 3] ?? 0) > 250)
}

/**
 * 建两张沙的画面并给出聚、散、取形的操作
 * @param isDark 当前是不是暗色：暗底上沙要提亮，亮底上要压深
 */
export function useWelcomeSand(isDark: () => boolean) {
  const hosts: [HTMLElement | null, HTMLElement | null] = [null, null]
  const surfaces: [BackgroundSurface | null, BackgroundSurface | null] = [null, null]
  /** 每张画面上此刻摆着的形状与取位置的函数：窗口尺寸变了再取一次、重摆 */
  const shown: [null | { cloud: PointCloud, box: () => SandBox }, null | { cloud: PointCloud, box: () => SandBox }] = [null, null]
  const logoImage = shallowRef<HTMLImageElement | null>(null)
  let logoPixels: ImageData | null = null
  /** 文字与光斑用的配色（RGB），取自标志；取不到用语义令牌 */
  const palette = shallowRef<Rgb[]>([])
  /** 标志能不能聚成沙（读不到像素的跨源图不能） */
  const hasLogo = ref(false)
  /** 沙画不画得出来：环境没有 WebGL2 时画面退成空白，调用方改放静止的标志 */
  const supported = ref(true)
  let observer: null | ResizeObserver = null

  function size(): { width: number, height: number } {
    const host = hosts[0] ?? hosts[1]
    return { width: host?.clientWidth ?? 0, height: host?.clientHeight ?? 0 }
  }

  /** 沙粒数量跟着画面面积走：大屏细密，小屏不至于叠成一片 */
  function grains(): number {
    const { width, height } = size()
    return Math.round(clamp(width * height / 70, 6000, 14000))
  }

  function fallbackPalette(): Rgb[] {
    const style = getComputedStyle(document.documentElement)
    return FALLBACK_TOKENS
      .map(token => parseColor(style.getPropertyValue(token).trim()))
      .filter((color): color is Rgb => color !== null)
  }

  /** 暗底上把颜色往白里提一点，亮底上往黑里压一点，沙才看得清 */
  function adapt(color: Rgb): Rgb {
    return isDark()
      ? [color[0] + (255 - color[0]) * 0.32, color[1] + (255 - color[1]) * 0.32, color[2] + (255 - color[2]) * 0.32]
      : [color[0] * 0.92, color[1] * 0.92, color[2] * 0.92]
  }

  /** 文字点云沿横向按配色渐变上色，每颗再随机取一档深浅 */
  function tint(cloud: PointCloud): PointCloud {
    const stops = (palette.value.length >= 2
      ? palette.value
      : fallbackPalette()).map(adapt)
    if (stops.length < 2 || cloud.count === 0)
      return cloud
    const { min, max } = boundsOf(cloud)
    const range = Math.max(max[0] - min[0], 1e-6)
    for (let i = 0; i < cloud.count; i++) {
      const at = clamp(((cloud.positions[i * 3] ?? 0) - min[0]) / range, 0, 0.9999) * (stops.length - 1)
      const from = stops[Math.floor(at)]!
      const to = stops[Math.floor(at) + 1]!
      const mix = at - Math.floor(at)
      const shade = SHADES[Math.floor(Math.random() * SHADES.length)]!
      for (let channel = 0; channel < 3; channel++)
        cloud.colors[i * 3 + channel] = Math.min(1, (from[channel]! + (to[channel]! - from[channel]!) * mix) / 255 * shade)
    }
    return cloud
  }

  /**
   * 把一段文字取成沙（问候语、版本号）
   * @param text 文字
   * @param weight 字重
   */
  function word(text: string, weight = 700): PointCloud {
    return tint(textToCloud(text, {
      count: grains(),
      fontSize: 220,
      fontWeight: weight,
      fontFamily: getComputedStyle(document.body).fontFamily,
      padding: 4,
    }))
  }

  /** 把标志取成沙：每颗沙取它落点上那个像素的颜色；标志读不了时返回空点云 */
  function logo(): PointCloud {
    const image = logoImage.value
    if (!image || !logoPixels)
      return EMPTY
    return drawableToCloud(image, isOpaque(logoPixels)
      ? {
          count: grains(),
          resolution: 320,
          channel: 'luminance',
          invert: true,
          threshold: 0.1,
        }
      : {
          count: grains(),
          resolution: 320,
          threshold: 0.5,
        })
  }

  /** 按形状的墨迹范围算出缩放与落点，让它正好落在指定的位置与大小上 */
  function place(layer: 0 | 1): void {
    const surface = surfaces[layer]
    const current = shown[layer]
    const { width, height } = size()
    if (!surface || !current || width === 0 || height === 0)
      return
    const { min, max } = boundsOf(current.cloud)
    const box = current.box()
    // 一个点云单位合多少像素：墨迹等比缩到正好装进给定的宽高；画面高的一半对应缩放 1
    const unit = Math.min(box.maxWidth / Math.max(max[0] - min[0], 1e-6), box.maxHeight / Math.max(max[1] - min[1], 1e-6))
    const inkCenterX = (min[0] + max[0]) / 2
    const inkCenterY = (min[1] + max[1]) / 2
    surface.setParams({
      zoom: unit / (height / 2),
      originX: (box.centerX - inkCenterX * unit) / width,
      // 点云的 y 轴朝上、原点在画面左下角
      originY: 1 - (box.centerY + inkCenterY * unit) / height,
      reach: clamp(width * 0.36, 220, 560) / unit,
    })
  }

  function mount(layer: 0 | 1, element: HTMLElement | null): void {
    if (hosts[layer] === element)
      return
    surfaces[layer]?.destroy()
    surfaces[layer] = null
    hosts[layer] = element
    if (!element)
      return
    // 模板 ref 回调时整层还没进文档：等这一轮渲染完再建画面，它才读得到所在容器的动效偏好
    void nextTick(() => {
      if (hosts[layer] === element && !surfaces[layer])
        create(layer, element)
    })
  }

  function create(layer: 0 | 1, element: HTMLElement): void {
    surfaces[layer] = createBackgroundSurface(element, {
      effect: sandEffect,
      // 欢迎层自己不接指针：沙只是画面，不抢按钮的交互
      pointer: false,
      params: {
        sweep: 0.22,
        scatter: 0.16,
        pointSize: 1.9,
        turbulence: 0.7,
        lift: 0.3,
        shimmer: 0.1,
      },
    })
    supported.value = surfaces[layer].backend === 'webgl2'
    if (!observer && typeof ResizeObserver === 'function') {
      observer = new ResizeObserver(() => {
        place(0)
        place(1)
      })
    }
    observer?.observe(element)
  }

  /**
   * 读入标志：之后才能把它聚成沙、才有取自标志的配色
   * @param source 标志地址
   */
  async function load(source: string): Promise<void> {
    try {
      const image = await loadImage(source)
      logoPixels = readPixels(image, 64)
      logoImage.value = image
      palette.value = logoPixels
        ? paletteOf(logoPixels, 4)
        : []
      hasLogo.value = logoPixels !== null && logo().count > 0
    }
    catch {
      // 标志加载不了：欢迎页照常，主画面只聚版本号，配色退回语义令牌
      logoImage.value = null
      logoPixels = null
      palette.value = []
      hasLogo.value = false
    }
  }

  /**
   * 让一张画面聚成给定的形状
   * @param layer 第几张画面
   * @param cloud 形状
   * @param box 取摆放位置的函数（窗口尺寸变了会再取）
   */
  function gather(layer: 0 | 1, cloud: PointCloud, box: () => SandBox): void {
    if (cloud.count === 0)
      return
    shown[layer] = { cloud, box }
    place(layer)
    surfaces[layer]?.setCloud(cloud, { duration: SAND_GATHER_SECONDS })
  }

  /** 把一张画面上的形状吹散 */
  function disperse(layer: 0 | 1): void {
    if (!shown[layer])
      return
    shown[layer] = null
    surfaces[layer]?.setCloud(EMPTY, { duration: SAND_DISPERSE_SECONDS })
  }

  /** 两张画面都吹散 */
  function scatter(): void {
    disperse(0)
    disperse(1)
  }

  onScopeDispose(() => {
    observer?.disconnect()
    surfaces[0]?.destroy()
    surfaces[1]?.destroy()
  })

  return {
    /** 两张画面的挂载点，给模板 ref 用 */
    attach: [
      (element: unknown) => mount(0, element instanceof HTMLElement
        ? element
        : null),
      (element: unknown) => mount(1, element instanceof HTMLElement
        ? element
        : null),
    ] as const,
    palette,
    hasLogo,
    supported,
    load,
    word,
    logo,
    gather,
    disperse,
    scatter,
  }
}
