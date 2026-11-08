/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import { computed } from 'vue'
import { useIsMobile } from '~/composables/useIsMobile'
import { useAppStore } from '~/stores'

/**
 * 生效的布局模式：小屏一律按 side 渲染，不改用户偏好
 * 其余模式依赖横向空间或双列，窄屏下不可用
 * 布局组件一律读本值而非 `appStore.layoutMode`，否则外壳与头部、侧栏形态错位
 */
export function useEffectiveLayoutMode() {
  const appStore = useAppStore()
  const { isMobile } = useIsMobile()

  return computed(() => (isMobile.value ? 'side' : appStore.layoutMode))
}
