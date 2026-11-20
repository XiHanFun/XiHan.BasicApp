/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

/**
 * 编辑弹窗的宽度档：取 Tailwind 的容器刻度（sm 24rem … 7xl 80rem），小屏照样收窄到视口
 * rem 随偏好里的字号缩放，缺省 14px 根下 3xl 是 672px
 */
export type EditModalWidth = 'sm' | 'md' | 'lg' | 'xl' | '2xl' | '3xl' | '4xl' | '5xl' | '6xl' | '7xl'

/** 各档对应的最大宽工具类：类名写全，Tailwind 才扫得到 */
export const EDIT_MODAL_WIDTH_CLASS: Record<EditModalWidth, string> = {
  'sm': 'max-w-sm',
  'md': 'max-w-md',
  'lg': 'max-w-lg',
  'xl': 'max-w-xl',
  '2xl': 'max-w-2xl',
  '3xl': 'max-w-3xl',
  '4xl': 'max-w-4xl',
  '5xl': 'max-w-5xl',
  '6xl': 'max-w-6xl',
  '7xl': 'max-w-7xl',
}
