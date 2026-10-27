/**
 * 消息提示音。
 *
 * 声音取组件库 @xihan-ui/sound 的语义音效，与组件库文档里试听的是同一套：站内通知 notification、
 * 收到聊天消息 receive、自己的消息发送成功 send。走 @xihan-ui/vue/sound 的共享播放器，音频上下文等第一次
 * 真正发声才建立；是否发声由偏好 `notifySound` 决定。
 */
import type { BuiltinSoundName } from '@xihan-ui/sound'
import { attachDocumentSoundUnlock } from '@xihan-ui/sound'
import { getSoundPlayer } from '@xihan-ui/vue/sound'

/** 相邻两声的最小间隔（毫秒）：每条都响，但岔开播放，不叠成一坨噪音 */
const SPACING = 200

/** 最多提前排到多久之后（毫秒）：刷屏时超出的直接丢，避免响个没完 */
const MAX_QUEUE_AHEAD = 1400

/** 提示音：组件库声音主题里的语义名 */
export type NotifySoundKind = Extract<BuiltinSoundName, 'notification' | 'receive' | 'send'>

/** 下一声最早可以从什么时候开始（performance.now 时钟） */
let nextAvailableAt = 0
let enabledResolver: (() => boolean) | null = null
let unlockAttached = false

function enabled(): boolean {
  return !enabledResolver || enabledResolver()
}

/**
 * 注入是否发声的判定（由全局挂载的组件在 setup 时接上偏好），顺带挂上首次手势解锁：
 * 浏览器要求页面上发生过点击或按键后音频才能出声，而消息推送不是手势，
 * 借用户的第一下操作把音频上下文解锁，之后推来的消息才响得出来。
 */
export function configureNotificationSound(isEnabled: () => boolean): void {
  enabledResolver = isEnabled
  if (unlockAttached)
    return
  unlockAttached = true
  attachDocumentSoundUnlock(() => {
    if (enabled())
      getSoundPlayer().unlock()
  })
}

/**
 * 播放一次提示音，每条消息都会响。
 * 密集到达时依次排开，排得太靠后的直接丢；偏好关闭、浏览器不支持或尚未发生用户手势时静默跳过。
 */
export function playNotificationSound(kind: NotifySoundKind): void {
  if (!enabled())
    return
  const now = performance.now()
  const startAt = Math.max(now, nextAvailableAt)
  if (startAt - now > MAX_QUEUE_AHEAD)
    return
  nextAvailableAt = startAt + SPACING
  if (startAt === now) {
    getSoundPlayer().play(kind)
    return
  }
  setTimeout(() => {
    // 排队期间偏好可能已关
    if (enabled())
      getSoundPlayer().play(kind)
  }, startAt - now)
}
