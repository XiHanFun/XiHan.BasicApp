/**
 * packages/printing/printer-preference.ts 的本地打印机偏好键隔离。
 *
 * 职责边界：只测存储键的构造与读写归一——同一浏览器换用户、换租户、换模板都不得串用，
 * 以及空白输入的清除语义。直打时的优先级选取（显式 > 本地偏好 > 客户端默认）已在 printing.test.ts 覆盖，这里不重复。
 *
 * 用例会改 localStorage（由 vitest-setup 逐用例清理）与 pinia 用户态（每个用例新建 pinia）。
 */
import { createPinia, setActivePinia } from 'pinia'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { useUserStore } from '~/stores'
import { getPreferredPrinter, savePreferredPrinter } from './printer-preference'

const PREFIX = 'xihan:printing:preferred-printer'

function signIn(basicId: string, tenantId: null | string) {
  useUserStore().userInfo = {
    basicId,
    userName: basicId,
    tenantId,
    roles: [],
    permissions: [],
  }
}

beforeEach(() => {
  setActivePinia(createPinia())
})

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('存储键构造', () => {
  it('按「租户:用户:模板」三段构键，登录用户与租户都体现在键里', () => {
    signIn('u-1', 't-9')

    savePreferredPrinter('invoice', 'HP-1')

    expect(localStorage.getItem(`${PREFIX}:t-9:u-1:invoice`)).toBe('HP-1')
  })

  it('未登录时用 anonymous 与 platform 兜底，不会拼出 undefined 段', () => {
    savePreferredPrinter('invoice', 'HP-1')

    expect(localStorage.getItem(`${PREFIX}:platform:anonymous:invoice`)).toBe('HP-1')
  })

  it('平台运维态（无租户）与租户内是两把不同的键', () => {
    signIn('u-1', null)
    savePreferredPrinter('invoice', '平台打印机')
    signIn('u-1', 't-9')
    savePreferredPrinter('invoice', '租户打印机')

    signIn('u-1', null)
    expect(getPreferredPrinter('invoice')).toBe('平台打印机')
    signIn('u-1', 't-9')
    expect(getPreferredPrinter('invoice')).toBe('租户打印机')
  })

  it('同租户不同用户各存各的，不互相覆盖', () => {
    signIn('u-1', 't-9')
    savePreferredPrinter('invoice', '甲的打印机')
    signIn('u-2', 't-9')
    savePreferredPrinter('invoice', '乙的打印机')

    expect(getPreferredPrinter('invoice')).toBe('乙的打印机')
    signIn('u-1', 't-9')
    expect(getPreferredPrinter('invoice')).toBe('甲的打印机')
  })

  it('同用户不同模板各存各的', () => {
    signIn('u-1', 't-9')

    savePreferredPrinter('invoice', '发票机')
    savePreferredPrinter('label', '标签机')

    expect(getPreferredPrinter('invoice')).toBe('发票机')
    expect(getPreferredPrinter('label')).toBe('标签机')
  })

  it('模板编码里的冒号被转义，不会把一段拆成两段造成串键', () => {
    signIn('u-1', 't-9')

    savePreferredPrinter('a:b', '甲')
    savePreferredPrinter('a', '乙')

    expect(getPreferredPrinter('a:b')).toBe('甲')
    expect(getPreferredPrinter('a')).toBe('乙')
    expect(localStorage.getItem(`${PREFIX}:t-9:u-1:a%3Ab`)).toBe('甲')
  })

  it('中文与 emoji 模板编码可正常存取', () => {
    signIn('u-1', 't-9')

    savePreferredPrinter('发票模板🧾', '标签机')

    expect(getPreferredPrinter('发票模板🧾')).toBe('标签机')
  })

  it('模板编码首尾空白被裁掉，写与读落在同一把键上', () => {
    signIn('u-1', 't-9')

    savePreferredPrinter('  invoice  ', 'HP-1')

    expect(getPreferredPrinter('invoice')).toBe('HP-1')
    expect(localStorage.getItem(`${PREFIX}:t-9:u-1:invoice`)).toBe('HP-1')
  })
})

describe('输入校验与归一', () => {
  it('模板编码为空串或纯空白时立即报错，不写出一把残缺的键', () => {
    expect(() => savePreferredPrinter('', 'HP-1')).toThrow(/不能为空/u)
    expect(() => savePreferredPrinter('   ', 'HP-1')).toThrow(/不能为空/u)
    expect(() => getPreferredPrinter('')).toThrow(/不能为空/u)
  })

  it('打印机名首尾空白被裁掉后再存', () => {
    savePreferredPrinter('invoice', '  HP-1  ')

    expect(getPreferredPrinter('invoice')).toBe('HP-1')
  })

  it('传 null 或空白打印机名表示清除偏好', () => {
    savePreferredPrinter('invoice', 'HP-1')

    savePreferredPrinter('invoice', null)
    expect(getPreferredPrinter('invoice')).toBeNull()

    savePreferredPrinter('invoice', 'HP-1')
    savePreferredPrinter('invoice', '   ')
    expect(getPreferredPrinter('invoice')).toBeNull()
  })

  it('从未设置过时读到 null 而不是空串', () => {
    expect(getPreferredPrinter('never-set')).toBeNull()
  })

  it('历史遗留的纯空白值按未设置处理', () => {
    localStorage.setItem(`${PREFIX}:platform:anonymous:invoice`, '   ')

    expect(getPreferredPrinter('invoice')).toBeNull()
  })

  it('清除一个从未设置过的偏好是安全空转', () => {
    expect(() => savePreferredPrinter('never-set', null)).not.toThrow()
  })
})

describe('非浏览器环境', () => {
  it('没有 localStorage 时读取直接返回 null，连键都不构造（因此空编码也不报错）', () => {
    vi.stubGlobal('localStorage', undefined)

    expect(getPreferredPrinter('invoice')).toBeNull()
    expect(getPreferredPrinter('')).toBeNull()
  })

  it('没有 localStorage 时写入按「存储不可写」抛出，交给调用方提示', () => {
    vi.stubGlobal('localStorage', undefined)

    expect(() => savePreferredPrinter('invoice', 'HP-1')).toThrow()
  })

  it('替身撤销后读写回到正常状态，不污染后续用例', () => {
    vi.stubGlobal('localStorage', undefined)
    expect(getPreferredPrinter('invoice')).toBeNull()

    vi.unstubAllGlobals()
    savePreferredPrinter('invoice', 'HP-1')

    expect(getPreferredPrinter('invoice')).toBe('HP-1')
  })
})
