import { watchEffect } from 'vue'
import { useTheme } from '~/hooks/useTheme'
import { useAppStore } from '~/stores'

/** 换算根字号百分比时的参照基准：CSS 规范里浏览器默认的根字号（px） */
const ROOT_FONT_SIZE_BASELINE = 16

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

    // XiHan.UI 的令牌按 [data-theme] 取值（color-scheme 也在那儿声明），
    // 与上面两个类名一并写，组件库与应用样式才认同一个明暗
    el.dataset.theme = isDark.value ? 'dark' : 'light'

    el.style.filter = appStore.grayscaleEnabled
      ? 'grayscale(100%)'
      : appStore.colorWeaknessEnabled
        ? 'invert(0.8) hue-rotate(180deg)'
        : ''

    // 根字号写成相对于浏览器默认字号的百分比：偏好里的档位仍表示「浏览器默认设置下等于多少 px」，
    // 而用户在浏览器/系统里调大默认字号时，应用会按同一比例跟着放大
    el.style.fontSize = `${(appStore.fontSize / ROOT_FONT_SIZE_BASELINE) * 100}%`

    el.classList.toggle('frosted-glass', appStore.frostedGlassEnabled)
    if (appStore.frostedGlassEnabled) {
      el.style.setProperty('--frosted-intensity', `${appStore.frostedGlassIntensity / 100}`)
    }
    else {
      el.style.removeProperty('--frosted-intensity')
    }
  })
}
