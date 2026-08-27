import { describe, expect, it, vi } from 'vitest'
/**
 * useAvatarUrl 头像 URL 解析单元测试。
 * 职责：锁定「空值→空串」「直链不走预签名」「其余按 fileId 换取（内存缓存 + 并发去重）」
 * 「换取失败回退空串」这几条兼容约定，以及响应式版本在解析期间不把旧 URL 错绑到新 fileId。
 */
import { nextTick, ref } from 'vue'

type PresignedFn = (fileId: string) => Promise<string>

/** 把所有排队中的微任务与宏任务跑完 */
async function flush(): Promise<void> {
  await new Promise(resolve => setTimeout(resolve, 0))
}

/** 每个用例一份全新模块状态：预签名缓存与在途表都是模块级的 */
async function bootstrap(getFilePresignedUrlApi: PresignedFn) {
  vi.resetModules()
  const { registerAppContext } = await import('~/stores/app-context')
  registerAppContext({ apis: { getFilePresignedUrlApi } as never })
  return import('./useAvatarUrl')
}

describe('resolveAvatarUrl 空值与直链', () => {
  it('空值 / null / undefined / 纯空白一律返回空串，由消费方以首字母兜底', async () => {
    const api = vi.fn<PresignedFn>(async () => 'never')
    const { resolveAvatarUrl } = await bootstrap(api)

    await expect(resolveAvatarUrl('')).resolves.toBe('')
    await expect(resolveAvatarUrl(null)).resolves.toBe('')
    await expect(resolveAvatarUrl(undefined)).resolves.toBe('')
    await expect(resolveAvatarUrl('   ')).resolves.toBe('')
    expect(api).not.toHaveBeenCalled()
  })

  it('http / https 外链不走预签名，原样返回', async () => {
    const api = vi.fn<PresignedFn>(async () => 'never')
    const { resolveAvatarUrl } = await bootstrap(api)

    await expect(resolveAvatarUrl('https://cdn.example.com/a.png')).resolves.toBe('https://cdn.example.com/a.png')
    await expect(resolveAvatarUrl('http://cdn.example.com/a.png')).resolves.toBe('http://cdn.example.com/a.png')
    expect(api).not.toHaveBeenCalled()
  })

  it('data: 与 blob: 前缀同样视为直链', async () => {
    const api = vi.fn<PresignedFn>(async () => 'never')
    const { resolveAvatarUrl } = await bootstrap(api)

    await expect(resolveAvatarUrl('data:image/png;base64,AAA')).resolves.toBe('data:image/png;base64,AAA')
    await expect(resolveAvatarUrl('blob:http://x/y')).resolves.toBe('blob:http://x/y')
    expect(api).not.toHaveBeenCalled()
  })

  it('以 / 开头的后端根相对路径视为直链，不走预签名', async () => {
    const api = vi.fn<PresignedFn>(async () => 'never')
    const { resolveAvatarUrl } = await bootstrap(api)

    await expect(resolveAvatarUrl('/uploads/a.png')).resolves.toBe('/uploads/a.png')
    expect(api).not.toHaveBeenCalled()
  })

  it('直链判定不区分大小写（HTTPS:// 同样命中）', async () => {
    const api = vi.fn<PresignedFn>(async () => 'never')
    const { resolveAvatarUrl } = await bootstrap(api)

    await expect(resolveAvatarUrl('HTTPS://cdn.example.com/a.png')).resolves.toBe('HTTPS://cdn.example.com/a.png')
    expect(api).not.toHaveBeenCalled()
  })

  it('原始值前后空白被去掉后再判定', async () => {
    const api = vi.fn<PresignedFn>(async () => 'never')
    const { resolveAvatarUrl } = await bootstrap(api)

    await expect(resolveAvatarUrl('  https://cdn.example.com/a.png  ')).resolves.toBe('https://cdn.example.com/a.png')
  })
})

describe('resolveAvatarUrl 预签名换取', () => {
  it('非直链值按 fileId 调用预签名端点', async () => {
    const api = vi.fn<PresignedFn>(async () => 'https://cdn/x.png')
    const { resolveAvatarUrl } = await bootstrap(api)

    await expect(resolveAvatarUrl('file-1')).resolves.toBe('https://cdn/x.png')
    expect(api).toHaveBeenCalledWith('file-1')
  })

  it('同一 fileId 命中内存缓存，不重复请求', async () => {
    const api = vi.fn<PresignedFn>(async () => 'https://cdn/x.png')
    const { resolveAvatarUrl } = await bootstrap(api)

    await resolveAvatarUrl('file-1')
    await resolveAvatarUrl('file-1')

    expect(api).toHaveBeenCalledTimes(1)
  })

  it('并发换取同一 fileId 共享同一次在途请求', async () => {
    let resolveApi: ((url: string) => void) | null = null
    const api = vi.fn<PresignedFn>(() => new Promise((resolve) => {
      resolveApi = resolve
    }))
    const { resolveAvatarUrl } = await bootstrap(api)

    const first = resolveAvatarUrl('file-1')
    const second = resolveAvatarUrl('file-1')

    expect(api).toHaveBeenCalledTimes(1)

    resolveApi!('https://cdn/x.png')

    await expect(first).resolves.toBe('https://cdn/x.png')
    await expect(second).resolves.toBe('https://cdn/x.png')
    expect(api).toHaveBeenCalledTimes(1)
  })

  it('不同 fileId 各自换取，互不串味', async () => {
    const api = vi.fn<PresignedFn>(async (id: string) => `https://cdn/${id}.png`)
    const { resolveAvatarUrl } = await bootstrap(api)

    await expect(resolveAvatarUrl('a')).resolves.toBe('https://cdn/a.png')
    await expect(resolveAvatarUrl('b')).resolves.toBe('https://cdn/b.png')
    expect(api).toHaveBeenCalledTimes(2)
  })

  it('换取失败时回退为空串而不是抛出，交由消费方兜底', async () => {
    const api = vi.fn<PresignedFn>(async () => {
      throw new Error('403')
    })
    const { resolveAvatarUrl } = await bootstrap(api)

    await expect(resolveAvatarUrl('file-1')).resolves.toBe('')
  })

  it('换取失败不写缓存，下一次重新请求', async () => {
    const api = vi.fn<PresignedFn>()
      .mockRejectedValueOnce(new Error('403'))
      .mockResolvedValueOnce('https://cdn/x.png')
    const { resolveAvatarUrl } = await bootstrap(api)

    await resolveAvatarUrl('file-1')
    await expect(resolveAvatarUrl('file-1')).resolves.toBe('https://cdn/x.png')
    expect(api).toHaveBeenCalledTimes(2)
  })

  it('换取到空 URL 时不写缓存，下一次仍会重试', async () => {
    const api = vi.fn<PresignedFn>()
      .mockResolvedValueOnce('')
      .mockResolvedValueOnce('https://cdn/x.png')
    const { resolveAvatarUrl } = await bootstrap(api)

    await expect(resolveAvatarUrl('file-1')).resolves.toBe('')
    await expect(resolveAvatarUrl('file-1')).resolves.toBe('https://cdn/x.png')
    expect(api).toHaveBeenCalledTimes(2)
  })
})

describe('useAvatarUrl 响应式版本', () => {
  it('直链同步解析，不产生请求也不留空白帧', async () => {
    const api = vi.fn<PresignedFn>(async () => 'never')
    const { useAvatarUrl } = await bootstrap(api)

    const url = useAvatarUrl('https://cdn/a.png')

    expect(url.value).toBe('https://cdn/a.png')
    expect(api).not.toHaveBeenCalled()
  })

  it('空值同步得到空串', async () => {
    const { useAvatarUrl } = await bootstrap(async () => 'never')

    expect(useAvatarUrl(null).value).toBe('')
    expect(useAvatarUrl('  ').value).toBe('')
  })

  it('fileId 换取期间先清空，换到后再赋值', async () => {
    let resolveApi: ((url: string) => void) | null = null
    const api = vi.fn<PresignedFn>(() => new Promise((resolve) => {
      resolveApi = resolve
    }))
    const { useAvatarUrl } = await bootstrap(api)

    const url = useAvatarUrl('file-1')
    expect(url.value).toBe('')

    resolveApi!('https://cdn/x.png')
    await flush()

    expect(url.value).toBe('https://cdn/x.png')
  })

  it('源变化后旧请求的结果被丢弃，不会把上一个头像错绑到新 fileId', async () => {
    const pending = new Map<string, (url: string) => void>()
    const api = vi.fn<PresignedFn>(id => new Promise((resolve) => {
      pending.set(id, resolve)
    }))
    const { useAvatarUrl } = await bootstrap(api)

    const source = ref('file-old')
    const url = useAvatarUrl(source)

    source.value = 'file-new'
    await nextTick()

    // 旧请求后到，此时 source 已换人，结果必须被丢弃
    pending.get('file-old')?.('https://cdn/old.png')
    await flush()
    expect(url.value).toBe('')

    pending.get('file-new')?.('https://cdn/new.png')
    await flush()
    expect(url.value).toBe('https://cdn/new.png')
  })

  it('已缓存的 fileId 同步命中缓存，不再产生空白帧', async () => {
    const api = vi.fn<PresignedFn>(async () => 'https://cdn/x.png')
    const { resolveAvatarUrl, useAvatarUrl } = await bootstrap(api)
    await resolveAvatarUrl('file-1')

    const url = useAvatarUrl('file-1')

    expect(url.value).toBe('https://cdn/x.png')
    expect(api).toHaveBeenCalledTimes(1)
  })

  it('传 getter 时随依赖变化重新解析', async () => {
    const api = vi.fn<PresignedFn>(async (id: string) => `https://cdn/${id}.png`)
    const { useAvatarUrl } = await bootstrap(api)
    const flag = ref(true)

    const url = useAvatarUrl(() => (flag.value ? 'https://cdn/a.png' : 'https://cdn/b.png'))
    expect(url.value).toBe('https://cdn/a.png')

    flag.value = false
    await nextTick()

    expect(url.value).toBe('https://cdn/b.png')
  })

  it('换取失败时保持空串，供组件走 fallback-src', async () => {
    const api = vi.fn<PresignedFn>(async () => {
      throw new Error('403')
    })
    const { useAvatarUrl } = await bootstrap(api)

    const url = useAvatarUrl('file-1')
    await flush()

    expect(url.value).toBe('')
  })
})
