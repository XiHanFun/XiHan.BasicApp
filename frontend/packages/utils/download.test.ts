/**
 * packages/utils/download.ts 单元测试。
 *
 * 职责边界：只验证「拿到数据后触发浏览器下载」这一步——临时 a 标签的创建/点击/移除、
 * download 属性的设置条件、以及 Blob 对象 URL 的延迟释放（漏 revoke 即内存泄漏）。
 * 不涉及任何真实网络请求。
 */
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { downloadBlob, downloadByUrl } from './download'

/** jsdom 未实现 URL.createObjectURL / revokeObjectURL，这里补桩并在用例后还原。 */
const createObjectURL = vi.fn<(blob: Blob) => string>()
const revokeObjectURL = vi.fn<(url: string) => void>()

let originalCreate: PropertyDescriptor | undefined
let originalRevoke: PropertyDescriptor | undefined
let clickedAnchors: HTMLAnchorElement[] = []
let clickSpy: ReturnType<typeof vi.spyOn> | undefined

beforeEach(() => {
  originalCreate = Object.getOwnPropertyDescriptor(URL, 'createObjectURL')
  originalRevoke = Object.getOwnPropertyDescriptor(URL, 'revokeObjectURL')
  createObjectURL.mockReset().mockReturnValue('blob:mock/object-url')
  revokeObjectURL.mockReset()
  Object.defineProperty(URL, 'createObjectURL', { value: createObjectURL, configurable: true, writable: true })
  Object.defineProperty(URL, 'revokeObjectURL', { value: revokeObjectURL, configurable: true, writable: true })

  // 点击真实 a 标签会让 jsdom 尝试导航并刷一屏 "Not implemented"，同时记录被点击的节点快照
  clickedAnchors = []
  clickSpy = vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(function mockClick(this: HTMLAnchorElement) {
    clickedAnchors.push(this)
  })
})

afterEach(() => {
  clickSpy?.mockRestore()
  clickSpy = undefined
  if (originalCreate) {
    Object.defineProperty(URL, 'createObjectURL', originalCreate)
  }
  else {
    Reflect.deleteProperty(URL, 'createObjectURL')
  }
  if (originalRevoke) {
    Object.defineProperty(URL, 'revokeObjectURL', originalRevoke)
  }
  else {
    Reflect.deleteProperty(URL, 'revokeObjectURL')
  }
  vi.useRealTimers()
})

function lastAnchor(): HTMLAnchorElement {
  const anchor = clickedAnchors.at(-1)
  if (!anchor) {
    throw new Error('没有任何锚点被点击')
  }
  return anchor
}

describe('downloadBlob', () => {
  it('用传入的 Blob 生成对象 URL 并点击一次锚点', () => {
    vi.useFakeTimers()
    const blob = new Blob(['内容'], { type: 'text/plain' })

    downloadBlob(blob, '报表.csv')

    expect(createObjectURL).toHaveBeenCalledTimes(1)
    expect(createObjectURL).toHaveBeenCalledWith(blob)
    expect(clickedAnchors).toHaveLength(1)
    expect(lastAnchor().href).toBe('blob:mock/object-url')
  })

  it('文件名写入 download 属性，浏览器据此命名而非取 URL 末段', () => {
    vi.useFakeTimers()
    downloadBlob(new Blob(['x']), '中文名 带空格.xlsx')

    expect(lastAnchor().getAttribute('download')).toBe('中文名 带空格.xlsx')
  })

  it('锚点点击后立即从 body 移除，不在页面上留下残留节点', () => {
    vi.useFakeTimers()
    downloadBlob(new Blob(['x']), 'a.txt')

    expect(document.querySelectorAll('a')).toHaveLength(0)
  })

  it('锚点以 display:none 插入，下载过程对用户不可见', () => {
    vi.useFakeTimers()
    downloadBlob(new Blob(['x']), 'a.txt')

    expect(lastAnchor().style.display).toBe('none')
  })

  it('对象 URL 延迟一秒后才释放，同步释放会中断部分浏览器的下载', () => {
    vi.useFakeTimers()
    downloadBlob(new Blob(['x']), 'a.txt')

    expect(revokeObjectURL).not.toHaveBeenCalled()
    vi.advanceTimersByTime(999)
    expect(revokeObjectURL).not.toHaveBeenCalled()

    vi.advanceTimersByTime(1)
    expect(revokeObjectURL).toHaveBeenCalledTimes(1)
    expect(revokeObjectURL).toHaveBeenCalledWith('blob:mock/object-url')
  })

  it('多次下载各自创建并各自释放对象 URL，不会漏掉任何一个', () => {
    vi.useFakeTimers()
    createObjectURL.mockReturnValueOnce('blob:one').mockReturnValueOnce('blob:two')

    downloadBlob(new Blob(['1']), '1.txt')
    downloadBlob(new Blob(['2']), '2.txt')
    vi.advanceTimersByTime(1000)

    expect(revokeObjectURL.mock.calls.map(([url]) => url)).toEqual(['blob:one', 'blob:two'])
  })

  it('文件名为空串时不写 download 属性，仍会创建并释放对象 URL', () => {
    vi.useFakeTimers()
    downloadBlob(new Blob(['x']), '')

    expect(lastAnchor().hasAttribute('download')).toBe(false)
    vi.advanceTimersByTime(1000)
    expect(revokeObjectURL).toHaveBeenCalledTimes(1)
  })

  it('空 Blob 同样走完整流程，不做大小判空短路', () => {
    vi.useFakeTimers()
    downloadBlob(new Blob([]), 'empty.bin')

    expect(clickedAnchors).toHaveLength(1)
  })
})

describe('downloadByUrl', () => {
  it('直链下载不经过对象 URL，也不需要释放', () => {
    downloadByUrl('https://example.com/a.pdf', 'a.pdf')

    expect(createObjectURL).not.toHaveBeenCalled()
    expect(revokeObjectURL).not.toHaveBeenCalled()
    expect(lastAnchor().href).toBe('https://example.com/a.pdf')
    expect(lastAnchor().getAttribute('download')).toBe('a.pdf')
  })

  it('省略文件名时不设置 download 属性，交给浏览器按响应头或 URL 决定', () => {
    downloadByUrl('https://example.com/a.pdf')

    expect(lastAnchor().hasAttribute('download')).toBe(false)
  })

  it('相对路径直链被锚点解析为当前源下的绝对地址', () => {
    downloadByUrl('/uploads/a.png', 'a.png')

    expect(lastAnchor().href).toBe(`${window.location.origin}/uploads/a.png`)
  })

  it('每次调用都新建锚点并在点击后移除，不复用同一节点', () => {
    downloadByUrl('https://example.com/1.pdf')
    downloadByUrl('https://example.com/2.pdf')

    expect(clickedAnchors).toHaveLength(2)
    expect(clickedAnchors[0]).not.toBe(clickedAnchors[1])
    expect(document.querySelectorAll('a')).toHaveLength(0)
  })
})
