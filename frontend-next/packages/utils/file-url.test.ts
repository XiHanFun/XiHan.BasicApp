/**
 * packages/utils/file-url.ts 单元测试。
 *
 * 职责边界：后端根相对路径（/uploads/...）到可直链访问 URL 的解析规则，以及 BACKEND_ORIGIN
 * 从 VITE_API_BASE_URL 推导时的取源、去尾斜杠、非法值兜底三条分支。
 * BACKEND_ORIGIN 在模块求值期一次算定，故按不同 env 取值的用例走 resetModules + 动态导入。
 */
import { afterEach, describe, expect, it, vi } from 'vitest'
import { BACKEND_ORIGIN, toAbsoluteFileUrl } from './file-url'

type FileUrlModule = typeof import('./file-url')

/** 以指定的 VITE_API_BASE_URL 重新求值模块，拿到对应的 BACKEND_ORIGIN。 */
async function importWithBaseUrl(baseUrl: string | undefined): Promise<FileUrlModule> {
  vi.resetModules()
  if (baseUrl === undefined) {
    vi.stubEnv('VITE_API_BASE_URL', '')
  }
  else {
    vi.stubEnv('VITE_API_BASE_URL', baseUrl)
  }
  return import('./file-url')
}

afterEach(() => {
  vi.unstubAllEnvs()
  vi.resetModules()
})

describe('后端源 BACKEND_ORIGIN 推导', () => {
  it('测试环境未配置 VITE_API_BASE_URL 时为空串，路径保持相对以走开发代理', () => {
    expect(BACKEND_ORIGIN).toBe('')
  })

  it('配置为带 /api 路径的完整地址时只取协议+主机+端口', async () => {
    const mod = await importWithBaseUrl('https://basicappapi.xihanfun.com/api')
    expect(mod.BACKEND_ORIGIN).toBe('https://basicappapi.xihanfun.com')
  })

  it('带非默认端口的地址保留端口', async () => {
    const mod = await importWithBaseUrl('http://localhost:9708/api/v1')
    expect(mod.BACKEND_ORIGIN).toBe('http://localhost:9708')
  })

  it('首尾空白被去除后再解析', async () => {
    const mod = await importWithBaseUrl('  https://api.example.com  ')
    expect(mod.BACKEND_ORIGIN).toBe('https://api.example.com')
  })

  it('空白串等同于未配置，结果为空串', async () => {
    const mod = await importWithBaseUrl('   ')
    expect(mod.BACKEND_ORIGIN).toBe('')
  })

  it('无法被 URL 解析的值退化为去掉尾部斜杠的原串', async () => {
    const mod = await importWithBaseUrl('//api.example.com///')
    expect(mod.BACKEND_ORIGIN).toBe('//api.example.com')
  })
})

describe('toAbsoluteFileUrl 空值与直通', () => {
  it('null / undefined / 空串 / 纯空白一律返回空串', () => {
    expect(toAbsoluteFileUrl(null)).toBe('')
    expect(toAbsoluteFileUrl(undefined)).toBe('')
    expect(toAbsoluteFileUrl('')).toBe('')
    expect(toAbsoluteFileUrl('   \n')).toBe('')
  })

  it('http / https 绝对地址原样返回，不再拼接后端源', () => {
    expect(toAbsoluteFileUrl('https://cdn.example.com/a.png')).toBe('https://cdn.example.com/a.png')
    expect(toAbsoluteFileUrl('http://cdn.example.com/a.png')).toBe('http://cdn.example.com/a.png')
  })

  it('data: 与 blob: 前缀原样返回，用于本地预览地址', () => {
    expect(toAbsoluteFileUrl('data:image/png;base64,AAA')).toBe('data:image/png;base64,AAA')
    expect(toAbsoluteFileUrl('blob:http://localhost/abc')).toBe('blob:http://localhost/abc')
  })

  it('协议前缀判定大小写不敏感', () => {
    expect(toAbsoluteFileUrl('HTTPS://cdn.example.com/a.png')).toBe('HTTPS://cdn.example.com/a.png')
    expect(toAbsoluteFileUrl('Data:text/plain,x')).toBe('Data:text/plain,x')
  })

  it('两侧空白被去除后再判定与返回', () => {
    expect(toAbsoluteFileUrl('  https://cdn.example.com/a.png  ')).toBe('https://cdn.example.com/a.png')
  })
})

describe('toAbsoluteFileUrl 相对路径解析', () => {
  it('无后端源配置时根相对路径保持相对，交给 vite 的 /uploads 代理', () => {
    expect(toAbsoluteFileUrl('/uploads/2026/a.png')).toBe('/uploads/2026/a.png')
  })

  it('配置了后端源时根相对路径被拼成绝对地址', async () => {
    const mod = await importWithBaseUrl('https://basicappapi.xihanfun.com/api')
    expect(mod.toAbsoluteFileUrl('/uploads/2026/a.png')).toBe(
      'https://basicappapi.xihanfun.com/uploads/2026/a.png',
    )
  })

  it('不以斜杠开头的相对路径即便配了后端源也不拼接，避免拼出错误层级', async () => {
    const mod = await importWithBaseUrl('https://basicappapi.xihanfun.com')
    expect(mod.toAbsoluteFileUrl('uploads/a.png')).toBe('uploads/a.png')
  })

  it('中文与空格文件名不做编码，原样拼接', async () => {
    const mod = await importWithBaseUrl('https://api.example.com')
    expect(mod.toAbsoluteFileUrl('/uploads/中文 名.png')).toBe('https://api.example.com/uploads/中文 名.png')
  })

  it('单个斜杠拼成后端根地址', async () => {
    const mod = await importWithBaseUrl('https://api.example.com')
    expect(mod.toAbsoluteFileUrl('/')).toBe('https://api.example.com/')
  })

  it('协议相对地址不被识别为绝对地址，按根相对路径拼接（当前行为）', async () => {
    const mod = await importWithBaseUrl('https://api.example.com')
    expect(mod.toAbsoluteFileUrl('//cdn.example.com/a.png')).toBe(
      'https://api.example.com//cdn.example.com/a.png',
    )
  })
})
