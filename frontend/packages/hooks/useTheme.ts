import type { GlobalThemeOverrides } from 'naive-ui'
import { darkTheme, lightTheme, useOsTheme } from 'naive-ui'
import { computed, watch } from 'vue'
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
      case r: h = ((g - b) / d + (g < b ? 6 : 0)) / 6; break
      case g: h = ((b - r) / d + 2) / 6; break
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
    }
  })

  function toggleTheme() {
    appStore.toggleTheme()
  }

  function withoutColorTransition(callback: () => void) {
    document.documentElement.classList.add('theme-switching')
    callback()
    requestAnimationFrame(() => {
      document.documentElement.classList.remove('theme-switching')
    })
  }

  function animateThemeTransition(mode: 'light' | 'dark', e?: MouseEvent) {
    if (appStore.themeMode === mode) {
      return
    }
    const canAnimate = appStore.themeAnimationEnabled && 'startViewTransition' in document
    if (!canAnimate) {
      withoutColorTransition(() => {
        appStore.setTheme(mode)
      })
      return
    }

    const x = e?.clientX ?? window.innerWidth / 2
    const y = e?.clientY ?? window.innerHeight / 2
    const endRadius = Math.hypot(
      Math.max(x, window.innerWidth - x),
      Math.max(y, window.innerHeight - y),
    )

    document.documentElement.setAttribute('data-theme-to', mode)
    const transition = (
      document as Document & {
        startViewTransition: (callback: () => void) => { ready: Promise<void> }
      }
    ).startViewTransition(() => {
      withoutColorTransition(() => {
        appStore.setTheme(mode)
      })
    })

    const isDarkMode = appStore.themeMode === 'dark' || mode === 'dark'
    // 切暗色：新主题从点击处扩散展开；切亮色：旧主题从点击处收缩消退
    transition.ready.then(() => {
      if (isDarkMode) {
        document.documentElement.animate(
          { clipPath: [`circle(0px at ${x}px ${y}px)`, `circle(${endRadius}px at ${x}px ${y}px)`] },
          { duration: 420, easing: 'ease-out', pseudoElement: '::view-transition-new(root)' } as KeyframeAnimationOptions,
        )
      }
      else {
        document.documentElement.animate(
          { clipPath: [`circle(${endRadius}px at ${x}px ${y}px)`, `circle(0px at ${x}px ${y}px)`] },
          { duration: 420, easing: 'ease-in', pseudoElement: '::view-transition-old(root)' } as KeyframeAnimationOptions,
        )
      }
    }).catch(() => { /* ViewTransition 被中断，无需处理 */ })
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
