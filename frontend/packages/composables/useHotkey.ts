import type { KbdResolvedPlatform } from '@xihan-ui/headless'
import { detectKbdPlatform, formatHotkey, isTypingTarget, matchesHotkey } from '@xihan-ui/headless'
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
 * 注册一组按键组合，只注册、不铺键帽。
 *
 * XiHan.UI 只保留 XhKbd 一件（展示键帽，`register` 打开才顺带监听），文档明说
 * 「只注册、不需要可见提示的全局命令放应用自己的命令系统」；本应用的全局快捷键
 * 与画布内的全选都是这一类，这里拿 headless 的 matchesHotkey / isTypingTarget
 * 自装监听，键位解析与 XhKbd 铺出来的键帽同一套规则。
 *
 * 判据与 XhKbd 一致：输入法合成期间不响应；没有命令修饰键（Ctrl / ⌘ / Alt）的组合
 * 落在正在打字的地方（输入框、文本域、可编辑区）时不响应。
 *
 * 一次调用管一组组合，注册四条就调四次。返回提前解绑的句柄，作用域销毁时自动解绑。
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
    o.onHotKey({ keys: [...o.keys], event })
  }

  let bound: EventTarget | null = null
  const stop = (): void => {
    bound?.removeEventListener('keydown', onKeyDown)
    bound = null
  }

  watchEffect(() => {
    const custom = toValue(options).target
    const next = custom ? custom() : (typeof document === 'undefined' ? null : document)
    if (next === bound)
      return
    stop()
    bound = next
    bound?.addEventListener('keydown', onKeyDown)
  })
  onBeforeUnmount(stop)

  return stop
}
