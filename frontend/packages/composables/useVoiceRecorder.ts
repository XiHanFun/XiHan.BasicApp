import { computed, onBeforeUnmount, ref } from 'vue'

/**
 * 语音录制（浏览器原生 MediaRecorder，不引第三方）。
 *
 * 格式由浏览器决定：Chrome / Firefox 产出 audio/webm;codecs=opus，Safari 产出 audio/mp4。
 * 不做转码，因此跨浏览器互通有限——Safari 放不了 webm/opus。
 */

/** 录音时长上限（秒），与后端 MaxVoiceSeconds 对齐 */
export const MAX_VOICE_SECONDS = 60

/** 低于这个秒数视为误触，不产出结果 */
const MIN_VOICE_SECONDS = 1

/** 按优先级挑一个当前浏览器支持的容器 */
const PREFERRED_MIME_TYPES = [
  'audio/webm;codecs=opus',
  'audio/webm',
  'audio/mp4',
  'audio/ogg;codecs=opus',
]

export interface VoiceRecording {
  file: File
  /** 取整后的秒数，向上取整保证 1 秒以内的录音不显示成 0 */
  durationSeconds: number
}

function pickMimeType(): string | undefined {
  if (typeof MediaRecorder === 'undefined') {
    return undefined
  }
  return PREFERRED_MIME_TYPES.find(type => MediaRecorder.isTypeSupported(type))
}

function extensionOf(mimeType: string | undefined): string {
  if (!mimeType) {
    return 'webm'
  }
  if (mimeType.includes('mp4')) {
    return 'm4a'
  }
  if (mimeType.includes('ogg')) {
    return 'ogg'
  }
  return 'webm'
}

export function useVoiceRecorder() {
  const recording = ref(false)
  const elapsed = ref(0)
  /** 浏览器是否具备录音能力（无 MediaRecorder 或非安全上下文时为 false） */
  const supported = computed(() =>
    typeof MediaRecorder !== 'undefined' && !!navigator.mediaDevices?.getUserMedia)

  let recorder: MediaRecorder | null = null
  let stream: MediaStream | null = null
  let chunks: Blob[] = []
  let tickTimer: ReturnType<typeof setInterval> | null = null
  let startedAt = 0
  let autoStopTimer: ReturnType<typeof setTimeout> | null = null
  let resolveResult: ((value: VoiceRecording | null) => void) | null = null

  function cleanup(): void {
    if (tickTimer) {
      clearInterval(tickTimer)
      tickTimer = null
    }
    if (autoStopTimer) {
      clearTimeout(autoStopTimer)
      autoStopTimer = null
    }
    stream?.getTracks().forEach(track => track.stop())
    stream = null
    recorder = null
    chunks = []
    recording.value = false
    elapsed.value = 0
  }

  /**
   * 开始录音。麦克风被拒绝或不支持时抛出，调用方负责提示。
   */
  async function start(): Promise<void> {
    if (recording.value || !supported.value) {
      return
    }
    stream = await navigator.mediaDevices.getUserMedia({ audio: true })
    const mimeType = pickMimeType()
    recorder = new MediaRecorder(stream, mimeType ? { mimeType } : undefined)
    chunks = []
    startedAt = Date.now()

    recorder.ondataavailable = (event) => {
      if (event.data.size > 0) {
        chunks.push(event.data)
      }
    }
    recorder.onstop = () => {
      const seconds = Math.ceil((Date.now() - startedAt) / 1000)
      const type = recorder?.mimeType || mimeType || 'audio/webm'
      const blob = new Blob(chunks, { type })
      const done = resolveResult
      resolveResult = null
      cleanup()
      if (!done) {
        return
      }
      // 太短当误触丢弃；blob 为空说明浏览器没吐出数据，同样按失败处理
      done(seconds < MIN_VOICE_SECONDS || blob.size === 0
        ? null
        : {
            file: new File([blob], `voice-${startedAt}.${extensionOf(type)}`, { type }),
            durationSeconds: Math.min(seconds, MAX_VOICE_SECONDS),
          })
    }

    recorder.start()
    recording.value = true
    tickTimer = setInterval(() => {
      elapsed.value = Math.floor((Date.now() - startedAt) / 1000)
    }, 200)
    // 到上限自动停：录到 60 秒仍不松手时直接出结果，而不是继续录成超长文件
    autoStopTimer = setTimeout(() => {
      recorder?.state === 'recording' && recorder.stop()
    }, MAX_VOICE_SECONDS * 1000)
  }

  /**
   * 停止并取回录音；时长不足或无数据时返回 null。
   */
  function stop(): Promise<VoiceRecording | null> {
    if (!recorder || recorder.state !== 'recording') {
      cleanup()
      return Promise.resolve(null)
    }
    return new Promise<VoiceRecording | null>((resolve) => {
      resolveResult = resolve
      recorder?.stop()
    })
  }

  /** 放弃本次录音（松手前移出按钮等） */
  function cancel(): void {
    const done = resolveResult
    resolveResult = null
    if (recorder?.state === 'recording') {
      recorder.onstop = null
      recorder.stop()
    }
    cleanup()
    done?.(null)
  }

  onBeforeUnmount(cancel)

  return { supported, recording, elapsed, start, stop, cancel }
}
