/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

// ==================== 数学曲线加载器 ====================
// 粒子拖尾沿参数曲线运动，叠加整体旋转与「呼吸」细节缩放（detailScale）

/** 曲线上的一点（视图坐标 0-100） */
export interface CurvePoint {
  x: number
  y: number
}

export interface LoaderConfig {
  /** 稳定标识（loadingName 取值） */
  id: string
  /** 展示名 */
  name: string
  /** 是否整体旋转 */
  rotate: boolean
  /** 粒子数 */
  particleCount: number
  /** 尾迹长度（0-1，越大尾巴越长） */
  trailSpan: number
  /** 主循环时长 ms */
  durationMs: number
  /** 旋转时长 ms */
  rotationDurationMs: number
  /** 呼吸时长 ms */
  pulseDurationMs: number
  /** 背景轨迹粗细 */
  strokeWidth: number
  /** 参数点函数：progress(0-1) × detailScale → 视图坐标(0-100) */
  point: (progress: number, detailScale: number) => CurvePoint
}

/** 一条曲线的写法：共用字段加曲线自己的参数，point 读到的参数按 params 推断类型 */
type CurveDefinition<P extends Record<string, number>> = Omit<LoaderConfig, 'point'> & {
  params: P
  point: (progress: number, detailScale: number, params: P) => CurvePoint
}

function defineCurve<P extends Record<string, number>>({ params, point, ...config }: CurveDefinition<P>): LoaderConfig {
  return {
    ...config,
    point: (progress, detailScale) => point(progress, detailScale, params),
  }
}

export const LOADER_CURVES: LoaderConfig[] = [
  defineCurve({
    id: 'original-thinking',
    name: 'Original Thinking',
    params: {
      baseRadius: 7,
      detailAmplitude: 3,
      petalCount: 7,
      curveScale: 3.9,
    },
    rotate: true,
    particleCount: 84,
    trailSpan: 0.46,
    durationMs: 4600,
    rotationDurationMs: 28000,
    pulseDurationMs: 4200,
    strokeWidth: 5.5,
    point(progress, detailScale, params) {
      const t = progress * Math.PI * 2
      const petals = Math.round(params.petalCount)
      const x = params.baseRadius * Math.cos(t) - params.detailAmplitude * detailScale * Math.cos(petals * t)
      const y = params.baseRadius * Math.sin(t) - params.detailAmplitude * detailScale * Math.sin(petals * t)
      return { x: 50 + x * params.curveScale, y: 50 + y * params.curveScale }
    },
  }),
  defineCurve({
    id: 'rose-curve',
    name: 'Rose Curve',
    params: {
      roseA: 9.2,
      roseABoost: 0.6,
      roseBreathBase: 0.72,
      roseBreathBoost: 0.28,
      roseK: 5,
      roseScale: 3.25,
    },
    rotate: true,
    particleCount: 100,
    trailSpan: 0.40,
    durationMs: 5400,
    rotationDurationMs: 28000,
    pulseDurationMs: 4600,
    strokeWidth: 4.5,
    point(progress, detailScale, params) {
      const t = progress * Math.PI * 2
      const a = params.roseA + detailScale * params.roseABoost
      const k = Math.round(params.roseK)
      const r = a * (params.roseBreathBase + detailScale * params.roseBreathBoost) * Math.cos(k * t)
      return { x: 50 + Math.cos(t) * r * params.roseScale, y: 50 + Math.sin(t) * r * params.roseScale }
    },
  }),
  defineCurve({
    id: 'lissajous-drift',
    name: 'Lissajous Drift',
    params: {
      lissajousAmp: 24,
      lissajousAmpBoost: 6,
      lissajousAX: 3,
      lissajousBY: 4,
      lissajousPhase: 1.57,
      lissajousYScale: 0.92,
    },
    rotate: false,
    particleCount: 88,
    trailSpan: 0.42,
    durationMs: 6000,
    rotationDurationMs: 36000,
    pulseDurationMs: 5400,
    strokeWidth: 4.7,
    point(progress, detailScale, params) {
      const t = progress * Math.PI * 2
      const amp = params.lissajousAmp + detailScale * params.lissajousAmpBoost
      return {
        x: 50 + Math.sin(Math.round(params.lissajousAX) * t + params.lissajousPhase) * amp,
        y: 50 + Math.sin(Math.round(params.lissajousBY) * t) * (amp * params.lissajousYScale),
      }
    },
  }),
  defineCurve({
    id: 'lemniscate-bloom',
    name: 'Lemniscate Bloom',
    params: { lemniscateA: 20, lemniscateBoost: 7 },
    rotate: false,
    particleCount: 92,
    trailSpan: 0.48,
    durationMs: 5600,
    rotationDurationMs: 34000,
    pulseDurationMs: 5000,
    strokeWidth: 4.8,
    point(progress, detailScale, params) {
      const t = progress * Math.PI * 2
      const scale = params.lemniscateA + detailScale * params.lemniscateBoost
      const denom = 1 + Math.sin(t) ** 2
      return { x: 50 + (scale * Math.cos(t)) / denom, y: 50 + (scale * Math.sin(t) * Math.cos(t)) / denom }
    },
  }),
  defineCurve({
    id: 'hypotrochoid-loop',
    name: 'Hypotrochoid Loop',
    params: {
      spiroR: 8.2,
      spiror: 2.7,
      spirorBoost: 0.45,
      spirod: 4.8,
      spirodBoost: 1.2,
      spiroScale: 3.05,
    },
    rotate: false,
    particleCount: 106,
    trailSpan: 0.54,
    durationMs: 7600,
    rotationDurationMs: 42000,
    pulseDurationMs: 6200,
    strokeWidth: 4.6,
    point(progress, detailScale, params) {
      const t = progress * Math.PI * 2
      const r = params.spiror + detailScale * params.spirorBoost
      const d = params.spirod + detailScale * params.spirodBoost
      const x = (params.spiroR - r) * Math.cos(t) + d * Math.cos(((params.spiroR - r) / r) * t)
      const y = (params.spiroR - r) * Math.sin(t) - d * Math.sin(((params.spiroR - r) / r) * t)
      return { x: 50 + x * params.spiroScale, y: 50 + y * params.spiroScale }
    },
  }),
  defineCurve({
    id: 'fourier-flow',
    name: 'Fourier Flow',
    params: {
      fourierX1: 17,
      fourierX3: 7.5,
      fourierX5: 3.2,
      fourierY1: 15,
      fourierY2: 8.2,
      fourierY4: 4.2,
      fourierMixBase: 1,
      fourierMixPulse: 0.16,
    },
    rotate: false,
    particleCount: 118,
    trailSpan: 0.39,
    durationMs: 8400,
    rotationDurationMs: 44000,
    pulseDurationMs: 6800,
    strokeWidth: 4.2,
    point(progress, detailScale, params) {
      const t = progress * Math.PI * 2
      const mix = params.fourierMixBase + detailScale * params.fourierMixPulse
      const x = params.fourierX1 * Math.cos(t) + params.fourierX3 * Math.cos(3 * t + 0.6 * mix) + params.fourierX5 * Math.sin(5 * t - 0.4)
      const y = params.fourierY1 * Math.sin(t) + params.fourierY2 * Math.sin(2 * t + 0.25) - params.fourierY4 * Math.cos(4 * t - 0.5 * mix)
      return { x: 50 + x, y: 50 + y }
    },
  }),
]

/** 取加载器配置；未命中回退到第一个 */
export function getLoaderConfig(id?: string): LoaderConfig {
  return LOADER_CURVES.find(curve => curve.id === id) ?? LOADER_CURVES[0]!
}

function normalizeProgress(progress: number): number {
  return ((progress % 1) + 1) % 1
}

/** 呼吸细节缩放（0.62 ~ 1.0，柔和呼吸） */
export function getDetailScale(time: number, config: LoaderConfig, phaseOffset: number): number {
  const pulseProgress = ((time + phaseOffset * config.pulseDurationMs) % config.pulseDurationMs) / config.pulseDurationMs
  const pulseAngle = pulseProgress * Math.PI * 2
  return 0.62 + ((Math.sin(pulseAngle + 0.55) + 1) / 2) * 0.38
}

/** 整体旋转角度（度），rotate=false 时为 0 */
export function getRotation(time: number, config: LoaderConfig, phaseOffset: number): number {
  if (!config.rotate) {
    return 0
  }
  return -(((time + phaseOffset * config.rotationDurationMs) % config.rotationDurationMs) / config.rotationDurationMs) * 360
}

/** 背景轨迹 path d */
export function buildPath(config: LoaderConfig, detailScale: number, steps: number): string {
  let d = ''
  for (let index = 0; index <= steps; index++) {
    const point = config.point(index / steps, detailScale)
    d += `${index === 0
      ? 'M'
      : 'L'} ${point.x.toFixed(2)} ${point.y.toFixed(2)} `
  }
  return d
}

/** 第 index 个拖尾粒子：丝滑彗星拖尾（柔和淡出 + 半径衰减） */
export function getParticle(config: LoaderConfig, index: number, particleCount: number, progress: number, detailScale: number) {
  const tailOffset = index / (particleCount - 1)
  const point = config.point(normalizeProgress(progress - tailOffset * config.trailSpan), detailScale)
  const fade = (1 - tailOffset) ** 0.5
  return {
    x: point.x,
    y: point.y,
    radius: 1.0 + fade * 2.8,
    opacity: 0.05 + fade * 0.95,
  }
}
