/**
 * 跨端同步设置的通用接法：本地即时恢复 + 后端为准 + 推送实时应用 + 回写本地。
 *
 * 多个浏览器同时登录时，本地那份可能是别的设备改动之前的旧值：先按它渲染、再被后端那份换掉，界面就会闪一下
 * （A 上清空了小组件，B 刷新先摆出旧的再消失）。所以：
 * - 关着同步：本地就是事实源，立即就绪；
 * - 开着同步：已在手的后端值（本会话拉过 / 收到过推送 / 本机保存过）直接采用、立即就绪；否则等后端那份回来再就绪，
 *   超过等待上限先就绪（按本地摆），后端回来再换；
 * - 后端那份与其它设备推来的值都经 apply 应用、经 writeLocal 写回本地，下次刷新首屏就是最新值；
 * - 空数组也是用户存下的值（全部删掉），由 accept 判定，别拿长度判断；
 * - 用户正在编辑时（isDirty）不拿迟到的后端值或推送覆盖改动。
 *
 * 调用方先从本地恢复初值，再把 ready 接到渲染上（未就绪时显示骨架或什么都不摆）。
 */
import type { Ref } from 'vue'
import { ref } from 'vue'

/** 开着同步时首屏最多空等后端多久（毫秒）：之后先按本地摆出来，后端回来再换 */
export const SYNCED_SETTING_WAIT = 1500

/** 设置在后端那一侧怎么读、怎么听 */
export interface SyncedSettingSource {
  /** 当前是否开启跨端同步 */
  enabled: () => boolean
  /** 已在手的后端值：有就返回 `{ value }`（value 可以是 undefined，表示后端没有这一项），没有返回 undefined */
  peek?: () => { value: unknown } | undefined
  /** 拉取后端值；失败或无记录 resolve undefined，不要 reject */
  load: () => Promise<unknown>
  /** 订阅其它设备推来的值，返回退订函数 */
  subscribe?: (listener: (value: unknown) => void) => () => void
}

/** 拿到后端值之后怎么用 */
export interface SyncedSettingTarget<T> {
  /** 认得的载荷才采用 */
  accept: (value: unknown) => value is T
  /** 应用到界面状态 */
  apply: (value: T) => void
  /** 写回本地 */
  writeLocal: (value: T) => void
  /** 用户正在编辑时为真 */
  isDirty?: () => boolean
}

export interface SyncedSettingBinding {
  /** 首屏能否渲染 */
  ready: Ref<boolean>
  /** 后端那份处理完（或本来就不用等）时 resolve */
  settled: Promise<void>
  /** 退订推送并清掉等待计时 */
  dispose: () => void
}

export interface SyncedSettingOptions {
  /** 开着同步时首屏最多空等多久（毫秒），缺省 SYNCED_SETTING_WAIT */
  wait?: number
  /** 变成就绪的那一刻调用一次（关着同步、已在手时立即调用）：多处共用一份状态时据此把就绪「只升不降」 */
  onReady?: () => void
}

export function bindSyncedSetting<T>(
  source: SyncedSettingSource,
  target: SyncedSettingTarget<T>,
  options: SyncedSettingOptions = {},
): SyncedSettingBinding {
  const adopt = (value: unknown) => {
    if (!target.accept(value) || target.isDirty?.()) {
      return
    }
    target.apply(value)
    target.writeLocal(value)
  }
  const unsubscribe = source.subscribe?.(adopt) ?? (() => {})
  const ready = ref(false)
  const markReady = () => {
    if (ready.value) {
      return
    }
    ready.value = true
    options.onReady?.()
  }

  if (!source.enabled()) {
    markReady()
    return { ready, settled: Promise.resolve(), dispose: unsubscribe }
  }
  const inHand = source.peek?.()
  if (inHand) {
    adopt(inHand.value)
    markReady()
    return { ready, settled: Promise.resolve(), dispose: unsubscribe }
  }

  let timer: ReturnType<typeof setTimeout> | null = setTimeout(markReady, options.wait ?? SYNCED_SETTING_WAIT)
  const clearTimer = () => {
    if (timer) {
      clearTimeout(timer)
      timer = null
    }
  }
  const settled = source.load()
    .then(adopt, () => {})
    .finally(() => {
      clearTimer()
      markReady()
    })
  return {
    ready,
    settled,
    dispose: () => {
      unsubscribe()
      clearTimer()
    },
  }
}
