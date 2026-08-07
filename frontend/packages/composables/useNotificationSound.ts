/**
 * 消息提示音。
 *
 * 用 Web Audio 现场合成，不引第三方库、也不往仓库塞音频文件。
 * 聊天消息与站内通知各一套音色，是否发声由偏好 `notifySound` 决定。
 */

/** 相邻两声的最小间隔（秒）：每条都响，但岔开播放，不叠成一坨噪音 */
const SPACING = 0.2

/** 最多提前排到多久之后（秒）：刷屏时超出的直接丢，避免响个没完 */
const MAX_QUEUE_AHEAD = 1.4

/** 主音量：提示音只作提醒，压得比界面音效更低一档 */
const PEAK_GAIN = 0.09

/** 提示音种类 */
export type NotifySoundKind = 'chat' | 'notice'

interface ToneSpec {
  /** 频率（Hz） */
  frequency: number
  /** 相对本次发声起点的延迟（秒） */
  offset: number
  /** 时长（秒） */
  duration: number
  /** 相对主音量的比例 */
  gain: number
}

/**
 * 两套音色：
 * - chat  上行纯五度（A5→E6），短促明亮，像有人在叫你
 * - notice 下行小六度（E5→B4），低一档且稍长，偏公告口吻
 */
const VOICES: Record<NotifySoundKind, ToneSpec[]> = {
  chat: [
    { frequency: 880, offset: 0, duration: 0.14, gain: 1 },
    { frequency: 1318.51, offset: 0.1, duration: 0.2, gain: 0.8 },
  ],
  notice: [
    { frequency: 659.25, offset: 0, duration: 0.18, gain: 1 },
    { frequency: 493.88, offset: 0.13, duration: 0.32, gain: 0.85 },
  ],
}

type AudioContextCtor = typeof AudioContext

let context: AudioContext | null = null
/** 下一声最早可以从什么时候开始（AudioContext 时钟） */
let nextAvailableAt = 0
let enabledResolver: (() => boolean) | null = null

/** 注入是否发声的判定（由全局挂载的组件在 setup 时接上偏好） */
export function configureNotificationSound(isEnabled: () => boolean): void {
  enabledResolver = isEnabled
}

function resolveContext(): AudioContext | null {
  if (context) {
    return context
  }
  const Ctor: AudioContextCtor | undefined = window.AudioContext
    ?? (window as unknown as { webkitAudioContext?: AudioContextCtor }).webkitAudioContext
  if (!Ctor) {
    return null
  }
  try {
    context = new Ctor()
  }
  catch {
    // 部分环境（无音频设备、隐私模式）构造即抛，静默降级为不发声
    context = null
  }
  return context
}

/** 单个音：指数包络进出，避免方波式的爆音 */
function tone(audio: AudioContext, spec: ToneSpec, startAt: number): void {
  const at = startAt + spec.offset
  const peak = PEAK_GAIN * spec.gain
  const oscillator = audio.createOscillator()
  const gain = audio.createGain()
  oscillator.type = 'sine'
  oscillator.frequency.value = spec.frequency
  gain.gain.setValueAtTime(0.0001, at)
  gain.gain.exponentialRampToValueAtTime(peak, at + 0.012)
  gain.gain.exponentialRampToValueAtTime(0.0001, at + spec.duration)
  oscillator.connect(gain).connect(audio.destination)
  oscillator.start(at)
  oscillator.stop(at + spec.duration + 0.02)
}

/**
 * 播放一次提示音，每条消息都会响。
 * 密集到达时按 AudioContext 时钟依次排开，排得太靠后的直接丢；
 * 偏好关闭、浏览器不支持或尚未发生用户手势时静默跳过。
 */
export function playNotificationSound(kind: NotifySoundKind): void {
  if (enabledResolver && !enabledResolver()) {
    return
  }
  const audio = resolveContext()
  if (!audio) {
    return
  }

  // 自动播放策略：页面未发生过用户手势前 AudioContext 停在 suspended，
  // resume 会被拒绝——吞掉即可，等用户点过页面后自然能响
  if (audio.state === 'suspended') {
    void audio.resume().catch(() => {})
  }

  const earliest = audio.currentTime + 0.01
  const startAt = Math.max(earliest, nextAvailableAt)
  if (startAt - audio.currentTime > MAX_QUEUE_AHEAD) {
    return
  }
  nextAvailableAt = startAt + SPACING

  for (const spec of VOICES[kind]) {
    tone(audio, spec, startAt)
  }
}
