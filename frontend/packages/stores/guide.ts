/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

/**
 * 界面引导：章节登记、看过的记录与播放
 * 职责：壳层登记界面导览，列表页与业务页按页登记自己的章节；进入页面时把没看过的章节连成一串播一遍，
 * 看过哪一章的哪一版记在服务端的用户设置里（跟着账号走，换设备不再播），之后可以从入口重看
 * 步骤只指向锚点（元素上的 data-guide），不认识具体组件：此刻不在页面上的步骤开播前就去掉
 */
import type { Placement } from '@xihan-ui/core'
import type { InjectionKey } from 'vue'
import { defineStore } from 'pinia'
import { computed, reactive, ref, shallowReactive, shallowRef } from 'vue'
import { GUIDE_SETTING_KEY, UserSettingScene } from '~/constants'
import { useAppContext } from './app-context'
import { useAuthStore } from './auth'
import { SetupStoreId } from './store-ids'

/** 步骤的目标：锚点名（元素上 data-guide 的值），或在章节范围里取元素的函数 */
export type GuideTarget = string | ((root: ParentNode) => Element | null | undefined)

/** 一步引导 */
export interface GuideStep {
  /** 同一章节里唯一 */
  id: string
  /** 高亮的目标；不给即居中（开场、收尾） */
  target?: GuideTarget
  /** 标题与说明的文案键 */
  titleKey: string
  descriptionKey: string
  /** 气泡的首选方位，缺省在目标下方 */
  placement?: Placement
  /** 另外的出现条件（如偏好关掉了对应功能） */
  when?: () => boolean
}

/** 一章引导：壳层、列表页通用或某个业务页 */
export interface GuideChapter {
  /** 记「看过」的键：shell、schema、page:<页面码> */
  key: string
  /** 内容版本：步骤有实质变化时加一，看过旧版的人会再看到一次 */
  version: number
  /** 章节名，显示在气泡顶上；返回可显示的文案 */
  title: () => string
  steps: () => readonly GuideStep[]
}

/** 页面登记的引导 */
export interface PageGuide {
  /** 页面根：页面章节的锚点只在它里面找（缓存的标签页、分屏的另一侧都不算） */
  root: () => HTMLElement | null
  /** 页面能介绍了没有（首批数据回来、表格出来）：自动播放等它 */
  ready: () => boolean
  chapters: () => readonly GuideChapter[]
}

/** 播放中的一步：目标在换步时再取一次，节点重建过也找得到 */
export interface GuideRunStep {
  id: string
  target: (() => HTMLElement | null) | null
  titleKey: string
  descriptionKey: string
  placement?: Placement
}

/** 播放中的一章：只留下此刻能指到的步骤 */
export interface GuideRunChapter {
  key: string
  version: number
  title: () => string
  steps: GuideRunStep[]
}

/** 分屏副侧渲染的页面注入 false：它不是当前页，不登记引导 */
export const PAGE_GUIDE_HOST: InjectionKey<boolean> = Symbol('page-guide-host')

/** 构建期开关：`VITE_APP_GUIDE=false` 即整个引导不出现（入口随之收起） */
export function isGuideEnabled(): boolean {
  return import.meta.env.VITE_APP_GUIDE !== 'false'
}

/**
 * 元素此刻看得见、能拿来高亮吗
 * 不在文档里、被 inert 收起、隐藏或透明、没有尺寸、整块移出视口左上方（收起的抽屉、滑出的顶栏）都不算
 * @param element 候选元素
 */
export function isGuideTargetShown(element: Element): element is HTMLElement {
  if (!(element instanceof HTMLElement) || !element.isConnected || element.closest('[inert]'))
    return false
  if (typeof element.checkVisibility === 'function'
    && !element.checkVisibility({ opacityProperty: true, visibilityProperty: true })) {
    return false
  }
  const rect = element.getBoundingClientRect()
  return rect.width > 0 && rect.height > 0 && rect.right > 0 && rect.bottom > 0
}

/**
 * 在范围里取目标：锚点名可能有几处（顶栏与悬浮组、两种布局各一颗），取看得见的那一个
 * @param target 步骤目标
 * @param root 查找范围
 * @returns 看得见的元素；没有返回 null
 */
export function resolveGuideTarget(target: GuideTarget, root: ParentNode): HTMLElement | null {
  if (typeof target === 'string') {
    for (const element of root.querySelectorAll(`[data-guide="${target}"]`)) {
      if (isGuideTargetShown(element))
        return element
    }
    return null
  }
  const element = target(root)
  return element && isGuideTargetShown(element)
    ? element
    : null
}

/**
 * 把一章落到此刻的界面上：条件不满足、目标不在的步骤去掉
 * 一个目标都指不到的章节整章不播（只剩开场白没有意义）
 * @param chapter 章节
 * @param root 锚点的查找范围
 * @returns 可播放的章节；不可播返回 null
 */
export function resolveGuideChapter(chapter: GuideChapter, root: ParentNode): GuideRunChapter | null {
  const steps: GuideRunStep[] = []
  let anchored = 0
  for (const step of chapter.steps()) {
    if (step.when && !step.when())
      continue
    const target = step.target
    if (target !== undefined && !resolveGuideTarget(target, root))
      continue
    if (target !== undefined)
      anchored += 1
    steps.push({
      id: step.id,
      target: target === undefined
        ? null
        : () => resolveGuideTarget(target, root),
      titleKey: step.titleKey,
      descriptionKey: step.descriptionKey,
      placement: step.placement,
    })
  }
  return anchored > 0
    ? {
        key: chapter.key,
        version: chapter.version,
        title: chapter.title,
        steps,
      }
    : null
}

/** 读出存下的记录：章节键 → 看过的版本；格式不对按没看过处理 */
function parseSeen(value: null | string | undefined): Record<string, number> {
  if (!value)
    return {}
  try {
    const parsed: unknown = JSON.parse(value)
    if (!parsed || typeof parsed !== 'object' || Array.isArray(parsed))
      return {}
    return Object.fromEntries(Object.entries(parsed).filter((entry): entry is [string, number] =>
      typeof entry[1] === 'number' && Number.isFinite(entry[1])))
  }
  catch {
    return {}
  }
}

export const useGuideStore = defineStore(SetupStoreId.Guide, () => {
  const auth = useAuthStore()

  /** 各章节看过的版本；null 即还没读到（读不到也是 null：这一次不自动播放，也不回写，免得盖掉服务端那份） */
  const seen = ref<null | Record<string, number>>(null)
  /** 记录属于哪个账号：同一个页面里换了人登录要重读 */
  let seenOwner: null | string = null
  let loading: null | Promise<void> = null

  /** 壳层的界面导览 */
  const shell = shallowRef<GuideChapter | null>(null)
  /** 登记了引导的页面，后登记（后激活）的在后 */
  const pages = shallowReactive<PageGuide[]>([])
  /** 暂缓自动播放的理由（登录后的公告弹窗等）：有一个在就不自动开始 */
  const holds = reactive(new Set<string>())
  /** 跳过之后到离开这一页之前不再自动播放：刚说了不看，不接着弹下一章 */
  const quiet = ref(false)

  /** 正在播放的一串章节 */
  const run = shallowRef<GuideRunChapter[] | null>(null)
  const chapterIndex = ref(0)
  const stepIndex = ref(0)

  const active = computed(() => run.value !== null)
  const chapter = computed(() => run.value?.[chapterIndex.value] ?? null)
  const lastChapter = computed(() => run.value !== null && chapterIndex.value === run.value.length - 1)
  /** 当前页（最后登记的那一页） */
  const page = computed(() => pages.at(-1) ?? null)
  /** 当前页有没有可重看的引导：页面引导入口按它显隐 */
  const pageAvailable = computed(() => isGuideEnabled() && (page.value?.chapters().length ?? 0) > 0)
  /** 有没有可重看的界面导览 */
  const shellAvailable = computed(() => isGuideEnabled() && shell.value !== null)
  const loaded = computed(() => seen.value !== null)
  const held = computed(() => holds.size > 0)

  /** 登记壳层的界面导览（布局挂载时） */
  function registerShell(next: GuideChapter): void {
    shell.value = next
  }

  /**
   * 登记一页的引导；页面停用、卸载时调返回的函数撤掉
   * 同一份登记重复调用只算一次（KeepAlive 的页面挂载与激活都会登记）
   */
  function registerPage(guide: PageGuide): () => void {
    const at = pages.indexOf(guide)
    if (at >= 0)
      pages.splice(at, 1)
    pages.push(guide)
    return () => {
      const index = pages.indexOf(guide)
      if (index >= 0)
        pages.splice(index, 1)
    }
  }

  /** 设一个暂缓自动播放的理由，或撤掉它 */
  function setHold(reason: string, on: boolean): void {
    if (on)
      holds.add(reason)
    else holds.delete(reason)
  }

  /**
   * 读「看过哪些」：进入壳层时调用
   * 模仿登录时不读：那是别人的账号，不替他看、也不替他记
   */
  function load(): Promise<void> {
    const owner = auth.session?.user?.id ?? null
    if (!isGuideEnabled() || owner === null || auth.session?.impersonatorUserName) {
      seen.value = null
      seenOwner = null
      return Promise.resolve()
    }
    if (owner === seenOwner && (seen.value !== null || loading))
      return loading ?? Promise.resolve()
    seenOwner = owner
    seen.value = null
    const pending = (async () => {
      try {
        const setting = await useAppContext().apis.userSettingApi.get({ scene: UserSettingScene.Preference, settingKey: GUIDE_SETTING_KEY })
        if (seenOwner === owner)
          seen.value = parseSeen(setting?.settingValue)
      }
      catch {
        // 读不到就不自动播放：宁可这一次不出现，也不对看过的人再播一遍；下次进入再读
        if (seenOwner === owner)
          seenOwner = null
      }
    })().finally(() => {
      if (loading === pending)
        loading = null
    })
    loading = pending
    return pending
  }

  function unseen(target: GuideChapter): boolean {
    return seen.value !== null && (seen.value[target.key] ?? 0) < target.version
  }

  /** 把一串章节记成看过；记录没读到时不写，免得用残缺的一份盖掉服务端 */
  function markSeen(chapters: readonly GuideRunChapter[]): void {
    const current = seen.value
    if (current === null)
      return
    const changed = chapters.filter(item => (current[item.key] ?? 0) < item.version)
    if (changed.length === 0)
      return
    const next = { ...current }
    for (const item of changed)
      next[item.key] = item.version
    seen.value = next
    // 记不上不拦关闭：下次进入会再播一次，由用户再跳过一遍
    void useAppContext().apis.userSettingApi.save({
      scene: UserSettingScene.Preference,
      settingKey: GUIDE_SETTING_KEY,
      settingValue: JSON.stringify(next),
    }).catch(() => {})
  }

  function start(chapters: GuideRunChapter[]): boolean {
    if (chapters.length === 0)
      return false
    chapterIndex.value = 0
    stepIndex.value = 0
    run.value = chapters
    return true
  }

  /** 当前页能播的章节；页面还没准备好或根不在时没有 */
  function pageChapters(filter: (chapter: GuideChapter) => boolean): GuideRunChapter[] {
    const current = page.value
    const root = current?.root()
    if (!current || !root || !current.ready())
      return []
    return current.chapters()
      .filter(filter)
      .map(item => resolveGuideChapter(item, root))
      .filter(item => item !== null)
  }

  /**
   * 自动播放：没看过的界面导览在前，当前页没看过的章节在后，连成一串
   * 记录没读到、模仿登录、有暂缓理由、刚跳过、已经在播都不开始
   * @returns 开始播放了没有
   */
  function autoplay(): boolean {
    if (!isGuideEnabled() || run.value || held.value || quiet.value || seen.value === null || auth.session?.impersonatorUserName)
      return false
    const queue: GuideRunChapter[] = []
    if (shell.value && unseen(shell.value)) {
      const resolved = resolveGuideChapter(shell.value, document)
      if (resolved)
        queue.push(resolved)
    }
    queue.push(...pageChapters(unseen))
    return start(queue)
  }

  /** 重看当前页的引导（不论看过没有） */
  function playPage(): boolean {
    if (!isGuideEnabled() || run.value)
      return false
    return start(pageChapters(() => true))
  }

  /** 重看界面导览 */
  function playShell(): boolean {
    if (!isGuideEnabled() || run.value || !shell.value)
      return false
    const resolved = resolveGuideChapter(shell.value, document)
    return resolved
      ? start([resolved])
      : false
  }

  function close(): void {
    run.value = null
    chapterIndex.value = 0
    stepIndex.value = 0
  }

  /** 一章播完：有下一章就接着播，没有就收尾并记成看过 */
  function advance(): void {
    const chapters = run.value
    if (!chapters)
      return
    if (chapterIndex.value < chapters.length - 1) {
      chapterIndex.value += 1
      stepIndex.value = 0
      return
    }
    markSeen(chapters)
    close()
  }

  /** 跳过或关掉：这一串都记成看过，离开这一页之前不再自动播放 */
  function dismiss(): void {
    const chapters = run.value
    if (!chapters)
      return
    markSeen(chapters)
    quiet.value = true
    close()
  }

  /** 中途离开页面（换路由、壳层卸载）：收起但不记，下次进来再播 */
  function stop(): void {
    close()
  }

  /** 离开了这一页：跳过的效力到此为止 */
  function resetQuiet(): void {
    quiet.value = false
  }

  return {
    seen,
    holds,
    quiet,
    run,
    chapterIndex,
    stepIndex,
    active,
    chapter,
    lastChapter,
    page,
    pageAvailable,
    shellAvailable,
    loaded,
    held,
    registerShell,
    registerPage,
    setHold,
    load,
    autoplay,
    playPage,
    playShell,
    advance,
    dismiss,
    stop,
    resetQuiet,
  }
})
