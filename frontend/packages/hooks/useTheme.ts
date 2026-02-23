import type { GlobalThemeOverrides } from 'naive-ui'
import { darkTheme, lightTheme, useOsTheme } from 'naive-ui'
import { computed, nextTick, watch } from 'vue'
import { THEME_AUTO } from '~/constants'
import { useAppStore } from '~/stores'

/** 将 hex 颜色解析为 HSL 数值三元组 [h(0-360), s(0-100), l(0-100)] */
function hexToHsl(hex: string): [number, number, number] {
  const r = Number.parseInt(hex.slice(1, 3), 16) / 255
  const g = Number.parseInt(hex.slice(3, 5), 16) / 255
  const b = Number.parseInt(hex.slice(5, 7), 16) / 255
  const max = Math.max(r, g, b)
  const min = Math.min(r, g, b)
  let h = 0
  let s = 0
  const l = (max + min) / 2
  if (max !== min) {
    const d = max - min
    s = l > 0.5 ? d / (2 - max - min) : d / (max + min)
    switch (max) {
      case r:
        h = ((g - b) / d + (g < b ? 6 : 0)) / 6
        break
      case g:
        h = ((b - r) / d + 2) / 6
        break
      default:
        h = ((r - g) / d + 4) / 6
    }
  }
  return [Math.round(h * 360), Math.round(s * 100), Math.round(l * 100)]
}

/** 将 HSL 数值转换为 hex 字符串 */
function hslToHex(h: number, s: number, l: number): string {
  const sl = s / 100
  const ll = l / 100
  const a = sl * Math.min(ll, 1 - ll)
  const f = (n: number) => {
    const k = (n + h / 30) % 12
    const color = ll - a * Math.max(Math.min(k - 3, 9 - k, 1), -1)
    return Math.round(255 * color)
      .toString(16)
      .padStart(2, '0')
  }
  return `#${f(0)}${f(8)}${f(4)}`
}

/** 将 HSL 数值转换为 CSS 变量用的分量字符串（如 "212 100% 45%"） */
function hslToVars(h: number, s: number, l: number): string {
  return `${h} ${s}% ${l}%`
}

/** 将 hex 颜色转换为 HSL 分量字符串（供 CSS 变量直接使用） */
function hexToHslVars(hex: string): string {
  const [h, s, l] = hexToHsl(hex)
  return hslToVars(h, s, l)
}

/** 将亮度值夹在 [5, 95] 范围内，避免主色变为纯白/纯黑 */
function clampL(l: number): number {
  return Math.max(5, Math.min(95, l))
}

/**
 * 从主色 hex 生成 hover/active/suppl 变体
 * - hover:  亮度 +8%（较亮，悬停反馈）
 * - active: 亮度 -8%（较暗，按下反馈）
 * - suppl:  亮度 +15%（深色模式下的补充色，更亮以保持对比度）
 */
function generatePrimaryScale(hex: string) {
  const [h, s, l] = hexToHsl(hex)
  return {
    base: hex,
    hover: hslToHex(h, s, clampL(l + 8)),
    active: hslToHex(h, s, clampL(l - 8)),
    suppl: hslToHex(h, s, clampL(l + 15)),
  }
}

/**
 * 读取 CSS 变量值并转换为 TinyColor 兼容的逗号格式 hsl()。
 * CSS 变量存储格式为 "H S% L%"（CSS Level 4 空格语法），
 * 而 Naive UI 内部的 TinyColor 只支持老式逗号语法 "hsl(H, S%, L%)"。
 */
function getCssColorVar(varName: string, fallback = ''): string {
  if (typeof document === 'undefined') return fallback
  const raw = getComputedStyle(document.documentElement).getPropertyValue(varName).trim()
  if (!raw) return fallback
  // "142 71% 45%" → "hsl(142, 71%, 45%)"
  const parts = raw.split(/\s+/)
  if (parts.length >= 3) {
    const h = parts[0]
    const s = parts[1].endsWith('%') ? parts[1] : `${parts[1]}%`
    const l = parts[2].endsWith('%') ? parts[2] : `${parts[2]}%`
    return `hsl(${h}, ${s}, ${l})`
  }
  return fallback
}

export function useTheme() {
  const appStore = useAppStore()
  const osTheme = useOsTheme()

  const isDark = computed(() => {
    if (appStore.themeMode === THEME_AUTO) {
      return osTheme.value === 'dark'
    }
    return appStore.themeMode === 'dark'
  })

  const naiveTheme = computed(() => (isDark.value ? darkTheme : lightTheme))

  /** 计算圆角像素值（供 CSS 变量同步及 Naive UI themeOverrides 共用） */
  function calcRadius(r: number) {
    return {
      radius: `${Math.round(4 + r * 12)}px`,
      cardRadius: `${Math.round(6 + r * 10)}px`,
    }
  }

  /** 将当前 uiRadius 同步到根元素 CSS 变量，供非 Naive UI 的自定义元素使用 */
  function syncRadiusCssVars(r: number) {
    if (typeof document === 'undefined') return
    const { radius, cardRadius } = calcRadius(r)
    document.documentElement.style.setProperty('--radius', radius)
    document.documentElement.style.setProperty('--radius-card', cardRadius)
  }

  watch(() => appStore.uiRadius, syncRadiusCssVars, { immediate: true })

  const themeOverrides = computed((): GlobalThemeOverrides => {
    const { radius, cardRadius } = calcRadius(appStore.uiRadius)
    const scale = generatePrimaryScale(appStore.themeColor)
    return {
      common: {
        primaryColor: scale.base,
        primaryColorHover: scale.hover,
        primaryColorPressed: scale.active,
        primaryColorSuppl: scale.suppl,
        successColor: getCssColorVar('--success', '#18a058'),
        successColorHover: getCssColorVar('--success', '#18a058'),
        successColorPressed: getCssColorVar('--success', '#18a058'),
        successColorSuppl: getCssColorVar('--success', '#18a058'),
        warningColor: getCssColorVar('--warning', '#f0a020'),
        warningColorHover: getCssColorVar('--warning', '#f0a020'),
        warningColorPressed: getCssColorVar('--warning', '#f0a020'),
        warningColorSuppl: getCssColorVar('--warning', '#f0a020'),
        errorColor: getCssColorVar('--destructive', '#d03050'),
        errorColorHover: getCssColorVar('--destructive', '#d03050'),
        errorColorPressed: getCssColorVar('--destructive', '#d03050'),
        errorColorSuppl: getCssColorVar('--destructive', '#d03050'),
        infoColor: getCssColorVar('--info', '#2080f0'),
        infoColorHover: getCssColorVar('--info', '#2080f0'),
        infoColorPressed: getCssColorVar('--info', '#2080f0'),
        infoColorSuppl: getCssColorVar('--info', '#2080f0'),
        borderRadius: radius,
      },
      Menu: {
        color: 'transparent',
        colorInverted: 'transparent',
      },
    }
  })

  function toggleTheme() {
    appStore.toggleTheme()
  }

  interface VTResult {
    ready: Promise<void>
    finished: Promise<void>
    skipTransition?: () => void
  }

  function animateThemeTransition(mode: 'light' | 'dark', e?: MouseEvent) {
    if (appStore.themeMode === mode) return

    // 无动画或浏览器不支持：直接切换，抑制 CSS 过渡一帧
    if (!appStore.themeAnimationEnabled || !('startViewTransition' in document)) {
      document.documentElement.classList.add('theme-switching')
      appStore.setTheme(mode)
      requestAnimationFrame(() => document.documentElement.classList.remove('theme-switching'))
      return
    }

    const x = e?.clientX ?? window.innerWidth / 2
    const y = e?.clientY ?? window.innerHeight / 2
    const endRadius = Math.hypot(
      Math.max(x, window.innerWidth - x),
      Math.max(y, window.innerHeight - y),
    )

    // clipPath 起止：从点击处 0 → 全屏
    const clipPath = [`circle(0px at ${x}px ${y}px)`, `circle(${endRadius}px at ${x}px ${y}px)`]

    // 全程抑制 CSS transition，防止截图期间元素颜色渐变产生残影
    document.documentElement.classList.add('theme-switching')

    const transition = (
      document as Document & { startViewTransition: (cb: () => Promise<void>) => VTResult }
    ).startViewTransition(async () => {
      appStore.setTheme(mode)
      // 等 Vue 全部 DOM 更新完毕，浏览器才截"新主题"快照
      // 缺少此步时截图不完整，是切亮色闪烁的根因（参照 vben 实现）
      await nextTick()
    })

    const toDark = mode === 'dark'
    transition.ready
      .then(() => {
        // 对齐 vben：
        //   切暗色 → 旧层（亮）在上，全屏 → 0 收缩（z-index 由 html.dark CSS 类自动控制）
        //   切亮色 → 新层（亮）在上，0 → 全屏 扩散
        const anim = document.documentElement.animate(
          { clipPath: toDark ? [...clipPath].reverse() : clipPath },
          {
            duration: 450,
            easing: 'ease-in',
            pseudoElement: toDark ? '::view-transition-old(root)' : '::view-transition-new(root)',
          } as KeyframeAnimationOptions,
        )
        anim.onfinish = () => {
          // 动画结束后立即跳过剩余 ViewTransition，消除尾帧闪烁（vben 同款做法）
          transition.skipTransition?.()
          document.documentElement.classList.remove('theme-switching')
        }
      })
      .catch(() => {
        document.documentElement.classList.remove('theme-switching')
      })
  }

  function toggleThemeWithTransition(e?: MouseEvent) {
    animateThemeTransition(isDark.value ? 'light' : 'dark', e)
  }

  function followSystem() {
    appStore.setFollowSystemTheme()
  }

  function setThemeColor(color: string) {
    appStore.setThemeColor(color)
    if (color?.startsWith('#') && color.length >= 7) {
      document.documentElement.style.setProperty('--primary', hexToHslVars(color))
    }
  }

  // 初始化及变化时同步 --primary CSS 变量
  watch(
    () => appStore.themeColor,
    (color) => {
      if (color?.startsWith('#') && color.length >= 7) {
        document.documentElement.style.setProperty('--primary', hexToHslVars(color))
      }
    },
    { immediate: true },
  )

  return {
    isDark,
    naiveTheme,
    themeOverrides,
    toggleTheme,
    toggleThemeWithTransition,
    animateThemeTransition,
    followSystem,
    setThemeColor,
  }
}
