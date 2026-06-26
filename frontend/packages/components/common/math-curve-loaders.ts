// ==================== 数学曲线加载器 ====================
// 粒子拖尾沿参数曲线运动，叠加整体旋转与「呼吸」细节缩放（detailScale）。

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
  /** 参数点函数：progress(0-1) × detailScale × config → 视图坐标(0-100) */
  point: (progress: number, detailScale: number, config: any) => { x: number, y: number }
  [key: string]: unknown
}

export const LOADER_CURVES: LoaderConfig[] = [
  {
    id: 'original-thinking',
    name: 'Original Thinking',
    baseRadius: 7,
    detailAmplitude: 3,
    petalCount: 7,
    curveScale: 3.9,
    rotate: true,
    particleCount: 84,
    trailSpan: 0.46,
    durationMs: 4600,
    rotationDurationMs: 28000,
    pulseDurationMs: 4200,
    strokeWidth: 5.5,
    point(progress, detailScale, config) {
      const t = progress * Math.PI * 2
      const petals = Math.round(config.petalCount)
      const x = config.baseRadius * Math.cos(t) - config.detailAmplitude * detailScale * Math.cos(petals * t)
      const y = config.baseRadius * Math.sin(t) - config.detailAmplitude * detailScale * Math.sin(petals * t)
      return { x: 50 + x * config.curveScale, y: 50 + y * config.curveScale }
    },
  },
  {
    id: 'thinking-five',
    name: 'Thinking Five',
    baseRadius: 7,
    detailAmplitude: 3,
    petalCount: 5,
    curveScale: 3.9,
    rotate: true,
    particleCount: 82,
    trailSpan: 0.46,
    durationMs: 4600,
    rotationDurationMs: 28000,
    pulseDurationMs: 4200,
    strokeWidth: 5.5,
    point(progress, detailScale, config) {
      const t = progress * Math.PI * 2
      const petals = Math.round(config.petalCount)
      const x = config.baseRadius * Math.cos(t) - config.detailAmplitude * detailScale * Math.cos(petals * t)
      const y = config.baseRadius * Math.sin(t) - config.detailAmplitude * detailScale * Math.sin(petals * t)
      return { x: 50 + x * config.curveScale, y: 50 + y * config.curveScale }
    },
  },
  {
    id: 'thinking-nine',
    name: 'Thinking Nine',
    baseRadius: 7,
    detailAmplitude: 3,
    petalCount: 9,
    curveScale: 3.9,
    rotate: true,
    particleCount: 88,
    trailSpan: 0.47,
    durationMs: 4700,
    rotationDurationMs: 30000,
    pulseDurationMs: 4200,
    strokeWidth: 5.5,
    point(progress, detailScale, config) {
      const t = progress * Math.PI * 2
      const petals = Math.round(config.petalCount)
      const x = config.baseRadius * Math.cos(t) - config.detailAmplitude * detailScale * Math.cos(petals * t)
      const y = config.baseRadius * Math.sin(t) - config.detailAmplitude * detailScale * Math.sin(petals * t)
      return { x: 50 + x * config.curveScale, y: 50 + y * config.curveScale }
    },
  },
  {
    id: 'rose-orbit',
    name: 'Rose Orbit',
    orbitRadius: 7,
    detailAmplitude: 2.7,
    petalCount: 7,
    curveScale: 3.9,
    rotate: true,
    particleCount: 94,
    trailSpan: 0.50,
    durationMs: 5200,
    rotationDurationMs: 28000,
    pulseDurationMs: 4600,
    strokeWidth: 5.2,
    point(progress, detailScale, config) {
      const t = progress * Math.PI * 2
      const k = Math.round(config.petalCount)
      const r = config.orbitRadius - config.detailAmplitude * detailScale * Math.cos(k * t)
      return { x: 50 + Math.cos(t) * r * config.curveScale, y: 50 + Math.sin(t) * r * config.curveScale }
    },
  },
  {
    id: 'rose-curve',
    name: 'Rose Curve',
    roseA: 9.2,
    roseABoost: 0.6,
    roseBreathBase: 0.72,
    roseBreathBoost: 0.28,
    roseK: 5,
    roseScale: 3.25,
    rotate: true,
    particleCount: 100,
    trailSpan: 0.40,
    durationMs: 5400,
    rotationDurationMs: 28000,
    pulseDurationMs: 4600,
    strokeWidth: 4.5,
    point(progress, detailScale, config) {
      const t = progress * Math.PI * 2
      const a = config.roseA + detailScale * config.roseABoost
      const k = Math.round(config.roseK)
      const r = a * (config.roseBreathBase + detailScale * config.roseBreathBoost) * Math.cos(k * t)
      return { x: 50 + Math.cos(t) * r * config.roseScale, y: 50 + Math.sin(t) * r * config.roseScale }
    },
  },
  {
    id: 'rose-two',
    name: 'Rose Two',
    roseA: 9.2,
    roseABoost: 0.6,
    roseBreathBase: 0.72,
    roseBreathBoost: 0.28,
    roseScale: 3.25,
    rotate: true,
    particleCount: 96,
    trailSpan: 0.38,
    durationMs: 5200,
    rotationDurationMs: 28000,
    pulseDurationMs: 4300,
    strokeWidth: 4.6,
    point(progress, detailScale, config) {
      const t = progress * Math.PI * 2
      const a = config.roseA + detailScale * config.roseABoost
      const r = a * (config.roseBreathBase + detailScale * config.roseBreathBoost) * Math.cos(2 * t)
      return { x: 50 + Math.cos(t) * r * config.roseScale, y: 50 + Math.sin(t) * r * config.roseScale }
    },
  },
  {
    id: 'rose-three',
    name: 'Rose Three',
    roseA: 9.2,
    roseABoost: 0.6,
    roseBreathBase: 0.72,
    roseBreathBoost: 0.28,
    roseScale: 3.25,
    rotate: true,
    particleCount: 98,
    trailSpan: 0.39,
    durationMs: 5300,
    rotationDurationMs: 28000,
    pulseDurationMs: 4400,
    strokeWidth: 4.6,
    point(progress, detailScale, config) {
      const t = progress * Math.PI * 2
      const a = config.roseA + detailScale * config.roseABoost
      const r = a * (config.roseBreathBase + detailScale * config.roseBreathBoost) * Math.cos(3 * t)
      return { x: 50 + Math.cos(t) * r * config.roseScale, y: 50 + Math.sin(t) * r * config.roseScale }
    },
  },
  {
    id: 'rose-four',
    name: 'Rose Four',
    roseA: 9.2,
    roseABoost: 0.6,
    roseBreathBase: 0.72,
    roseBreathBoost: 0.28,
    roseScale: 3.25,
    rotate: true,
    particleCount: 100,
    trailSpan: 0.40,
    durationMs: 5400,
    rotationDurationMs: 28000,
    pulseDurationMs: 4500,
    strokeWidth: 4.6,
    point(progress, detailScale, config) {
      const t = progress * Math.PI * 2
      const a = config.roseA + detailScale * config.roseABoost
      const r = a * (config.roseBreathBase + detailScale * config.roseBreathBoost) * Math.cos(4 * t)
      return { x: 50 + Math.cos(t) * r * config.roseScale, y: 50 + Math.sin(t) * r * config.roseScale }
    },
  },
  {
    id: 'lissajous-drift',
    name: 'Lissajous Drift',
    lissajousAmp: 24,
    lissajousAmpBoost: 6,
    lissajousAX: 3,
    lissajousBY: 4,
    lissajousPhase: 1.57,
    lissajousYScale: 0.92,
    rotate: false,
    particleCount: 88,
    trailSpan: 0.42,
    durationMs: 6000,
    rotationDurationMs: 36000,
    pulseDurationMs: 5400,
    strokeWidth: 4.7,
    point(progress, detailScale, config) {
      const t = progress * Math.PI * 2
      const amp = config.lissajousAmp + detailScale * config.lissajousAmpBoost
      return {
        x: 50 + Math.sin(Math.round(config.lissajousAX) * t + config.lissajousPhase) * amp,
        y: 50 + Math.sin(Math.round(config.lissajousBY) * t) * (amp * config.lissajousYScale),
      }
    },
  },
  {
    id: 'lemniscate-bloom',
    name: 'Lemniscate Bloom',
    lemniscateA: 20,
    lemniscateBoost: 7,
    rotate: false,
    particleCount: 92,
    trailSpan: 0.48,
    durationMs: 5600,
    rotationDurationMs: 34000,
    pulseDurationMs: 5000,
    strokeWidth: 4.8,
    point(progress, detailScale, config) {
      const t = progress * Math.PI * 2
      const scale = config.lemniscateA + detailScale * config.lemniscateBoost
      const denom = 1 + Math.sin(t) ** 2
      return { x: 50 + (scale * Math.cos(t)) / denom, y: 50 + (scale * Math.sin(t) * Math.cos(t)) / denom }
    },
  },
  {
    id: 'hypotrochoid-loop',
    name: 'Hypotrochoid Loop',
    spiroR: 8.2,
    spiror: 2.7,
    spirorBoost: 0.45,
    spirod: 4.8,
    spirodBoost: 1.2,
    spiroScale: 3.05,
    rotate: false,
    particleCount: 106,
    trailSpan: 0.54,
    durationMs: 7600,
    rotationDurationMs: 42000,
    pulseDurationMs: 6200,
    strokeWidth: 4.6,
    point(progress, detailScale, config) {
      const t = progress * Math.PI * 2
      const r = config.spiror + detailScale * config.spirorBoost
      const d = config.spirod + detailScale * config.spirodBoost
      const x = (config.spiroR - r) * Math.cos(t) + d * Math.cos(((config.spiroR - r) / r) * t)
      const y = (config.spiroR - r) * Math.sin(t) - d * Math.sin(((config.spiroR - r) / r) * t)
      return { x: 50 + x * config.spiroScale, y: 50 + y * config.spiroScale }
    },
  },
  {
    id: 'three-petal-spiral',
    name: 'Three-Petal Spiral',
    spiralR: 3,
    spiralr: 1,
    spirald: 3,
    spiralScale: 2.2,
    spiralBreath: 0.45,
    rotate: true,
    particleCount: 106,
    trailSpan: 0.42,
    durationMs: 4600,
    rotationDurationMs: 28000,
    pulseDurationMs: 4200,
    strokeWidth: 4.4,
    point(progress, detailScale, config) {
      const t = progress * Math.PI * 2
      const d = config.spirald + detailScale * 0.25
      const baseX = (config.spiralR - config.spiralr) * Math.cos(t) + d * Math.cos(((config.spiralR - config.spiralr) / config.spiralr) * t)
      const baseY = (config.spiralR - config.spiralr) * Math.sin(t) - d * Math.sin(((config.spiralR - config.spiralr) / config.spiralr) * t)
      const scale = config.spiralScale + detailScale * config.spiralBreath
      return { x: 50 + baseX * scale, y: 50 + baseY * scale }
    },
  },
  {
    id: 'four-petal-spiral',
    name: 'Four-Petal Spiral',
    spiralR: 4,
    spiralr: 1,
    spirald: 3,
    spiralScale: 2.2,
    spiralBreath: 0.45,
    rotate: true,
    particleCount: 108,
    trailSpan: 0.42,
    durationMs: 4600,
    rotationDurationMs: 28000,
    pulseDurationMs: 4200,
    strokeWidth: 4.4,
    point(progress, detailScale, config) {
      const t = progress * Math.PI * 2
      const d = config.spirald + detailScale * 0.25
      const baseX = (config.spiralR - config.spiralr) * Math.cos(t) + d * Math.cos(((config.spiralR - config.spiralr) / config.spiralr) * t)
      const baseY = (config.spiralR - config.spiralr) * Math.sin(t) - d * Math.sin(((config.spiralR - config.spiralr) / config.spiralr) * t)
      const scale = config.spiralScale + detailScale * config.spiralBreath
      return { x: 50 + baseX * scale, y: 50 + baseY * scale }
    },
  },
  {
    id: 'five-petal-spiral',
    name: 'Five-Petal Spiral',
    spiralR: 5,
    spiralr: 1,
    spirald: 3,
    spiralScale: 2.2,
    spiralBreath: 0.45,
    rotate: true,
    particleCount: 110,
    trailSpan: 0.42,
    durationMs: 4600,
    rotationDurationMs: 28000,
    pulseDurationMs: 4200,
    strokeWidth: 4.4,
    point(progress, detailScale, config) {
      const t = progress * Math.PI * 2
      const d = config.spirald + detailScale * 0.25
      const baseX = (config.spiralR - config.spiralr) * Math.cos(t) + d * Math.cos(((config.spiralR - config.spiralr) / config.spiralr) * t)
      const baseY = (config.spiralR - config.spiralr) * Math.sin(t) - d * Math.sin(((config.spiralR - config.spiralr) / config.spiralr) * t)
      const scale = config.spiralScale + detailScale * config.spiralBreath
      return { x: 50 + baseX * scale, y: 50 + baseY * scale }
    },
  },
  {
    id: 'six-petal-spiral',
    name: 'Six-Petal Spiral',
    spiralR: 6,
    spiralr: 1,
    spirald: 3,
    spiralScale: 2.2,
    spiralBreath: 0.45,
    rotate: true,
    particleCount: 112,
    trailSpan: 0.42,
    durationMs: 4600,
    rotationDurationMs: 28000,
    pulseDurationMs: 4200,
    strokeWidth: 4.4,
    point(progress, detailScale, config) {
      const t = progress * Math.PI * 2
      const d = config.spirald + detailScale * 0.25
      const baseX = (config.spiralR - config.spiralr) * Math.cos(t) + d * Math.cos(((config.spiralR - config.spiralr) / config.spiralr) * t)
      const baseY = (config.spiralR - config.spiralr) * Math.sin(t) - d * Math.sin(((config.spiralR - config.spiralr) / config.spiralr) * t)
      const scale = config.spiralScale + detailScale * config.spiralBreath
      return { x: 50 + baseX * scale, y: 50 + baseY * scale }
    },
  },
  {
    id: 'butterfly-phase',
    name: 'Butterfly Phase',
    butterflyTurns: 12,
    butterflyScale: 4.6,
    butterflyPulse: 0.45,
    butterflyCosWeight: 2,
    butterflyPower: 5,
    rotate: false,
    particleCount: 114,
    trailSpan: 0.38,
    durationMs: 9000,
    rotationDurationMs: 50000,
    pulseDurationMs: 7000,
    strokeWidth: 4.4,
    point(progress, detailScale, config) {
      const t = progress * Math.PI * config.butterflyTurns
      const s = Math.exp(Math.cos(t)) - config.butterflyCosWeight * Math.cos(4 * t) - Math.sin(t / 12) ** Math.round(config.butterflyPower)
      const scale = config.butterflyScale + detailScale * config.butterflyPulse
      return { x: 50 + Math.sin(t) * s * scale, y: 50 + Math.cos(t) * s * scale }
    },
  },
  {
    id: 'cardioid-glow',
    name: 'Cardioid Glow',
    cardioidA: 8.4,
    cardioidPulse: 0.8,
    cardioidScale: 2.15,
    rotate: false,
    particleCount: 94,
    trailSpan: 0.44,
    durationMs: 6200,
    rotationDurationMs: 36000,
    pulseDurationMs: 5200,
    strokeWidth: 4.9,
    point(progress, detailScale, config) {
      const t = progress * Math.PI * 2
      const a = config.cardioidA + detailScale * config.cardioidPulse
      const r = a * (1 - Math.cos(t))
      return { x: 50 + Math.cos(t) * r * config.cardioidScale, y: 50 + Math.sin(t) * r * config.cardioidScale }
    },
  },
  {
    id: 'cardioid-heart',
    name: 'Cardioid Heart',
    cardioidA: 8.8,
    cardioidPulse: 0.8,
    cardioidScale: 2.15,
    rotate: false,
    particleCount: 96,
    trailSpan: 0.44,
    durationMs: 6200,
    rotationDurationMs: 36000,
    pulseDurationMs: 5200,
    strokeWidth: 4.9,
    point(progress, detailScale, config) {
      const t = progress * Math.PI * 2
      const a = config.cardioidA + detailScale * config.cardioidPulse
      const r = a * (1 + Math.cos(t))
      const baseX = Math.cos(t) * r
      const baseY = Math.sin(t) * r
      return { x: 50 - baseY * config.cardioidScale, y: 50 - baseX * config.cardioidScale }
    },
  },
  {
    id: 'heart-wave',
    name: 'Heart Wave',
    heartWaveB: 6.4,
    heartWaveRoot: 3.3,
    heartWaveAmp: 0.9,
    heartWaveScaleX: 23.2,
    heartWaveScaleY: 24.5,
    rotate: false,
    particleCount: 132,
    trailSpan: 0.24,
    durationMs: 8400,
    rotationDurationMs: 22000,
    pulseDurationMs: 5600,
    strokeWidth: 3.9,
    point(progress, detailScale, config) {
      const xLimit = Math.sqrt(config.heartWaveRoot)
      const x = -xLimit + progress * xLimit * 2
      const safeRoot = Math.max(0, config.heartWaveRoot - x * x)
      const b = config.heartWaveB
      const wave = config.heartWaveAmp * Math.sqrt(safeRoot) * Math.sin(b * Math.PI * x)
      const curve = Math.pow(Math.abs(x), 2 / 3)
      const y = curve + wave
      const scaleX = config.heartWaveScaleX
      const scaleY = config.heartWaveScaleY + detailScale * 1.5
      return { x: 50 + x * scaleX, y: 18 + (1.75 - y) * scaleY }
    },
  },
  {
    id: 'spiral-search',
    name: 'Spiral Search',
    searchTurns: 4,
    searchBaseRadius: 8,
    searchRadiusAmp: 8.5,
    searchPulse: 2.4,
    searchScale: 1,
    rotate: false,
    particleCount: 112,
    trailSpan: 0.36,
    durationMs: 7800,
    rotationDurationMs: 44000,
    pulseDurationMs: 6800,
    strokeWidth: 4.3,
    point(progress, detailScale, config) {
      const t = progress * Math.PI * 2
      const angle = t * config.searchTurns
      const radius = config.searchBaseRadius + (1 - Math.cos(t)) * (config.searchRadiusAmp + detailScale * config.searchPulse)
      return { x: 50 + Math.cos(angle) * radius * config.searchScale, y: 50 + Math.sin(angle) * radius * config.searchScale }
    },
  },
  {
    id: 'fourier-flow',
    name: 'Fourier Flow',
    fourierX1: 17,
    fourierX3: 7.5,
    fourierX5: 3.2,
    fourierY1: 15,
    fourierY2: 8.2,
    fourierY4: 4.2,
    fourierMixBase: 1,
    fourierMixPulse: 0.16,
    rotate: false,
    particleCount: 118,
    trailSpan: 0.39,
    durationMs: 8400,
    rotationDurationMs: 44000,
    pulseDurationMs: 6800,
    strokeWidth: 4.2,
    point(progress, detailScale, config) {
      const t = progress * Math.PI * 2
      const mix = config.fourierMixBase + detailScale * config.fourierMixPulse
      const x = config.fourierX1 * Math.cos(t) + config.fourierX3 * Math.cos(3 * t + 0.6 * mix) + config.fourierX5 * Math.sin(5 * t - 0.4)
      const y = config.fourierY1 * Math.sin(t) + config.fourierY2 * Math.sin(2 * t + 0.25) - config.fourierY4 * Math.cos(4 * t - 0.5 * mix)
      return { x: 50 + x, y: 50 + y }
    },
  },
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
    const point = config.point(index / steps, detailScale, config)
    d += `${index === 0 ? 'M' : 'L'} ${point.x.toFixed(2)} ${point.y.toFixed(2)} `
  }
  return d
}

/** 第 index 个拖尾粒子：丝滑彗星拖尾（柔和淡出 + 半径衰减） */
export function getParticle(config: LoaderConfig, index: number, particleCount: number, progress: number, detailScale: number) {
  const tailOffset = index / (particleCount - 1)
  const point = config.point(normalizeProgress(progress - tailOffset * config.trailSpan), detailScale, config)
  const fade = (1 - tailOffset) ** 0.5
  return {
    x: point.x,
    y: point.y,
    radius: 1.0 + fade * 2.8,
    opacity: 0.05 + fade * 0.95,
  }
}
