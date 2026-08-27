/**
 * 主题外观切片（app/theme）单元测试。
 * 职责边界：主题模式/品牌色/圆角/字号/动画/毛玻璃/无障碍/水印等外观偏好的
 * 默认值、本地还原、setter 落地与取值夹取，以及 isDark 派生的口径。
 */
import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it } from 'vitest'
import {
  DEFAULT_FONT_SIZE,
  DEFAULT_THEME,
  DEFAULT_THEME_COLOR,
  DEFAULT_UI_RADIUS,
  FROSTED_GLASS_INTENSITY_KEY,
  THEME_AUTO,
  THEME_COLOR_KEY,
  THEME_DYNAMIC_COLOR_KEY,
  THEME_MODE_KEY,
  WATERMARK_TEXT_KEY,
} from '~/constants'
import { useAppStore } from '../app'

function freshStore(): ReturnType<typeof useAppStore> {
  setActivePinia(createPinia())
  return useAppStore()
}

beforeEach(() => {
  setActivePinia(createPinia())
})

describe('默认值', () => {
  it('新装用户的外观默认值与常量表一致', () => {
    const store = freshStore()

    expect(store.themeMode).toBe(DEFAULT_THEME)
    expect(store.themeColor).toBe(DEFAULT_THEME_COLOR)
    expect(store.uiRadius).toBe(DEFAULT_UI_RADIUS)
    expect(store.fontSize).toBe(DEFAULT_FONT_SIZE)
  })

  it('动态取色、主题动画、页面过渡默认开启；毛玻璃、灰度、色弱、水印默认关闭', () => {
    const store = freshStore()

    expect(store.themeDynamicColor).toBe(true)
    expect(store.themeAnimationEnabled).toBe(true)
    expect(store.transitionEnable).toBe(true)
    expect(store.transitionProgress).toBe(true)
    expect(store.transitionLoading).toBe(true)
    expect(store.frostedGlassEnabled).toBe(false)
    expect(store.grayscaleEnabled).toBe(false)
    expect(store.colorWeaknessEnabled).toBe(false)
    expect(store.watermarkEnabled).toBe(false)
  })

  it('过渡动画名、加载动画名、毛玻璃强度、水印文案的默认取值', () => {
    const store = freshStore()

    expect(store.transitionName).toBe('scale-up')
    expect(store.loadingName).toBe('lissajous-drift')
    expect(store.loadingFixedColor).toBe(false)
    expect(store.frostedGlassIntensity).toBe(10)
    expect(store.watermarkText).toBe('XiHan BasicApp')
  })
})

describe('本地还原', () => {
  it('已保存的外观偏好在 store 初始化时被读回', () => {
    localStorage.setItem(THEME_MODE_KEY, JSON.stringify('dark'))
    localStorage.setItem(THEME_COLOR_KEY, JSON.stringify('#123456'))
    localStorage.setItem(WATERMARK_TEXT_KEY, JSON.stringify('机密'))

    const store = freshStore()

    expect(store.themeMode).toBe('dark')
    expect(store.themeColor).toBe('#123456')
    expect(store.watermarkText).toBe('机密')
  })

  it('本地值损坏时逐项回落默认值，不影响其它偏好', () => {
    localStorage.setItem(THEME_COLOR_KEY, '{坏数据')

    const store = freshStore()

    expect(store.themeColor).toBe(DEFAULT_THEME_COLOR)
    expect(store.themeMode).toBe(DEFAULT_THEME)
  })

  it('本地存的 false 不会被 ?? 误判成缺省（布尔偏好的经典坑）', () => {
    localStorage.setItem(THEME_DYNAMIC_COLOR_KEY, 'false')

    const store = freshStore()

    expect(store.themeDynamicColor).toBe(false)
  })
})

describe('主题模式切换', () => {
  it('setTheme 写入指定模式并落地', () => {
    const store = freshStore()

    store.setTheme('dark')

    expect(store.themeMode).toBe('dark')
    expect(localStorage.getItem(THEME_MODE_KEY)).toBe(JSON.stringify('dark'))
  })

  it('toggleTheme 在明暗之间来回切换', () => {
    const store = freshStore()

    store.toggleTheme()
    expect(store.themeMode).toBe('dark')

    store.toggleTheme()
    expect(store.themeMode).toBe('light')
  })

  it('当前是跟随系统时 toggleTheme 落到 light（只有 light 才切向 dark）', () => {
    const store = freshStore()
    store.setFollowSystemTheme()

    store.toggleTheme()

    expect(store.themeMode).toBe('light')
  })

  it('setFollowSystemTheme 写入 auto', () => {
    const store = freshStore()

    store.setFollowSystemTheme()

    expect(store.themeMode).toBe(THEME_AUTO)
    expect(localStorage.getItem(THEME_MODE_KEY)).toBe(JSON.stringify(THEME_AUTO))
  })

  it('isDark 只认显式 dark —— auto 模式下为 false，由样式层解析系统主题', () => {
    const store = freshStore()

    store.setTheme('dark')
    expect(store.isDark).toBe(true)

    store.setFollowSystemTheme()
    expect(store.isDark).toBe(false)

    store.setTheme('light')
    expect(store.isDark).toBe(false)
  })
})

describe('数值型偏好的取值处理', () => {
  it('毛玻璃强度被夹到 [0, 100]', () => {
    const store = freshStore()

    store.setFrostedGlassIntensity(-20)
    expect(store.frostedGlassIntensity).toBe(0)

    store.setFrostedGlassIntensity(500)
    expect(store.frostedGlassIntensity).toBe(100)

    store.setFrostedGlassIntensity(42)
    expect(store.frostedGlassIntensity).toBe(42)
    expect(localStorage.getItem(FROSTED_GLASS_INTENSITY_KEY)).toBe('42')
  })

  it('圆角与字号不做范围夹取，负数与超大值原样写入', () => {
    const store = freshStore()

    store.setUiRadius(-1)
    store.setFontSize(999)

    expect(store.uiRadius).toBe(-1)
    expect(store.fontSize).toBe(999)
  })
})

describe('字符串型偏好', () => {
  it('主题色支持任意字符串（含空串），由调用方保证合法性', () => {
    const store = freshStore()

    store.setThemeColor('')

    expect(store.themeColor).toBe('')
    expect(localStorage.getItem(THEME_COLOR_KEY)).toBe('""')
  })

  it('水印文案支持中文与 emoji，原样存取', () => {
    const store = freshStore()

    store.setWatermarkText('内部资料 🔒')

    expect(store.watermarkText).toBe('内部资料 🔒')
    expect(JSON.parse(localStorage.getItem(WATERMARK_TEXT_KEY) ?? '""')).toBe('内部资料 🔒')
  })
})

describe('开关型偏好逐个可写', () => {
  it('外观与动画类开关写入后回读一致', () => {
    const store = freshStore()

    store.setThemeDynamicColor(false)
    store.setThemeAnimationEnabled(false)
    store.setTransitionEnable(false)
    store.setTransitionProgress(false)
    store.setTransitionLoading(false)
    store.setLoadingFixedColor(true)

    expect(store.themeDynamicColor).toBe(false)
    expect(store.themeAnimationEnabled).toBe(false)
    expect(store.transitionEnable).toBe(false)
    expect(store.transitionProgress).toBe(false)
    expect(store.transitionLoading).toBe(false)
    expect(store.loadingFixedColor).toBe(true)
  })

  it('无障碍与水印类开关写入后回读一致', () => {
    const store = freshStore()

    store.setGrayscaleEnabled(true)
    store.setColorWeaknessEnabled(true)
    store.setWatermarkEnabled(true)
    store.setFrostedGlassEnabled(true)

    expect(store.grayscaleEnabled).toBe(true)
    expect(store.colorWeaknessEnabled).toBe(true)
    expect(store.watermarkEnabled).toBe(true)
    expect(store.frostedGlassEnabled).toBe(true)
  })

  it('过渡与加载动画名可自由设置', () => {
    const store = freshStore()

    store.setTransitionName('fade')
    store.setLoadingName('spinner')

    expect(store.transitionName).toBe('fade')
    expect(store.loadingName).toBe('spinner')
  })
})
