/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import type { ComputedRef, Ref } from 'vue'
import { usePreferredDark } from '@vueuse/core'
import { deriveBrandScale, ON_COLOR_CROSSOVER, relativeLuminance } from '@xihan-ui/tokens'
import { computed, effectScope, nextTick, watch } from 'vue'
import { THEME_AUTO } from '~/constants'
import { setPendingPreferenceOrigin, useAppStore } from '~/stores'
import { runThemeTransition } from '~/utils'
import { deriveMaterialPalette, hexToHsl, MATERIAL_PALETTE_KEYS } from './material-palette'

/**
 * 主题扩散动画起点：鼠标事件，或调用方自算的视口坐标
 * （如 change 事件本身无坐标，由控件矩形中心换算）
 */
export type ThemeTransitionOrigin = MouseEvent | { clientX: number, clientY: number }

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
 * 主色派生 hover / active / suppl 变体
 * suppl 是深色模式的补充色，提亮以保持对比度
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
 * 实心底上的前景色：亮过交叉点用深字，否则用浅字
 * 判据取组件库的公共能力，不另起一套；color 须是实际底色（色阶 600），不是用户挑的原色
 */
function onSolidFor(color: string): string {
  return relativeLuminance(color) > ON_COLOR_CROSSOVER
    ? '220 12% 12%'
    : '0 0% 100%'
}

/** 计算圆角像素值（写入 --radius / --radius-card，组件库经令牌桥读它） */
function calcRadius(r: number) {
  return {
    radius: `${Math.round(4 + r * 12)}px`,
    cardRadius: `${Math.round(6 + r * 10)}px`,
  }
}

/** 将当前 uiRadius 同步到根元素 CSS 变量，供组件库之外的自定义元素使用 */
function syncRadiusCssVars(r: number) {
  if (typeof document === 'undefined')
    return
  const { radius, cardRadius } = calcRadius(r)
  const el = document.documentElement
  el.style.setProperty('--radius', radius)
  el.style.setProperty('--radius-card', cardRadius)
}

/**
 * 主色色阶与派生色阶写到 CSS 变量
 * 内联样式盖过 :root / .dark，明暗切换时也要重写
 */
function applyThemePalette(hex: string, dark: boolean, dynamic: boolean) {
  if (typeof document === 'undefined' || !hex?.startsWith('#') || hex.length < 7)
    return
  const scale = generatePrimaryScale(hex)
  const el = document.documentElement
  // 主色：始终保持用户所选精确颜色
  el.style.setProperty('--primary', hexToHslVars(hex))
  // 品牌色阶按组件库的固定明度曲线派生：实心底取的是 600 档而不是用户挑的原色，
  // 明度定住，上面的字才能恒是白的
  const brand = deriveBrandScale(hex)
  for (const [step, value] of Object.entries(brand))
    el.style.setProperty(`--xh-color-brand-${step}`, value)
  // 主色上的前景跟着实心底那一档走，与 Material You 开关无关：它不是派生的装饰色，
  // 而是「这个底上的字读不读得清」，关掉动态取色一样要算
  el.style.setProperty('--primary-foreground', onSolidFor(brand['600']))
  el.style.setProperty('--primary-hover', hexToHslVars(scale.hover))
  el.style.setProperty('--primary-active', hexToHslVars(scale.active))
  el.style.setProperty('--primary-suppl', hexToHslVars(scale.suppl))
  // 派生色调：开启 Material You 时写入；关掉或这一档不写的变量撤掉内联值，回退到 :root / .dark 的静态配色
  const palette = deriveMaterialPalette(hex, dark)
  for (const name of MATERIAL_PALETTE_KEYS) {
    const value = dynamic
      ? palette[name]
      : undefined
    if (value)
      el.style.setProperty(name, value)
    else
      el.style.removeProperty(name)
  }
}

interface SharedTheme {
  isDark: ComputedRef<boolean>
  prefersDark: Ref<boolean>
}

const sharedThemes = new WeakMap<object, SharedTheme>()

/**
 * 主题写到文档根上，按 store 实例全局只装一份
 * useTheme 调用方有十几处，各挂监听会把色阶与圆角重写多遍；故挂在游离作用域上随应用常驻，明暗判断也共用
 */
function sharedThemeOf(appStore: ReturnType<typeof useAppStore>): SharedTheme {
  const existing = sharedThemes.get(appStore)
  if (existing)
    return existing
  const shared = effectScope(true).run(() => {
    const prefersDark = usePreferredDark()
    const isDark = computed(() => {
      if (appStore.themeMode === THEME_AUTO) {
        return prefersDark.value
      }
      return appStore.themeMode === 'dark'
    })
    watch(() => appStore.uiRadius, syncRadiusCssVars, { immediate: true })
    // 主色 / 明暗 / 动态取色开关 变化都需重算派生色阶（Material You 明暗自适应）
    watch(
      [
        () => appStore.themeColor,
        isDark,
        () => appStore.themeDynamicColor,
      ],
      ([hex, dark, dynamic]) => applyThemePalette(hex, dark, dynamic),
      { immediate: true },
    )
    return { isDark, prefersDark }
  })!
  sharedThemes.set(appStore, shared)
  return shared
}

export function useTheme() {
  const appStore = useAppStore()
  const { isDark, prefersDark } = sharedThemeOf(appStore)

  /** 解析目标模式切换后「实际呈现的明暗」（auto 取当前系统主题） */
  function resolveEffectiveDark(mode: 'light' | 'dark' | 'auto'): boolean {
    if (mode === THEME_AUTO) {
      return prefersDark.value
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

    // 把点击位置按视口百分比登记，随本次偏好上行带给其它设备，使对端从相同相对位置扩散
    setPendingPreferenceOrigin(e
      ? `${(e.clientX / window.innerWidth) * 100},${(e.clientY / window.innerHeight) * 100}`
      : null)

    void runThemeTransition({
      toDark: willBeDark,
      origin: e,
      enabled: appStore.themeAnimationEnabled,
      commit: async () => {
        commitThemeMode(mode)
        // 等 Vue 全部 DOM 更新完毕，浏览器才截「新主题」快照，缺少此步截图不完整
        await nextTick()
      },
    })
  }

  function toggleTheme() {
    appStore.toggleTheme()
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
    toggleTheme,
    toggleThemeWithTransition,
    animateThemeTransition,
    followSystem,
    setThemeColor,
  }
}
