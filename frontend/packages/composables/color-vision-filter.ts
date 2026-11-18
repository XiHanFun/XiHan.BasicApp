/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

type Matrix3 = readonly (readonly [number, number, number])[]

/** 色觉校正滤镜的元素 id，useHtmlStyle 以 url(#…) 挂到 <html> 上 */
export const COLOR_VISION_FILTER_ID = 'app-color-vision-filter'

/** 绿色弱（deuteranomaly，程度 1.0）的模拟矩阵，线性 RGB，取自 Machado、Oliveira、Fernandes 2009 */
const DEUTERANOMALY: Matrix3 = [
  [0.367322, 0.860646, -0.227968],
  [0.280085, 0.672501, 0.047413],
  [-0.01182, 0.04294, 0.968881],
]

/** 看不出的那部分差异怎么挪：红通道不动，各取 0.7 加进绿与蓝，绿、蓝自己的差异原样补回 */
const SHIFT: Matrix3 = [
  [0, 0, 0],
  [0.7, 1, 0],
  [0.7, 0, 1],
]

/**
 * 红绿色觉校正矩阵（daltonize）
 * @remarks
 * 校正色 = 原色 + SHIFT ×（原色 − 模拟色）。每行和为 1：白仍是白、黑仍是黑、灰不变，对比度不受影响；
 * 只把红绿之间看不出的差异换到蓝黄方向上，红偏品红、绿保持绿，两者靠蓝通道拉开
 */
export function colorVisionMatrix(): number[][] {
  // Number(a === b) 是单位矩阵在 (a, b) 上的元
  return [0, 1, 2].map(row => [0, 1, 2].map((col) => {
    const shifted = SHIFT[row]!.reduce((sum, weight, k) => sum + weight * (Number(k === col) - DEUTERANOMALY[k]![col]!), 0)
    return Number((Number(row === col) + shifted).toFixed(6))
  }))
}

/** 首次用到时往 body 挂一份隐藏的 SVG 滤镜定义，返回 CSS filter 的取值 */
export function ensureColorVisionFilter(doc: Document = document): string {
  if (!doc.getElementById(COLOR_VISION_FILTER_ID)) {
    const ns = 'http://www.w3.org/2000/svg'
    const svg = doc.createElementNS(ns, 'svg')
    svg.setAttribute('aria-hidden', 'true')
    svg.setAttribute('width', '0')
    svg.setAttribute('height', '0')
    svg.style.position = 'absolute'

    const filter = doc.createElementNS(ns, 'filter')
    filter.id = COLOR_VISION_FILTER_ID
    // 模拟矩阵是在线性 RGB 里定的，滤镜也在线性空间里算
    filter.setAttribute('color-interpolation-filters', 'linearRGB')

    const matrix = doc.createElementNS(ns, 'feColorMatrix')
    matrix.setAttribute('type', 'matrix')
    // feColorMatrix 是 4×5：RGB 三行补上 A 列与偏移列，透明度一行原样
    const rows = colorVisionMatrix().map(row => [...row, 0, 0])
    rows.push([0, 0, 0, 1, 0])
    matrix.setAttribute('values', rows.flat().join(' '))

    filter.append(matrix)
    svg.append(filter)
    doc.body.append(svg)
  }
  return `url(#${COLOR_VISION_FILTER_ID})`
}
