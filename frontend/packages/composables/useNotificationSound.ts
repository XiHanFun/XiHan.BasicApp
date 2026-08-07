/**
 * 消息提示音。
 *
 * 用 Web Audio 现场合成两声「叮咚」，不引第三方库、也不往仓库塞音频文件。
 * 由聊天新消息与站内通知调用，是否发声由偏好 `notifySound` 决定。
 */

/** 两次发声的最小间隔：一波消息连着到时不至于响成一串 */
const MIN_INTERVAL = 1500

/** 主音量：提示音只作提醒，压得比界面音效更低一档 */
const PEAK_GAIN = 0.09

type AudioContextCtor = typeof AudioContext

let context: AudioContext | null = null
let lastPlayedAt = 0
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
function tone(audio: AudioContext, frequency: number, startAt: number, duration: number, peak: number): void {
  const oscillator = audio.createOscillator()
  const gain = audio.createGain()
  oscillator.type = 'sine'
  oscillator.frequency.value = frequency
  gain.gain.setValueAtTime(0.0001, startAt)
  gain.gain.exponentialRampToValueAtTime(peak, startAt + 0.012)
  gain.gain.exponentialRampToValueAtTime(0.0001, startAt + duration)
  oscillator.connect(gain).connect(audio.destination)
  oscillator.start(startAt)
  oscillator.stop(startAt + duration + 0.02)
}

/**
 * 播放一次提示音。偏好关闭、节流未到、浏览器不支持或尚未发生用户手势时静默跳过。
 */
export function playNotificationSound(): void {
  if (enabledResolver && !enabledResolver()) {
    return
  }
  const now = Date.now()
  if (now - lastPlayedAt < MIN_INTERVAL) {
    return
  }
  const audio = resolveContext()
  if (!audio) {
    return
  }
  lastPlayedAt = now

  // 自动播放策略：页面未发生过用户手势时 AudioContext 停在 suspended，
  // resume 会被拒绝——吞掉即可，等用户点过页面后自然能响
  if (audio.state === 'suspended') {
    void audio.resume().catch(() => {})
  }

  const startAt = audio.currentTime + 0.01
  // A5 → E6，两声上行小三度，短促不刺耳
  tone(audio, 880, startAt, 0.14, PEAK_GAIN)
  tone(audio, 1318.51, startAt + 0.1, 0.2, PEAK_GAIN * 0.8)
}
