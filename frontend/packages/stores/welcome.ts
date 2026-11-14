/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

/**
 * 新版欢迎页：亮点登记点与开合状态
 * 职责：壳层与各能力包把「这一版带来了什么」按版本线登记进来；登录后首次进入一条新的版本线时欢迎层出现一次，
 * 看到哪条版本线记在服务端的用户设置里（跟着账号走，换设备不再弹），之后可以从「关于」重看
 * 欢迎层不依赖任何具体功能模块：亮点挂在模块的页面上，后端不下发那个页面即不出现
 */
import type { Component } from 'vue'
import { defineStore } from 'pinia'
import { computed, ref, shallowReactive, shallowRef } from 'vue'
import { UserSettingScene, WELCOME_SETTING_KEY } from '~/constants'
import { useAppContext } from './app-context'
import { useAuthStore } from './auth'
import { SetupStoreId } from './store-ids'

/** 一屏亮点取哪个语气的颜色（背景极光与类目标签跟着它走） */
export type WelcomeTone = 'brand' | 'info' | 'success' | 'warning' | 'danger'

/** 一屏亮点末尾的「去看看」 */
export interface WelcomeAction {
  /** 按钮文案键 */
  labelKey: string
  /** 去处：应用内路由 */
  to?: string
  /** 去处不是路由时的动作（如打开命令面板）；与 to 二选一 */
  run?: () => void
}

/** 欢迎页里的一屏亮点 */
export interface WelcomeHighlight {
  /** 归属的版本线：主版本.次版本（如 6.0） */
  release: string
  /** 同一条版本线里唯一 */
  key: string
  /** 排序，小的在前 */
  order: number
  /** 类目图标（iconify） */
  icon: string
  /** 类目、标题、说明的文案键 */
  categoryKey: string
  titleKey: string
  descriptionKey: string
  /** 要点的文案键（两三条） */
  pointKeys: readonly string[]
  /** 右侧的演示，按需加载 */
  demo: () => Promise<{ default: Component }>
  /** 色调，缺省 brand */
  tone?: WelcomeTone
  /** 所属页面：调用方可见的页面里有它才出现（可选模块关掉、或没有权限时后端不下发） */
  page?: string
  /** 可选权限码：声明后仅拥有该权限的用户可见 */
  permission?: string
  /** 另外的出现条件（如登录方式没开放通行密钥） */
  when?: () => boolean | Promise<boolean>
  /** 「去看看」；不给即这一屏没有按钮 */
  action?: WelcomeAction
}

const highlights = shallowReactive<WelcomeHighlight[]>([])

/**
 * 登记欢迎页的亮点；模块 setup 钩子（app.mount 之前）或壳层模块求值时调用
 * 同一条版本线里同名的后登记覆盖先登记
 * @param items 亮点
 */
export function registerWelcomeHighlights(...items: WelcomeHighlight[]): void {
  for (const item of items) {
    const at = highlights.findIndex(existing => existing.release === item.release && existing.key === item.key)
    if (at >= 0)
      highlights.splice(at, 1, item)
    else highlights.push(item)
  }
}

/**
 * 读取全部已登记的亮点
 * @returns 亮点列表（登记顺序）
 */
export function useWelcomeHighlights(): readonly WelcomeHighlight[] {
  return highlights
}

/**
 * 版本号所在的版本线：`6.0.0-preview.2` → `6.0`
 * @param version 版本号
 * @returns 版本线；认不出返回 null
 */
export function releaseLineOf(version: string): null | string {
  const match = /^(\d+)\.(\d+)/.exec(version.trim())
  return match
    ? `${Number(match[1])}.${Number(match[2])}`
    : null
}

/**
 * 比较两条版本线
 * @returns 负数即 a 更早，0 相同，正数即 a 更新
 */
export function compareReleaseLines(a: string, b: string): number {
  const [aMajor = 0, aMinor = 0] = a.split('.').map(Number)
  const [bMajor = 0, bMinor = 0] = b.split('.').map(Number)
  return aMajor - bMajor || aMinor - bMinor
}

/**
 * 当前版本该介绍哪条版本线：登记过亮点、又不高于当前版本的最新一条
 * @param version 当前应用版本号
 * @param releases 登记过亮点的版本线
 * @returns 版本线；没有可介绍的返回 null
 */
export function welcomeReleaseFor(version: string, releases: Iterable<string>): null | string {
  const current = releaseLineOf(version)
  if (current === null)
    return null
  let latest: null | string = null
  for (const release of releases) {
    if (compareReleaseLines(release, current) > 0)
      continue
    if (latest === null || compareReleaseLines(release, latest) > 0)
      latest = release
  }
  return latest
}

/** 存进用户设置的内容 */
interface WelcomeSeen {
  /** 已经看过的最新版本线 */
  release: string
}

function parseSeen(value: null | string | undefined): null | string {
  if (!value)
    return null
  try {
    const parsed = JSON.parse(value) as Partial<WelcomeSeen>
    return typeof parsed.release === 'string' && releaseLineOf(parsed.release) !== null
      ? parsed.release
      : null
  }
  catch {
    return null
  }
}

/** 构建期开关：`VITE_APP_WELCOME=false` 即整个欢迎页不出现（重看入口随之收起） */
export function isWelcomeEnabled(): boolean {
  return import.meta.env.VITE_APP_WELCOME !== 'false'
}

export const useWelcomeStore = defineStore(SetupStoreId.Welcome, () => {
  const auth = useAuthStore()

  /** 欢迎层是否开着 */
  const open = ref(false)
  /** 这一次是不是重看（重看不改「看过」的记录） */
  const replay = ref(false)
  /** 正在介绍的版本线 */
  const release = ref<null | string>(null)
  /** 这一次要展示的亮点（按当前用户可见的页面、权限与条件裁过） */
  const slides = shallowRef<WelcomeHighlight[]>([])
  /** 进入壳层后的那次检查做完没有：登录后的弹窗等它做完再出，免得先弹出来又被欢迎层盖住 */
  const checked = ref(false)
  let checking: null | Promise<void> = null

  /** 当前版本有没有可介绍的版本线：重看入口按它显隐 */
  const available = computed(() =>
    isWelcomeEnabled() && welcomeReleaseFor(__APP_VERSION__, highlights.map(item => item.release)) !== null)

  /** 按当前用户裁出一条版本线要展示的亮点 */
  async function resolveSlides(line: string): Promise<WelcomeHighlight[]> {
    const pages = new Set(auth.permissions?.pages ?? [])
    const candidates = highlights
      .filter(item => item.release === line)
      .filter(item => !item.page || pages.has(item.page))
      .filter(item => !item.permission || auth.can(item.permission))
    const verdicts = await Promise.all(candidates.map(async (item) => {
      if (!item.when)
        return true
      try {
        return await item.when()
      }
      catch {
        // 条件判不出来（如登录配置取不到）：这一屏不出现，欢迎页照常
        return false
      }
    }))
    return candidates
      .filter((_, index) => verdicts[index])
      .sort((a, b) => a.order - b.order)
  }

  async function present(line: string, asReplay: boolean): Promise<void> {
    slides.value = await resolveSlides(line)
    release.value = line
    replay.value = asReplay
    open.value = true
  }

  /**
   * 登录后进入壳层时调用：这条版本线还没看过就打开欢迎层
   * 模仿登录时不弹：那是别人的账号，不替他看、也不替他记；会话已经没了（正在退出）也不读
   */
  function check(): Promise<void> {
    if (checking)
      return checking
    // 同一个页面里换了人登录会再检查一次：上一个人的「检查完了」不算数
    checked.value = false
    checking = (async () => {
      if (!isWelcomeEnabled() || open.value || !auth.session || auth.session.impersonatorUserName)
        return
      const line = welcomeReleaseFor(__APP_VERSION__, highlights.map(item => item.release))
      if (line === null)
        return
      let seen: null | string
      try {
        const setting = await useAppContext().apis.userSettingApi.get({ scene: UserSettingScene.Preference, settingKey: WELCOME_SETTING_KEY })
        seen = parseSeen(setting?.settingValue)
      }
      catch {
        // 读不到「看过没有」就不弹：宁可这一次不出现，也不对着看过的人再弹一遍；下次进入再试
        return
      }
      if (seen !== null && compareReleaseLines(seen, line) >= 0)
        return
      await present(line, false)
    })().finally(() => {
      checking = null
      checked.value = true
    })
    return checking
  }

  /** 重看当前版本的欢迎页（「关于」里的入口） */
  async function show(): Promise<void> {
    const line = welcomeReleaseFor(__APP_VERSION__, highlights.map(item => item.release))
    if (!isWelcomeEnabled() || line === null || open.value)
      return
    await present(line, true)
  }

  /**
   * 关闭欢迎层（看完、跳过都走这里）；首次出现的这一次把版本线记到服务端
   * 记不上不拦关闭：下次进入会再弹一次，由用户再点一遍
   */
  function finish(): void {
    if (!open.value)
      return
    open.value = false
    const line = release.value
    if (replay.value || line === null)
      return
    void useAppContext().apis.userSettingApi.save({
      scene: UserSettingScene.Preference,
      settingKey: WELCOME_SETTING_KEY,
      settingValue: JSON.stringify({ release: line } satisfies WelcomeSeen),
    }).catch(() => {})
  }

  return {
    open,
    replay,
    release,
    slides,
    checked,
    available,
    check,
    show,
    finish,
  }
})
