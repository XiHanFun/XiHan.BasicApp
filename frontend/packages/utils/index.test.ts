/**
 * packages/utils 汇总入口契约测试。
 *
 * 职责边界：只验证 barrel 的转出范围——各子模块的导出必须能从 `~/utils` 取到，
 * 且 device-fingerprint 有意不在其中（登录页用 `import('~/utils/device-fingerprint')` 动态取用，
 * 收进 barrel 会把这段一次性的指纹采集代码拖进主包）。
 */
import { describe, expect, it } from 'vitest'
import * as utils from './index'

const SUB_MODULES = [
  () => import('./common'),
  () => import('./csv'),
  () => import('./download'),
  () => import('./file-url'),
  () => import('./navigation'),
  () => import('./request-log'),
  () => import('./storage'),
  () => import('./theme-transition'),
  () => import('./tree'),
] as const

describe('汇总入口转出范围', () => {
  it('九个子模块的运行时导出全部可从入口取到', async () => {
    const modules = await Promise.all(SUB_MODULES.map(load => load()))

    const missing: string[] = []
    for (const mod of modules) {
      for (const name of Object.keys(mod)) {
        if (!(name in utils)) {
          missing.push(name)
        }
      }
    }

    expect(missing).toEqual([])
  })

  it('子模块之间没有重名导出被静默覆盖，入口取到的是同一份实现', async () => {
    const [{ deepClone }, { listToTree }, { LocalStorage }] = await Promise.all([
      import('./common'),
      import('./tree'),
      import('./storage'),
    ])

    expect(utils.deepClone).toBe(deepClone)
    expect(utils.listToTree).toBe(listToTree)
    expect(utils.LocalStorage).toBe(LocalStorage)
  })

  it('设备指纹刻意不在入口转出，避免被打进主包', () => {
    expect('generateDeviceFingerprint' in utils).toBe(false)
  })
})
