/**
 * 界面引导（guide store）单元测试
 * 职责边界：目标看不看得见的判定与取法、章节按此刻界面裁剪、「看过没有」的读写，
 * 自动播放的条件（没看过、页面就绪、暂缓、刚跳过、模仿登录、读不到、开关关着）与一串章节的推进、跳过、中止
 * 每个用例经 vi.resetModules + 动态 import 取全新的仓库；后端经 AppContext 注入替身，元素的尺寸在用例里给
 */
import type { AppContextApis } from '~/types'
import { createPinia, setActivePinia } from 'pinia'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { GUIDE_SETTING_KEY, UserSettingScene } from '~/constants'

type GuideModule = typeof import('~/stores/guide')
type GuideChapter = import('~/stores/guide').GuideChapter
type GuideStep = import('~/stores/guide').GuideStep

interface SettingDto { scene: number, settingKey: string, settingValue?: null | string }

const getApi = vi.fn<(input: { scene: number, settingKey: string }) => Promise<SettingDto>>()
const saveApi = vi.fn<(input: { scene: number, settingKey: string, settingValue?: null | string }) => Promise<SettingDto>>()

function record(value: null | Record<string, number>): SettingDto {
  return {
    scene: UserSettingScene.Preference,
    settingKey: GUIDE_SETTING_KEY,
    settingValue: value === null
      ? null
      : JSON.stringify(value),
  }
}

/** 挂一个带锚点的元素；shown 为 true 时给它尺寸（jsdom 里元素没有布局，缺省量出来是 0） */
function anchor(name: string, options: { shown?: boolean, parent?: HTMLElement } = {}): HTMLElement {
  const element = document.createElement('button')
  element.dataset.guide = name
  if (options.shown ?? true) {
    element.getBoundingClientRect = () => ({
      x: 10,
      y: 10,
      left: 10,
      top: 10,
      right: 50,
      bottom: 30,
      width: 40,
      height: 20,
      toJSON: () => ({}),
    })
  }
  ;(options.parent ?? document.body).append(element)
  return element
}

function step(id: string, target?: GuideStep['target'], overrides: Partial<GuideStep> = {}): GuideStep {
  return {
    id,
    target,
    titleKey: `${id}.title`,
    descriptionKey: `${id}.description`,
    ...overrides,
  }
}

function chapter(key: string, steps: GuideStep[], version = 1): GuideChapter {
  return {
    key,
    version,
    title: () => key,
    steps: () => steps,
  }
}

/** 取一份全新的仓库，给当前用户配上会话 */
async function setup(options: { impersonator?: string, signedOut?: boolean } = {}): Promise<GuideModule & { store: ReturnType<GuideModule['useGuideStore']> }> {
  setActivePinia(createPinia())
  const { registerAppContext } = await import('~/stores/app-context')
  registerAppContext({
    apis: {
      userSettingApi: { get: getApi, save: saveApi },
    } as unknown as AppContextApis,
  })
  const { useAuthStore } = await import('~/stores/auth')
  const auth = useAuthStore()
  auth.session = options.signedOut
    ? null
    : {
        accessToken: 'a',
        refreshToken: 'r',
        user: { id: 'u' },
        impersonatorUserName: options.impersonator,
      } as unknown as typeof auth.session
  const module = await import('~/stores/guide')
  return {
    ...module,
    store: module.useGuideStore(),
  }
}

/** 登记一页：根里挂着给定的锚点 */
function page(store: ReturnType<GuideModule['useGuideStore']>, chapters: GuideChapter[], options: { ready?: boolean } = {}) {
  const root = document.createElement('section')
  document.body.append(root)
  const state = { ready: options.ready ?? true }
  const release = store.registerPage({
    root: () => root,
    ready: () => state.ready,
    chapters: () => chapters,
  })
  return { root, state, release }
}

beforeEach(() => {
  vi.resetModules()
  localStorage.clear()
  document.body.innerHTML = ''
  getApi.mockReset()
  saveApi.mockReset()
  getApi.mockResolvedValue(record(null))
  saveApi.mockResolvedValue(record(null))
})

afterEach(() => {
  vi.unstubAllEnvs()
})

describe('目标看不看得见', () => {
  it('在文档里、有尺寸才算；收起（inert）、没尺寸、整块移出视口左上方的都不算', async () => {
    const { isGuideTargetShown } = await setup()
    const shown = anchor('a')
    const detached = document.createElement('div')
    const zero = anchor('b', { shown: false })
    const offCanvas = anchor('c')
    offCanvas.getBoundingClientRect = () => ({
      x: -300,
      y: 0,
      left: -300,
      top: 0,
      right: -20,
      bottom: 30,
      width: 280,
      height: 30,
      toJSON: () => ({}),
    })
    const stowed = document.createElement('div')
    stowed.setAttribute('inert', '')
    document.body.append(stowed)
    const insideInert = anchor('d', { parent: stowed })

    expect(isGuideTargetShown(shown)).toBe(true)
    expect(isGuideTargetShown(detached)).toBe(false)
    expect(isGuideTargetShown(zero)).toBe(false)
    expect(isGuideTargetShown(offCanvas)).toBe(false)
    expect(isGuideTargetShown(insideInert)).toBe(false)
  })

  it('同名锚点有几处时取看得见的那一个；函数目标取到看不见的也不算', async () => {
    const { resolveGuideTarget } = await setup()
    anchor('shell.theme', { shown: false })
    const visible = anchor('shell.theme')
    const hidden = anchor('x', { shown: false })

    expect(resolveGuideTarget('shell.theme', document)).toBe(visible)
    expect(resolveGuideTarget('missing', document)).toBeNull()
    expect(resolveGuideTarget(() => hidden, document)).toBeNull()
    expect(resolveGuideTarget(() => visible, document)).toBe(visible)
  })
})

describe('按此刻的界面裁剪章节', () => {
  it('目标不在、条件不满足的步骤去掉，居中的开场留着；目标在换步时重新取', async () => {
    const { resolveGuideChapter } = await setup()
    const first = anchor('one')
    anchor('two', { shown: false })
    anchor('three')

    const resolved = resolveGuideChapter(chapter('c', [
      step('intro'),
      step('one', 'one'),
      step('two', 'two'),
      step('three', 'three', {
        when: () => false,
      }),
    ]), document)

    expect(resolved?.steps.map(item => item.id)).toEqual(['intro', 'one'])
    expect(resolved?.steps[0]!.target).toBeNull()
    expect(resolved?.steps[1]!.target?.()).toBe(first)
    first.remove()
    const again = anchor('one')
    expect(resolved?.steps[1]!.target?.()).toBe(again)
  })

  it('一个目标都指不到的章节整章不播', async () => {
    const { resolveGuideChapter } = await setup()

    expect(resolveGuideChapter(chapter('c', [
      step('intro'),
      step('gone', 'gone'),
    ]), document)).toBeNull()
  })

  it('锚点只在给定的范围里找', async () => {
    const { resolveGuideChapter } = await setup()
    anchor('schema.table')
    const root = document.createElement('section')
    document.body.append(root)

    expect(resolveGuideChapter(chapter('c', [
      step('table', 'schema.table'),
    ]), root)).toBeNull()
    anchor('schema.table', { parent: root })
    expect(resolveGuideChapter(chapter('c', [
      step('table', 'schema.table'),
    ]), root)?.steps).toHaveLength(1)
  })
})

describe('读「看过哪些」', () => {
  it('读偏好场景下的 guide 设置，格式不对的项按没看过处理', async () => {
    const { store } = await setup()
    getApi.mockResolvedValue({
      ...record(null),
      settingValue: JSON.stringify({ shell: 2, schema: 'x', page: Number.NaN }),
    })

    await store.load()

    expect(getApi).toHaveBeenCalledWith({ scene: UserSettingScene.Preference, settingKey: GUIDE_SETTING_KEY })
    expect(store.seen).toEqual({ shell: 2 })
    expect(store.loaded).toBe(true)
  })

  it('同一个账号只读一次', async () => {
    const { store } = await setup()

    await Promise.all([
      store.load(),
      store.load(),
    ])
    await store.load()

    expect(getApi).toHaveBeenCalledTimes(1)
  })

  it('模仿登录、没有会话、开关关着都不读', async () => {
    const impersonating = await setup({ impersonator: 'admin' })
    await impersonating.store.load()
    expect(impersonating.store.loaded).toBe(false)

    vi.resetModules()
    const signedOut = await setup({ signedOut: true })
    await signedOut.store.load()

    vi.resetModules()
    vi.stubEnv('VITE_APP_GUIDE', 'false')
    const disabled = await setup()
    await disabled.store.load()

    expect(getApi).not.toHaveBeenCalled()
  })

  it('读不到：不自动播放，也不回写', async () => {
    const { store } = await setup()
    anchor('a')
    store.registerShell(chapter('shell', [
      step('a', 'a'),
    ]))
    getApi.mockRejectedValue(new Error('offline'))

    await store.load()

    expect(store.loaded).toBe(false)
    expect(store.autoplay()).toBe(false)
    expect(store.playShell()).toBe(true)
    store.dismiss()
    expect(saveApi).not.toHaveBeenCalled()
  })
})

describe('自动播放', () => {
  it('没看过的界面导览在前、当前页没看过的章节在后，连成一串', async () => {
    const { store } = await setup()
    anchor('shell.sidebar')
    store.registerShell(chapter('shell', [
      step('sidebar', 'shell.sidebar'),
    ]))
    const { root } = page(store, [
      chapter('schema', [
        step('table', 'schema.table'),
      ]),
      chapter('page:x', [
        step('table', 'schema.table'),
      ]),
    ])
    anchor('schema.table', { parent: root })
    getApi.mockResolvedValue(record({ 'page:x': 1 }))
    await store.load()

    expect(store.autoplay()).toBe(true)
    expect(store.run?.map(item => item.key)).toEqual(['shell', 'schema'])
    expect(store.chapter?.key).toBe('shell')
  })

  it('看过的版本不低于当前版本就不播；内容版本加了一，看过旧版的人再看一次', async () => {
    const { store } = await setup()
    anchor('a')
    store.registerShell(chapter('shell', [
      step('a', 'a'),
    ], 2))
    getApi.mockResolvedValue(record({ shell: 2 }))
    await store.load()
    expect(store.autoplay()).toBe(false)

    vi.resetModules()
    const next = await setup()
    anchor('a')
    next.store.registerShell(chapter('shell', [
      step('a', 'a'),
    ], 3))
    getApi.mockResolvedValue(record({ shell: 2 }))
    await next.store.load()
    expect(next.store.autoplay()).toBe(true)
  })

  it('页面还没就绪时只播界面导览，就绪后页面的章节再来', async () => {
    const { store } = await setup()
    const { root, state } = page(store, [
      chapter('schema', [
        step('table', 'schema.table'),
      ]),
    ], { ready: false })
    anchor('schema.table', { parent: root })
    await store.load()

    expect(store.autoplay()).toBe(false)
    state.ready = true
    expect(store.autoplay()).toBe(true)
  })

  it('有暂缓理由、刚跳过、已经在播、模仿登录时都不开始', async () => {
    const { store } = await setup()
    anchor('a')
    store.registerShell(chapter('shell', [
      step('a', 'a'),
    ]))
    await store.load()

    store.setHold('notification-gate', true)
    expect(store.autoplay()).toBe(false)
    store.setHold('notification-gate', false)

    expect(store.autoplay()).toBe(true)
    expect(store.autoplay()).toBe(false)
    store.stop()

    store.quiet = true
    expect(store.autoplay()).toBe(false)
    store.resetQuiet()
    expect(store.autoplay()).toBe(true)
    store.stop()

    const { useAuthStore } = await import('~/stores/auth')
    useAuthStore().session!.impersonatorUserName = 'admin'
    expect(store.autoplay()).toBe(false)
  })
})

describe('一串章节的推进', () => {
  it('一章播完接下一章；最后一章播完收起，整串记成看过并保留原有记录', async () => {
    const { store } = await setup()
    anchor('a')
    store.registerShell(chapter('shell', [
      step('a', 'a'),
    ]))
    const { root } = page(store, [
      chapter('schema', [
        step('t', 't'),
      ]),
    ])
    anchor('t', { parent: root })
    getApi.mockResolvedValue(record({ 'page:other': 1 }))
    await store.load()
    store.autoplay()
    store.stepIndex = 0

    store.advance()
    expect(store.chapter?.key).toBe('schema')
    expect(store.lastChapter).toBe(true)
    expect(store.stepIndex).toBe(0)
    expect(saveApi).not.toHaveBeenCalled()

    store.advance()
    expect(store.active).toBe(false)
    expect(saveApi).toHaveBeenCalledWith({
      scene: UserSettingScene.Preference,
      settingKey: GUIDE_SETTING_KEY,
      settingValue: JSON.stringify({ 'page:other': 1, 'shell': 1, 'schema': 1 }),
    })
  })

  it('跳过：整串记成看过，离开这一页之前不再自动播放', async () => {
    const { store } = await setup()
    anchor('a')
    store.registerShell(chapter('shell', [
      step('a', 'a'),
    ]))
    const { root } = page(store, [
      chapter('schema', [
        step('t', 't'),
      ]),
    ])
    anchor('t', { parent: root })
    await store.load()
    store.autoplay()

    store.dismiss()

    expect(store.active).toBe(false)
    expect(store.quiet).toBe(true)
    expect(store.seen).toEqual({ shell: 1, schema: 1 })
    expect(saveApi).toHaveBeenCalledTimes(1)
  })

  it('中途离开页面：收起但不记，下次进来再播', async () => {
    const { store } = await setup()
    anchor('a')
    store.registerShell(chapter('shell', [
      step('a', 'a'),
    ]))
    await store.load()
    store.autoplay()

    store.stop()

    expect(store.active).toBe(false)
    expect(saveApi).not.toHaveBeenCalled()
    expect(store.autoplay()).toBe(true)
  })

  it('重看不论看过没有；看过的章节再看完不重复写', async () => {
    const { store } = await setup()
    anchor('a')
    store.registerShell(chapter('shell', [
      step('a', 'a'),
    ]))
    const { root } = page(store, [
      chapter('schema', [
        step('t', 't'),
      ]),
    ])
    anchor('t', { parent: root })
    getApi.mockResolvedValue(record({ shell: 1, schema: 1 }))
    await store.load()

    expect(store.playPage()).toBe(true)
    expect(store.run?.map(item => item.key)).toEqual(['schema'])
    store.advance()
    expect(store.playShell()).toBe(true)
    store.advance()

    expect(saveApi).not.toHaveBeenCalled()
  })
})

describe('页面登记', () => {
  it('后登记的是当前页；撤掉后回到前一页；重复登记只算一次', async () => {
    const { store } = await setup()
    const first = page(store, [
      chapter('schema', []),
    ])
    const second = page(store, [
      chapter('page:b', []),
    ])

    expect(store.page?.chapters()[0]!.key).toBe('page:b')
    second.release()
    expect(store.page?.chapters()[0]!.key).toBe('schema')
    first.release()
    expect(store.page).toBeNull()
    expect(store.pageAvailable).toBe(false)
  })

  it('开关关着：入口都不出', async () => {
    vi.stubEnv('VITE_APP_GUIDE', 'false')
    const { store } = await setup()
    store.registerShell(chapter('shell', []))
    page(store, [
      chapter('schema', []),
    ])

    expect(store.pageAvailable).toBe(false)
    expect(store.shellAvailable).toBe(false)
    expect(store.playPage()).toBe(false)
  })
})
