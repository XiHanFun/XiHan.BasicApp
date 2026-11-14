/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

/**
 * 页面标题：正文由路由守卫给，未读数由别处给，两者合成 document.title
 * 有未读时标题前面带「(n) 」，标签页不在眼前也看得到
 */

/** 标题里未读数的上限，超过写成 99+ */
const BADGE_MAX = 99

let base: null | string = null
let badge = 0

function render(): void {
  if (base === null)
    return
  document.title = badge > 0
    ? `(${badge > BADGE_MAX
      ? `${BADGE_MAX}+`
      : badge}) ${base}`
    : base
}

/**
 * 设置标题正文（不含未读数）
 * @param title 标题正文
 */
export function setDocumentTitle(title: string): void {
  base = title
  render()
}

/**
 * 设置标题前面的未读数
 * @param count 未读数；0 即不带
 */
export function setDocumentTitleBadge(count: number): void {
  // 正文还没人给过：页面现有的标题就是正文
  base ??= document.title
  badge = Math.max(0, Math.trunc(count))
  render()
}
