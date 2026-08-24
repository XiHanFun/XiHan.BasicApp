import type { RuntimeConfig } from '@xihan-ui/kernel'
import type { MaybeRefOrGetter } from 'vue'
import { createRuntimeConfig } from '@xihan-ui/kernel'
import { useScrollLock } from '@xihan-ui/vue/behavior'
import { getScrollRoot } from './useScrollRoot'

/**
 * 自建全屏浮层期间锁住页面滚动。
 *
 * 锁计数按文档维护，与组件库自己那些浮层（对话框、抽屉）共用同一份，
 * 两边同时开着时后收的那个才真正解锁。锁哪个元素由 scrollRoot 决定：
 * 本应用把滚动搬进了内容容器，body 自己不滚。
 */

let runtimeConfig: RuntimeConfig | null = null

function scrollLockConfig(): RuntimeConfig {
  runtimeConfig ??= createRuntimeConfig({ scrollRoot: getScrollRoot })
  return runtimeConfig
}

export function usePageScrollLock(active: MaybeRefOrGetter<boolean>): void {
  useScrollLock(active, scrollLockConfig)
}
