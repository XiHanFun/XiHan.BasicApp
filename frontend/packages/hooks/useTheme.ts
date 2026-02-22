import type { GlobalThemeOverrides } from 'naive-ui'
import { darkTheme, lightTheme, useOsTheme } from 'naive-ui'
import { computed, nextTick, watch } from 'vue'
import { THEME_AUTO } from '~/constants'
import { useAppStore } from '~/stores'

/** 将 hex 颜色转换为 HSL 分量字符串（如 "212 100% 45%"） */
function hexToHslVars(hex: string): string {
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
      default: h = ((r - g) / d + 4) / 6
    }
  }
  return `${Math.round(h * 360)} ${Math.round(s * 100)}% ${Math.round(l * 100)}%`
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

  const themeOverrides = computed((): GlobalThemeOverrides => {
    const radius = `${Math.round(4 + appStore.uiRadius * 12)}px`
    return {
      common: {
        primaryColor: appStore.themeColor,
        primaryColorHover: appStore.themeColor,
        primaryColorPressed: appStore.themeColor,
        primaryColorSuppl: appStore.themeColor,
        borderRadius: radius,
      },
      Button: {
        borderRadiusMedium: radius,
      },
      Card: {
        borderRadius: `${Math.round(6 + appStore.uiRadius * 10)}px`,
      },
      DataTable: {
        borderRadius: `${Math.round(6 + appStore.uiRadius * 10)}px`,
      },
      // NMenu 背景始终透明，由侧边栏容器的 --sidebar-bg 统一控制背景色。
      // 对齐 vben-admin 的方式：菜单背景用设计系统 CSS 变量而非 Naive UI 自身的 cardColor。
      Menu: {
        color: 'transparent',
        colorInverted: 'transparent',
      },
    }
  })

  function toggleTheme() {
    appStore.toggleTheme()
  }

  interface VTResult { ready: Promise<void>, finished: Promise<void>, skipTransition?: () => void }

  function animateThemeTransition(mode: 'light' | 'dark', e?: MouseEvent) {
    if (appStore.themeMode === mode)
      return

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
    const clipPath = [
      `circle(0px at ${x}px ${y}px)`,
      `circle(${endRadius}px at ${x}px ${y}px)`,
    ]

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
    transition.ready.then(() => {
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
    }).catch(() => {
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
