/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

/** 对话框最大宽：数字按 px，字符串是完整的 CSS 长度；小屏照样收窄到视口 */
export type DialogWidth = number | string

/** 写到对话框内容部件上的最大宽 */
export function dialogWidthStyle(width: DialogWidth): Record<'--xh-dialog-max-w', string> {
  return {
    '--xh-dialog-max-w': typeof width === 'number'
      ? `${width}px`
      : width,
  }
}
