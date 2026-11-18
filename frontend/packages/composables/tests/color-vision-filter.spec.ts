/**
 * 色弱模式的红绿色觉校正滤镜
 *
 * 校正矩阵不能动明暗（白、黑、灰原样），只把绿色弱看不出的红绿差异换到看得出的方向上；
 * 滤镜定义挂在 body 上，只挂一份
 */
import { afterEach, describe, expect, it } from 'vitest'
import { COLOR_VISION_FILTER_ID, colorVisionMatrix, ensureColorVisionFilter } from '~/composables/color-vision-filter'

/** Machado 2009 绿色弱（程度 1.0）模拟矩阵，线性 RGB：绿色弱的人看到的颜色 */
const SIMULATE = [
  [0.367322, 0.860646, -0.227968],
  [0.280085, 0.672501, 0.047413],
  [-0.01182, 0.04294, 0.968881],
]

const apply = (m: number[][], c: number[]) => m.map(row => row.reduce((sum, w, i) => sum + w * c[i]!, 0))
const clamp = (c: number[]) => c.map(v => Math.min(1, Math.max(0, v)))
const distance = (a: number[], b: number[]) => Math.hypot(...a.map((v, i) => v - b[i]!))

afterEach(() => {
  document.getElementById(COLOR_VISION_FILTER_ID)?.closest('svg')?.remove()
})

describe('校正矩阵', () => {
  it('白、黑与各档灰原样不动，对比度不受影响', () => {
    const m = colorVisionMatrix()
    for (const gray of [0, 0.2, 0.5, 0.8, 1]) {
      apply(m, [gray, gray, gray]).forEach(v => expect(v).toBeCloseTo(gray, 5))
    }
  })

  it('绿色弱看来几乎一样的红与绿，校正后拉开', () => {
    const m = colorVisionMatrix()
    const red = [0.8, 0.25, 0.1]
    const green = [0.3, 0.55, 0.1]
    const before = distance(apply(SIMULATE, red), apply(SIMULATE, green))
    const after = distance(apply(SIMULATE, clamp(apply(m, red))), apply(SIMULATE, clamp(apply(m, green))))
    expect(after).toBeGreaterThan(before * 1.5)
  })
})

describe('滤镜定义', () => {
  it('挂一份到 body，再要时复用，返回 url 引用', () => {
    const first = ensureColorVisionFilter()
    const second = ensureColorVisionFilter()

    expect(first).toBe(`url(#${COLOR_VISION_FILTER_ID})`)
    expect(second).toBe(first)
    expect(document.querySelectorAll(`#${COLOR_VISION_FILTER_ID}`)).toHaveLength(1)

    const filter = document.getElementById(COLOR_VISION_FILTER_ID)!
    expect(filter.closest('svg')!.getAttribute('aria-hidden')).toBe('true')
    expect(filter.getAttribute('color-interpolation-filters')).toBe('linearRGB')
    const values = filter.querySelector('feColorMatrix')!.getAttribute('values')!.split(' ').map(Number)
    expect(values).toHaveLength(20)
    expect(values.slice(15)).toEqual([0, 0, 0, 1, 0])
  })
})
