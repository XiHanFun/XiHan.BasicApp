/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import type { KbdResolvedPlatform } from '@xihan-ui/core/hotkey'
import { detectKbdPlatform, formatHotkey, matchesHotkey } from '@xihan-ui/core/hotkey'
import { isTypingTarget } from '@xihan-ui/headless'
import { onBeforeUnmount, onMounted, ref, toValue, watchEffect } from 'vue'

export interface HotkeyTriggerDetails {
  keys: readonly string[]
  event: KeyboardEvent
}

export interface UseHotkeyOptions {
  /** 单键或组合键，例如 ['Escape']、['Mod', 'K']；`Mod` 在 Mac 上是 ⌘、其余平台是 Ctrl */
  keys: readonly string[]
  /** 已注册的监听是否生效，缺省生效 */
  enabled?: boolean
  /** 命中时是否阻止浏览器默认动作，缺省阻止 */
  preventDefault?: boolean
  /** 监听装在哪儿：缺省 document；局部监听传返回节点的函数，节点还没挂上时先不装 */
  target?: () => EventTarget | null
  onHotKey: (details: HotkeyTriggerDetails) => void
}

/**
 * 注册一个按键组合，只注册、不铺键帽；返回提前解绑的句柄，作用域销毁时自动解绑
 * 组件库只有展示键帽的 XhKbd，不需可见提示的命令由应用用 matchesHotkey 与 isTypingTarget 自装监听，键位解析与 XhKbd 同一套
 * 判据同 XhKbd：输入法合成期间不响应；没有命令修饰键（Ctrl / ⌘ / Alt）的组合落在输入框、文本域、可编辑区时不响应
 */
export function useHotkey(options: UseHotkeyOptions | (() => UseHotkeyOptions)): () => void {
  // 平台要等挂载后才测得出来：落定前 'Mod' 会解析成 Control，Mac 上 ⌘K 按不出来
  const platform = ref<KbdResolvedPlatform>('other')
  onMounted(() => {
    platform.value = detectKbdPlatform()
  })

  // 每次都现取选项：监听节点可以不变，接不接这次按键的判据却随选项走
  const onKeyDown = (raw: Event): void => {
    const event = raw as KeyboardEvent
    const o = toValue(options)
    if (o.enabled === false || event.isComposing)
      return
    const segments = formatHotkey(o.keys, platform.value)
    const hasCommandModifier = segments.some(segment => segment.modifier && segment.key !== 'Shift')
    if (!hasCommandModifier && isTypingTarget(event.target))
      return
    if (!matchesHotkey(event, o.keys, platform.value))
      return
    if (o.preventDefault !== false)
      event.preventDefault()
    o.onHotKey({
      keys: [...o.keys],
      event,
    })
  }

  let bound: EventTarget | null = null
  const stop = (): void => {
    bound?.removeEventListener('keydown', onKeyDown)
    bound = null
  }

  watchEffect(() => {
    const custom = toValue(options).target
    const next = custom
      ? custom()
      : (typeof document === 'undefined'
          ? null
          : document)
    if (next === bound)
      return
    stop()
    bound = next
    bound?.addEventListener('keydown', onKeyDown)
  })
  onBeforeUnmount(stop)

  return stop
}
