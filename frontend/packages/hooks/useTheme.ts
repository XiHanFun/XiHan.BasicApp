import type { GlobalThemeOverrides } from 'naive-ui'
import { darkTheme, lightTheme, useOsTheme } from 'naive-ui'
import { computed } from 'vue'
import { THEME_AUTO } from '~/constants'
import { useAppStore } from '~/stores'

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

  function toggleThemeWithTransition(e?: MouseEvent) {
    const canAnimate = appStore.themeAnimationEnabled && 'startViewTransition' in document
    if (!canAnimate) {
      toggleTheme()
      return
    }

    const x = e?.clientX ?? window.innerWidth / 2
    const y = e?.clientY ?? window.innerHeight / 2
    const endRadius = Math.hypot(
      Math.max(x, window.innerWidth - x),
      Math.max(y, window.innerHeight - y),
    )

    const transition = (document as Document & {
      startViewTransition: (callback: () => void) => { ready: Promise<void> }
    }).startViewTransition(() => {
      toggleTheme()
    })

    transition.ready.then(() => {
      document.documentElement.animate(
        {
          clipPath: [`circle(0px at ${x}px ${y}px)`, `circle(${endRadius}px at ${x}px ${y}px)`],
        },
        {
          duration: 420,
          easing: 'ease-in-out',
          pseudoElement: '::view-transition-new(root)',
        } as KeyframeAnimationOptions,
      )
    })
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
    followSystem,
    setThemeColor,
  }
}
