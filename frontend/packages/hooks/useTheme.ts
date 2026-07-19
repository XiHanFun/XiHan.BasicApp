import type { GlobalThemeOverrides } from 'naive-ui'
import { darkTheme, lightTheme, useOsTheme } from 'naive-ui'
import { computed, nextTick, watch } from 'vue'
import { THEME_AUTO } from '~/constants'
import { useAppStore } from '~/stores'

/**
 * 主题扩散动画起点：鼠标事件，或调用方自算的视口坐标
 * （如 change 事件本身无坐标，由控件矩形中心换算）
 */
export type ThemeTransitionOrigin = MouseEvent | { clientX: number, clientY: number }

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

/** 计算 hex 的相对亮度（WCAG），用于决定主色上的前景文字取深/浅 */
function relLuminance(hex: string): number {
  const channel = (i: number) => {
    const c = Number.parseInt(hex.slice(i, i + 2), 16) / 255
    return c <= 0.03928 ? c / 12.92 : ((c + 0.055) / 1.055) ** 2.4
  }
  return 0.2126 * channel(1) + 0.7152 * channel(3) + 0.0722 * channel(5)
}

/**
 * Material You 动态取色：从单个品牌色（seed）推导整套和谐色阶，明暗自适应。
 *
 * 设计取舍：`--primary` 保持用户所选的精确颜色；围绕其「同色相」派生
 * 辅色(secondary)、容器(accent)、各自前景(on-*)、聚焦环(ring) 与「带品牌色相微调的中性色」
 * (muted/border)，使整套 UI 随品牌色协调，而非只改一个 primary。
 *
 * 返回 CSS 变量名 → "H S% L%" 分量串；由 applyThemePalette 内联写到根元素
 * （内联样式优先级高于 :root / .dark，故须按当前明暗重新计算并覆盖）。
 */
function deriveMaterialPalette(hex: string, dark: boolean): Record<string, string> {
  const [h, s, l] = hexToHsl(hex)
  // 主色上的前景：主色偏亮用深字，偏暗用浅字（修复如黄色主色上白字看不清）
  const onPrimary = relLuminance(hex) > 0.55 ? '220 12% 12%' : '0 0% 98%'
  const cs = (v: number) => Math.max(0, Math.min(100, Math.round(v)))

  if (dark) {
    return {
      '--primary-foreground': onPrimary,
      '--ring': `${h} ${cs(Math.min(s, 80))}% ${cs(Math.max(l, 60))}%`,
      '--accent': `${h} ${cs(Math.min(s * 0.4, 40))}% 22%`,
      '--accent-foreground': `${h} 22% 90%`,
      '--secondary': `${h} ${cs(Math.min(s * 0.3, 28))}% 18%`,
      '--secondary-foreground': `${h} 16% 92%`,
      '--muted': `${h} ${cs(Math.min(s * 0.2, 8))}% 16%`,
      '--border': `${h} ${cs(Math.min(s * 0.22, 12))}% 24%`,
    }
  }
  return {
    '--primary-foreground': onPrimary,
    '--ring': `${h} ${cs(Math.min(s, 85))}% ${cs(Math.max(Math.min(l, 55), 40))}%`,
    '--accent': `${h} ${cs(Math.min(s * 0.5, 45))}% 93%`,
    '--accent-foreground': `${h} ${cs(Math.min(s, 45))}% 24%`,
    '--secondary': `${h} ${cs(Math.min(s * 0.35, 30))}% 95%`,
    '--secondary-foreground': `${h} ${cs(Math.min(s, 35))}% 22%`,
    '--muted': `${h} ${cs(Math.min(s * 0.15, 8))}% 96%`,
    '--border': `${h} ${cs(Math.min(s * 0.18, 10))}% 90%`,
  }
}

/**
 * 读取 CSS 变量值并转换为 TinyColor 兼容的逗号格式 hsl()。
 * CSS 变量存储格式为 "H S% L%"（CSS Level 4 空格语法），
 * 而 Naive UI 内部的 TinyColor 只支持老式逗号语法 "hsl(H, S%, L%)"。
 */
function getCssColorVar(varName: string, fallback = ''): string {
  if (typeof document === 'undefined')
    return fallback
  const raw = getComputedStyle(document.documentElement).getPropertyValue(varName).trim()
  if (!raw)
    return fallback
  // "142 71% 45%" → "hsl(142, 71%, 45%)"
  const parts = raw.split(/\s+/)
  if (parts.length >= 3) {
    const [h = '', sRaw = '', lRaw = ''] = parts
    const s = sRaw.endsWith('%') ? sRaw : `${sRaw}%`
    const l = lRaw.endsWith('%') ? lRaw : `${lRaw}%`
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
    if (typeof document === 'undefined')
      return
    const { radius, cardRadius } = calcRadius(r)
    const el = document.documentElement
    el.style.setProperty('--radius', radius)
    el.style.setProperty('--radius-card', cardRadius)
  }

  /**
   * 同步主色色阶 + Material You 派生色阶到 CSS 变量（明暗自适应）。
   * 内联样式覆盖 :root/.dark，故明暗切换时也需重算重写。
   */
  function applyThemePalette(hex: string, dark: boolean, dynamic: boolean) {
    if (typeof document === 'undefined' || !hex?.startsWith('#') || hex.length < 7)
      return
    const scale = generatePrimaryScale(hex)
    const el = document.documentElement
    // 主色：始终保持用户所选精确颜色
    el.style.setProperty('--primary', hexToHslVars(hex))
    el.style.setProperty('--primary-hover', hexToHslVars(scale.hover))
    el.style.setProperty('--primary-active', hexToHslVars(scale.active))
    el.style.setProperty('--primary-suppl', hexToHslVars(scale.suppl))
    // 派生色阶：开启 Material You 时写入；关闭则移除内联覆盖，回退到 :root/.dark 静态 token
    const palette = deriveMaterialPalette(hex, dark)
    for (const name of Object.keys(palette)) {
      if (dynamic) {
        el.style.setProperty(name, palette[name]!)
      }
      else {
        el.style.removeProperty(name)
      }
    }
  }

  /** 同步字号到 CSS 变量 */
  function syncFontSize(size: number) {
    if (typeof document === 'undefined')
      return
    document.documentElement.style.setProperty('--font-size-base', `${size}px`)
  }

  watch(() => appStore.uiRadius, syncRadiusCssVars, { immediate: true })
  // 主色 / 明暗 / 动态取色开关 变化都需重算派生色阶（Material You 明暗自适应）
  watch(
    [() => appStore.themeColor, isDark, () => appStore.themeDynamicColor],
    ([hex, dark, dynamic]) => applyThemePalette(hex, dark, dynamic),
    { immediate: true },
  )
  watch(() => appStore.fontSize, syncFontSize, { immediate: true })

  const themeOverrides = computed((): GlobalThemeOverrides => {
    const { radius } = calcRadius(appStore.uiRadius)
    const scale = generatePrimaryScale(appStore.themeColor)
    const [h, s, l] = hexToHsl(appStore.themeColor)
    const primaryActive = `hsla(${h}, ${s}%, ${l}%, 0.15)`
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
        itemColorActive: primaryActive,
        itemColorActiveHover: primaryActive,
        itemColorActiveCollapsed: primaryActive,
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

  /** 解析目标模式切换后「实际呈现的明暗」（auto 取当前系统主题） */
  function resolveEffectiveDark(mode: 'light' | 'dark' | 'auto'): boolean {
    if (mode === THEME_AUTO) {
      return osTheme.value === 'dark'
    }
    return mode === 'dark'
  }

  /** 将目标模式落地到 store（auto 走跟随系统，其余直设） */
  function commitThemeMode(mode: 'light' | 'dark' | 'auto') {
    if (mode === THEME_AUTO) {
      appStore.setFollowSystemTheme()
    }
    else {
      appStore.setTheme(mode)
    }
  }

  function animateThemeTransition(mode: 'light' | 'dark' | 'auto', e?: ThemeTransitionOrigin) {
    if (appStore.themeMode === mode)
      return

    // 切换前后「实际明暗」一致（如 dark → auto 且系统也是 dark）：仅更新模式，无需扩散动画
    const willBeDark = resolveEffectiveDark(mode)
    if (willBeDark === isDark.value) {
      commitThemeMode(mode)
      return
    }

    // 无动画或浏览器不支持：直接切换，抑制 CSS 过渡一帧
    if (!appStore.themeAnimationEnabled || !('startViewTransition' in document)) {
      document.documentElement.classList.add('theme-switching')
      commitThemeMode(mode)
      requestAnimationFrame(() => document.documentElement.classList.remove('theme-switching'))
      return
    }

    const vw = window.innerWidth
    const vh = window.innerHeight
    const x = e?.clientX ?? vw / 2
    const y = e?.clientY ?? vh / 2
    // 覆盖全屏所需半径：取点击处到最远视口角的距离
    const endRadius = Math.hypot(Math.max(x, vw - x), Math.max(y, vh - y))

    // 一律用百分比而非 px：::view-transition-* 伪元素的几何空间不跟随浏览器页面缩放，
    // 写 px 会被按缩放比整体压缩（圆心偏向左上），百分比相对伪元素自身盒子解析，与缩放无关。
    const xPercent = (x / vw) * 100
    const yPercent = (y / vh) * 100
    // circle() 的百分比半径按规范以 √(w²+h²)/√2 为参照解析
    const radiusPercent = (endRadius / (Math.hypot(vw, vh) / Math.SQRT2)) * 100

    // clipPath 起止：从点击处 0 → 全屏
    const clipPath = [
      `circle(0% at ${xPercent}% ${yPercent}%)`,
      `circle(${radiusPercent}% at ${xPercent}% ${yPercent}%)`,
    ]

    // 全程抑制 CSS transition，防止截图期间元素颜色渐变产生残影
    document.documentElement.classList.add('theme-switching')

    const transition = (
      document as Document & { startViewTransition: (cb: () => Promise<void>) => VTResult }
    ).startViewTransition(async () => {
      commitThemeMode(mode)
      // 等 Vue 全部 DOM 更新完毕，浏览器才截"新主题"快照，缺少此步截图不完整
      await nextTick()
    })

    // 扩散方向按「切换后实际明暗」决定（auto 时取系统主题对应的明暗）
    const toDark = willBeDark
    transition.ready
      .then(() => {
        // 切暗色 → 旧层（亮）在上，全屏 → 0 收缩（z-index 由 html.dark CSS 类自动控制）
        // 切亮色 → 新层（亮）在上，0 → 全屏 扩散
        const anim = document.documentElement.animate(
          { clipPath: toDark ? [...clipPath].reverse() : clipPath },
          {
            duration: 450,
            easing: 'ease-in',
            pseudoElement: toDark ? '::view-transition-old(root)' : '::view-transition-new(root)',
          } as KeyframeAnimationOptions,
        )
        anim.onfinish = () => {
          // 动画结束后立即跳过剩余 ViewTransition，消除尾帧闪烁
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
  }

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
