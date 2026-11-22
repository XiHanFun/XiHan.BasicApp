/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import type { PageGuide } from '~/stores'
import { inject, onActivated, onBeforeUnmount, onDeactivated, onMounted } from 'vue'
import { PAGE_GUIDE_HOST, useGuideStore } from '~/stores'

/**
 * 页面登记自己的引导：挂载、激活时登记，停用、卸载时撤掉
 * 分屏副侧渲染的页面不登记（它不是当前页）；在页面组件的 setup 里调用
 * @param guide 页面根、就绪条件与章节
 */
export function usePageGuide(guide: PageGuide): void {
  if (!inject(PAGE_GUIDE_HOST, true))
    return
  const store = useGuideStore()
  let release: (() => void) | null = null
  function register(): void {
    release?.()
    release = store.registerPage(guide)
  }
  function unregister(): void {
    release?.()
    release = null
  }
  onMounted(register)
  onActivated(register)
  onDeactivated(unregister)
  onBeforeUnmount(unregister)
}
