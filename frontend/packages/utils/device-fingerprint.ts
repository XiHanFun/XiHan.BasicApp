type AudioWindow = Window & {
  webkitOfflineAudioContext?: typeof OfflineAudioContext
}

type DeviceNavigator = Navigator & {
  deviceMemory?: number
  getBattery?: () => Promise<unknown>
}

/**
 * 设备指纹生成工具
 * 基于多维度浏览器/硬件特征生成稳定的设备唯一标识
 * 无外部依赖，通过 Canvas/WebGL/Audio/硬件参数等综合判定
 */

async function getCanvasFingerprint(): Promise<string> {
  try {
    const canvas = document.createElement('canvas')
    canvas.width = 200
    canvas.height = 50
    const ctx = canvas.getContext('2d')
    if (!ctx)
      return ''

    ctx.textBaseline = 'top'
    ctx.font = '14px Arial'
    ctx.fillStyle = '#f60'
    ctx.fillRect(100, 1, 62, 20)
    ctx.fillStyle = '#069'
    ctx.fillText('XiHan.FP', 2, 15)
    ctx.fillStyle = 'rgba(102,204,0,0.7)'
    ctx.fillText('XiHan.FP', 4, 17)

    return canvas.toDataURL()
  }
  catch {
    return ''
  }
}

function getWebGLFingerprint(): string {
  try {
    const canvas = document.createElement('canvas')
    const gl = canvas.getContext('webgl') || canvas.getContext('experimental-webgl')
    if (!gl || !(gl instanceof WebGLRenderingContext))
      return ''

    const debugInfo = gl.getExtension('WEBGL_debug_renderer_info')
    const vendor = debugInfo ? gl.getParameter(debugInfo.UNMASKED_VENDOR_WEBGL) : ''
    const renderer = debugInfo ? gl.getParameter(debugInfo.UNMASKED_RENDERER_WEBGL) : ''

    return `${vendor}~${renderer}`
  }
  catch {
    return ''
  }
}

/**
 * 音频指纹：离线渲染一段固定波形，取样本能量作为特征。
 *
 * 必须用 OfflineAudioContext 而非实时 AudioContext：登录页在 onMounted 即取指纹，
 * 此时没有用户手势，自动播放策略会把实时上下文挂起，onaudioprocess 永不触发，只能等超时拿空串；
 * 而退出登录后客户端跳转过来的登录页已有手势，同一段代码又能拿到真实值——
 * 同一台设备算出两种指纹，会被判成新设备而反复触发设备变更通知。
 * 离线渲染不受手势限制，结果只取决于硬件与浏览器实现，因而稳定。
 */
function getAudioFingerprint(): Promise<string> {
  return new Promise((resolve) => {
    try {
      const OfflineCtor = window.OfflineAudioContext
        || (window as AudioWindow).webkitOfflineAudioContext
      if (!OfflineCtor) {
        resolve('')
        return
      }

      const ctx = new OfflineCtor(1, 44100, 44100)
      const oscillator = ctx.createOscillator()
      oscillator.type = 'triangle'
      oscillator.frequency.setValueAtTime(10000, ctx.currentTime)

      // 压缩器的实现差异是音频指纹的主要熵来源
      const compressor = ctx.createDynamicsCompressor()
      compressor.threshold.setValueAtTime(-50, ctx.currentTime)
      compressor.knee.setValueAtTime(40, ctx.currentTime)
      compressor.ratio.setValueAtTime(12, ctx.currentTime)
      compressor.attack.setValueAtTime(0, ctx.currentTime)
      compressor.release.setValueAtTime(0.25, ctx.currentTime)

      oscillator.connect(compressor)
      compressor.connect(ctx.destination)
      oscillator.start(0)

      void ctx.startRendering()
        .then((buffer) => {
          const data = buffer.getChannelData(0)
          let sum = 0
          for (let i = 4500; i < 5000; i++) {
            sum += Math.abs(data[i] ?? 0)
          }
          resolve(sum.toString())
        })
        .catch(() => resolve(''))
    }
    catch {
      resolve('')
    }
  })
}

function getNavigatorFeatures(): string {
  const nav = window.navigator as DeviceNavigator
  return [
    nav.language || '',
    (nav.languages || []).join(','),
    nav.platform || '',
    nav.hardwareConcurrency ?? '',
    nav.deviceMemory ?? '',
    nav.maxTouchPoints ?? 0,
    'ontouchstart' in window ? 1 : 0,
    nav.cookieEnabled ? 1 : 0,
    typeof nav.getBattery === 'function' ? 1 : 0,
  ].join('|')
}

function getScreenFeatures(): string {
  return [
    screen.width,
    screen.height,
    screen.colorDepth,
    screen.pixelDepth,
    window.devicePixelRatio ?? 1,
    screen.availWidth,
    screen.availHeight,
  ].join('|')
}

function getTimezoneFeatures(): string {
  const offset = new Date().getTimezoneOffset()
  let tz = ''
  try {
    tz = new Intl.DateTimeFormat().resolvedOptions().timeZone || ''
  }
  catch { /* ignore */ }
  return `${offset}|${tz}`
}

function getPluginFeatures(): string {
  try {
    const plugins = navigator.plugins
    if (!plugins || plugins.length === 0)
      return ''
    const names: string[] = []
    for (let i = 0; i < Math.min(plugins.length, 20); i++) {
      names.push(plugins[i]!.name)
    }
    return names.sort().join(',')
  }
  catch {
    return ''
  }
}

function getFontFeatures(): string {
  // 通过 canvas 测量特定字体是否可用来检测已安装字体
  const testFonts = [
    'Arial',
    'Verdana',
    'Times New Roman',
    'Courier New',
    'Georgia',
    'Trebuchet MS',
    'Impact',
    'Comic Sans MS',
    'Microsoft YaHei',
    'SimHei',
    'Consolas',
    'Menlo',
    'Monaco',
    'Fira Code',
  ]
  const baseFonts = ['monospace', 'sans-serif', 'serif']
  const testString = 'mmMwWLli10O&1'
  const testSize = '72px'
  const canvas = document.createElement('canvas')
  const ctx = canvas.getContext('2d')
  if (!ctx)
    return ''

  const widths: Record<string, number> = {}
  for (const base of baseFonts) {
    ctx.font = `${testSize} ${base}`
    widths[base] = ctx.measureText(testString).width
  }

  const detected: string[] = []
  for (const font of testFonts) {
    for (const base of baseFonts) {
      ctx.font = `${testSize} '${font}', ${base}`
      if (ctx.measureText(testString).width !== widths[base]) {
        detected.push(font)
        break
      }
    }
  }
  return detected.join(',')
}

/**
 * DJB2a 哈希变体，输出 hex 字符串
 */
function djb2Hash(str: string): string {
  let hash = 5381
  for (let i = 0; i < str.length; i++) {
    hash = ((hash << 5) + hash) ^ str.charCodeAt(i)
  }
  return (hash >>> 0).toString(16)
}

/**
 * 生成设备指纹
 * 综合 Canvas/WebGL/Audio/Navigator/Screen/Timezone/Plugins/Fonts 多维特征
 * @returns 稳定的设备标识字符串（格式 dfp_xxxxxxxx_xxxxxxxx）
 */
export async function generateDeviceFingerprint(): Promise<string> {
  const [canvasFp, audioFp] = await Promise.all([
    getCanvasFingerprint(),
    getAudioFingerprint(),
  ])

  const components = [
    canvasFp,
    getWebGLFingerprint(),
    audioFp,
    getNavigatorFeatures(),
    getScreenFeatures(),
    getTimezoneFeatures(),
    getPluginFeatures(),
    getFontFeatures(),
  ]

  const raw = components.join('###')

  // 双段哈希减少碰撞
  const mid = Math.floor(raw.length / 2)
  const h1 = djb2Hash(raw.slice(0, mid))
  const h2 = djb2Hash(raw.slice(mid))

  return `dfp_${h1}_${h2}`
}
