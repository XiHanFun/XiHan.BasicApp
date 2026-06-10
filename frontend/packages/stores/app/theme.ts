import { computed, ref } from 'vue'
import {
  COLOR_WEAKNESS_ENABLED_KEY,
  DEFAULT_FONT_SIZE,
  DEFAULT_THEME,
  DEFAULT_THEME_COLOR,
  DEFAULT_UI_RADIUS,
  FONT_SIZE_KEY,
  FROSTED_GLASS_ENABLED_KEY,
  FROSTED_GLASS_INTENSITY_KEY,
  GRAYSCALE_ENABLED_KEY,
  LOADING_FIXED_COLOR_KEY,
  LOADING_NAME_KEY,
  THEME_ANIMATION_ENABLED_KEY,
  THEME_AUTO,
  THEME_COLOR_KEY,
  THEME_DYNAMIC_COLOR_KEY,
  THEME_MODE_KEY,
  TRANSITION_ENABLE_KEY,
  TRANSITION_LOADING_KEY,
  TRANSITION_NAME_KEY,
  TRANSITION_PROGRESS_KEY,
  UI_RADIUS_KEY,
  WATERMARK_ENABLED_KEY,
  WATERMARK_TEXT_KEY,
} from '~/constants'
import { LocalStorage } from '~/utils'
import { bindPersist, save } from '../helpers'

/** 主题、外观、动画、无障碍相关状态 */
export function createThemeSlice() {
  const themeMode = ref<'light' | 'dark' | 'auto'>(
    LocalStorage.get<'light' | 'dark' | 'auto'>(THEME_MODE_KEY) ?? DEFAULT_THEME,
  )
  const themeColor = ref<string>(LocalStorage.get<string>(THEME_COLOR_KEY) ?? DEFAULT_THEME_COLOR)
  // Material You 动态取色：从品牌色派生整套和谐色阶（默认开启）
  const themeDynamicColor = ref<boolean>(LocalStorage.get<boolean>(THEME_DYNAMIC_COLOR_KEY) ?? true)
  const uiRadius = ref<number>(LocalStorage.get<number>(UI_RADIUS_KEY) ?? DEFAULT_UI_RADIUS)
  const fontSize = ref<number>(LocalStorage.get<number>(FONT_SIZE_KEY) ?? DEFAULT_FONT_SIZE)

  const themeAnimationEnabled = ref<boolean>(
    LocalStorage.get<boolean>(THEME_ANIMATION_ENABLED_KEY) ?? true,
  )
  const transitionEnable = ref<boolean>(LocalStorage.get<boolean>(TRANSITION_ENABLE_KEY) ?? true)
  const transitionName = ref<string>(LocalStorage.get<string>(TRANSITION_NAME_KEY) ?? 'scale-up')
  // 页面加载动画（数学曲线加载器）
  const loadingName = ref<string>(LocalStorage.get<string>(LOADING_NAME_KEY) ?? 'lissajous-drift')
  // 加载动画固定颜色：开启后用前景色（暗色主题亮、亮色主题暗），不跟随主题色
  const loadingFixedColor = ref<boolean>(LocalStorage.get<boolean>(LOADING_FIXED_COLOR_KEY) ?? false)
  const transitionProgress = ref<boolean>(
    LocalStorage.get<boolean>(TRANSITION_PROGRESS_KEY) ?? true,
  )
  const transitionLoading = ref<boolean>(LocalStorage.get<boolean>(TRANSITION_LOADING_KEY) ?? true)

  const frostedGlassEnabled = ref<boolean>(LocalStorage.get<boolean>(FROSTED_GLASS_ENABLED_KEY) ?? false)
  const frostedGlassIntensity = ref<number>(LocalStorage.get<number>(FROSTED_GLASS_INTENSITY_KEY) ?? 10)

  const grayscaleEnabled = ref<boolean>(LocalStorage.get<boolean>(GRAYSCALE_ENABLED_KEY) ?? false)
  const colorWeaknessEnabled = ref<boolean>(
    LocalStorage.get<boolean>(COLOR_WEAKNESS_ENABLED_KEY) ?? false,
  )
  const watermarkEnabled = ref<boolean>(LocalStorage.get<boolean>(WATERMARK_ENABLED_KEY) ?? false)
  const watermarkText = ref<string>(
    LocalStorage.get<string>(WATERMARK_TEXT_KEY) ?? 'XiHan BasicApp',
  )

  const isDark = computed(() => themeMode.value === 'dark')

  bindPersist(THEME_MODE_KEY, themeMode, DEFAULT_THEME)
  bindPersist(THEME_COLOR_KEY, themeColor, DEFAULT_THEME_COLOR)
  bindPersist(THEME_DYNAMIC_COLOR_KEY, themeDynamicColor, true)
  bindPersist(UI_RADIUS_KEY, uiRadius, DEFAULT_UI_RADIUS)
  bindPersist(FONT_SIZE_KEY, fontSize, DEFAULT_FONT_SIZE)
  bindPersist(THEME_ANIMATION_ENABLED_KEY, themeAnimationEnabled, true)
  bindPersist(TRANSITION_ENABLE_KEY, transitionEnable, true)
  bindPersist(TRANSITION_NAME_KEY, transitionName, 'scale-up')
  bindPersist(LOADING_NAME_KEY, loadingName, 'lissajous-drift')
  bindPersist(LOADING_FIXED_COLOR_KEY, loadingFixedColor, false)
  bindPersist(TRANSITION_PROGRESS_KEY, transitionProgress, true)
  bindPersist(TRANSITION_LOADING_KEY, transitionLoading, true)
  bindPersist(FROSTED_GLASS_ENABLED_KEY, frostedGlassEnabled, false)
  bindPersist(FROSTED_GLASS_INTENSITY_KEY, frostedGlassIntensity, 10)
  bindPersist(GRAYSCALE_ENABLED_KEY, grayscaleEnabled, false)
  bindPersist(COLOR_WEAKNESS_ENABLED_KEY, colorWeaknessEnabled, false)
  bindPersist(WATERMARK_ENABLED_KEY, watermarkEnabled, false)
  bindPersist(WATERMARK_TEXT_KEY, watermarkText, 'XiHan BasicApp')

  function toggleTheme() {
    save(THEME_MODE_KEY, themeMode, themeMode.value === 'light' ? 'dark' : 'light')
  }
  function setTheme(mode: 'light' | 'dark' | 'auto') {
    save(THEME_MODE_KEY, themeMode, mode)
  }
  function setFollowSystemTheme() {
    save(THEME_MODE_KEY, themeMode, THEME_AUTO)
  }
  function setThemeColor(color: string) {
    save(THEME_COLOR_KEY, themeColor, color)
  }
  function setThemeDynamicColor(v: boolean) {
    save(THEME_DYNAMIC_COLOR_KEY, themeDynamicColor, v)
  }
  function setUiRadius(value: number) {
    save(UI_RADIUS_KEY, uiRadius, value)
  }
  function setFontSize(size: number) {
    save(FONT_SIZE_KEY, fontSize, size)
  }
  function setThemeAnimationEnabled(v: boolean) {
    save(THEME_ANIMATION_ENABLED_KEY, themeAnimationEnabled, v)
  }
  function setTransitionEnable(v: boolean) {
    save(TRANSITION_ENABLE_KEY, transitionEnable, v)
  }
  function setTransitionName(v: string) {
    save(TRANSITION_NAME_KEY, transitionName, v)
  }
  function setLoadingName(v: string) {
    save(LOADING_NAME_KEY, loadingName, v)
  }
  function setLoadingFixedColor(v: boolean) {
    save(LOADING_FIXED_COLOR_KEY, loadingFixedColor, v)
  }
  function setTransitionProgress(v: boolean) {
    save(TRANSITION_PROGRESS_KEY, transitionProgress, v)
  }
  function setTransitionLoading(v: boolean) {
    save(TRANSITION_LOADING_KEY, transitionLoading, v)
  }
  function setFrostedGlassEnabled(v: boolean) {
    save(FROSTED_GLASS_ENABLED_KEY, frostedGlassEnabled, v)
  }
  function setFrostedGlassIntensity(v: number) {
    save(FROSTED_GLASS_INTENSITY_KEY, frostedGlassIntensity, Math.min(100, Math.max(0, v)))
  }
  function setGrayscaleEnabled(v: boolean) {
    save(GRAYSCALE_ENABLED_KEY, grayscaleEnabled, v)
  }
  function setColorWeaknessEnabled(v: boolean) {
    save(COLOR_WEAKNESS_ENABLED_KEY, colorWeaknessEnabled, v)
  }
  function setWatermarkEnabled(v: boolean) {
    save(WATERMARK_ENABLED_KEY, watermarkEnabled, v)
  }
  function setWatermarkText(v: string) {
    save(WATERMARK_TEXT_KEY, watermarkText, v)
  }

  return {
    themeMode,
    themeColor,
    themeDynamicColor,
    uiRadius,
    fontSize,
    isDark,
    themeAnimationEnabled,
    transitionEnable,
    transitionName,
    loadingName,
    loadingFixedColor,
    transitionProgress,
    transitionLoading,
    frostedGlassEnabled,
    frostedGlassIntensity,
    grayscaleEnabled,
    colorWeaknessEnabled,
    watermarkEnabled,
    watermarkText,
    toggleTheme,
    setTheme,
    setFollowSystemTheme,
    setThemeColor,
    setThemeDynamicColor,
    setUiRadius,
    setFontSize,
    setThemeAnimationEnabled,
    setTransitionEnable,
    setTransitionName,
    setLoadingName,
    setLoadingFixedColor,
    setTransitionProgress,
    setTransitionLoading,
    setFrostedGlassEnabled,
    setFrostedGlassIntensity,
    setGrayscaleEnabled,
    setColorWeaknessEnabled,
    setWatermarkEnabled,
    setWatermarkText,
  }
}
