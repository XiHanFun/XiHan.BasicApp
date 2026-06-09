import { watchEffect } from 'vue'
import { useTheme } from '~/hooks'
import { useAppStore } from '~/stores'

/**
 * 将主题 / 灰度 / 色弱 / 字号 / 磨砂等偏好实时同步到 <html> 元素。
 * 在 App 根组件中调用一次即可。
 */
export function useHtmlStyle() {
  const appStore = useAppStore()
  const { isDark } = useTheme()

  watchEffect(() => {
    const el = document.documentElement

    el.classList.toggle('dark', isDark.value)
    el.classList.toggle('light', !isDark.value)

    el.style.filter = appStore.grayscaleEnabled
      ? 'grayscale(100%)'
      : appStore.colorWeaknessEnabled
        ? 'invert(0.8) hue-rotate(180deg)'
        : ''

    el.style.fontSize = `${appStore.fontSize}px`

    el.classList.toggle('frosted-glass', appStore.frostedGlassEnabled)
    if (appStore.frostedGlassEnabled) {
      el.style.setProperty('--frosted-intensity', `${appStore.frostedGlassIntensity / 100}`)
    }
    else {
      el.style.removeProperty('--frosted-intensity')
    }
  })
}
