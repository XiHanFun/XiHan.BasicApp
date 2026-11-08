/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import type { Ref } from 'vue'
import { resolveMotionPreference } from '@xihan-ui/motion'
import { onBeforeUnmount, ref, watch } from 'vue'

/** 翻一屏时与上一屏重叠的宽度：翻过去还看得到刚才最边上的那一项 */
const SCROLL_OVERLAP = 120

/** 滚轮一格折成横向位移的倍数，与顶栏水平菜单、标签栏一致 */
const WHEEL_FACTOR = 3

/**
 * 一行排不下的横条（图标集的页签带）：滚动条藏起来，交给两端的箭头与滚轮
 * 与顶栏水平菜单、标签栏同一套手感：箭头一次翻一屏（留一段重叠），滚轮把纵向滚动折成横向，到头的那一侧箭头置灰
 * viewport 是 overflow-x: auto 的那个盒；它挂上来才开始量，换了或卸了即解绑
 */
export function useScrollStrip(viewport: Ref<HTMLElement | null>) {
  const overflowing = ref(false)
  const atStart = ref(true)
  const atEnd = ref(true)

  function scrollBehavior(): ScrollBehavior {
    return resolveMotionPreference() === 'reduce'
      ? 'instant'
      : 'smooth'
  }

  /** 量一遍：排不排得下、离两端还有没有距离 */
  function measure() {
    const el = viewport.value
    if (!el)
      return
    // RTL 下 scrollLeft 为负值（现代引擎口径），取绝对值即可当作「离起点多远」
    const offset = Math.abs(el.scrollLeft)
    overflowing.value = el.scrollWidth > el.clientWidth + 1
    atStart.value = offset <= 1
    atEnd.value = offset + el.clientWidth >= el.scrollWidth - 1
  }

  /** 朝起点 / 终点翻一屏 */
  function scrollToward(side: 'start' | 'end') {
    const el = viewport.value
    if (!el)
      return
    const step = Math.max(el.clientWidth - SCROLL_OVERLAP, SCROLL_OVERLAP)
    const sign = (side === 'end'
      ? 1
      : -1) * (getComputedStyle(el).direction === 'rtl'
      ? -1
      : 1)
    el.scrollBy({
      behavior: scrollBehavior(),
      left: sign * step,
    })
  }

  /** 把某一项滚进可视区（当前选中的那一项） */
  function reveal(target: Element | null | undefined) {
    const el = viewport.value
    if (!el || !target || el.scrollWidth <= el.clientWidth)
      return
    target.scrollIntoView({
      behavior: scrollBehavior(),
      inline: 'nearest',
      block: 'nearest',
    })
  }

  /** 滚轮：排得下时不拦（让外面照常滚），排不下时把这一格折成横向 */
  function onWheel(event: WheelEvent) {
    const el = viewport.value
    if (!el || el.scrollWidth <= el.clientWidth)
      return
    event.preventDefault()
    el.scrollBy({
      left: (event.deltaY !== 0
        ? event.deltaY
        : event.deltaX) * WHEEL_FACTOR,
    })
  }

  let observer: null | ResizeObserver = null

  function detach(el: HTMLElement | null | undefined) {
    observer?.disconnect()
    observer = null
    el?.removeEventListener('scroll', measure)
    el?.removeEventListener('wheel', onWheel)
  }

  function attach(el: HTMLElement | null) {
    if (!el)
      return
    // 盒自己变宽变窄、里面的内容换了宽度，都要重量
    observer = new ResizeObserver(measure)
    observer.observe(el)
    if (el.firstElementChild)
      observer.observe(el.firstElementChild)
    el.addEventListener('scroll', measure, { passive: true })
    el.addEventListener('wheel', onWheel, { passive: false })
    measure()
  }

  watch(viewport, (el, previous) => {
    detach(previous)
    attach(el)
  }, {
    immediate: true,
    flush: 'post',
  })

  onBeforeUnmount(() => detach(viewport.value))

  return {
    overflowing,
    atStart,
    atEnd,
    measure,
    scrollToward,
    reveal,
  }
}
