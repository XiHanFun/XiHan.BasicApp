import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it } from 'vitest'
import { useSiteSettings } from '@/modules/settings/views/site/use-site'
import { configureRequest } from '~/request'

interface Call { method: string, url: string, body: unknown }

function profile(extra: Record<string, unknown> = {}) {
  return {
    name: null,
    subtitle: null,
    description: null,
    logo: null,
    favicon: null,
    copyrightOwner: null,
    copyrightUrl: null,
    copyrightSince: null,
    icp: null,
    icpUrl: null,
    ...extra,
  }
}

const tenantSide = {
  isPlatform: false,
  own: profile({
    name: 'Contoso',
    logo: { id: 'l-1', url: '/api/site/assets/l-1' },
  }),
  inherited: profile({ name: 'Acme Cloud', copyrightOwner: 'Acme' }),
}

function record(): Call[] {
  const calls: Call[] = []
  configureRequest({
    baseUrl: '',
    getToken: () => 't',
    fetch: (async (url: RequestInfo | URL, init?: RequestInit) => {
      const call = {
        method: init?.method ?? 'GET',
        url: String(url),
        body: init?.body
          ? JSON.parse(String(init.body))
          : undefined,
      }
      calls.push(call)
      const body = call.url.startsWith('/api/site:resolve')
        ? { profile: tenantSide.own, tenantId: null }
        : tenantSide
      return new Response(JSON.stringify(body), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      })
    }) as typeof fetch,
  })
  return calls
}

beforeEach(() => {
  setActivePinia(createPinia())
})

describe('站点设置页', () => {
  it('租户侧：自己的值进表单，没设的字段用沿用的平台值做占位', async () => {
    record()
    const editor = useSiteSettings()
    await editor.load()
    expect(editor.form.name).toBe('Contoso')
    expect(editor.form.copyrightOwner).toBe('')
    expect(editor.inherited('copyrightOwner')).toBe('Acme')
  })

  it('保存整体覆盖：空串发 null，图片原样带回现在的，存好后刷新壳层的站点设置', async () => {
    const calls = record()
    const editor = useSiteSettings()
    await editor.load()
    editor.form.subtitle = '  Hello  '
    editor.form.name = ''
    expect(await editor.save()).toBe(true)
    expect(calls.map(c => `${c.method} ${c.url.split('?')[0]}`)).toEqual(['GET /api/site', 'PUT /api/site', 'GET /api/site:resolve'])
    expect(calls[1]!.body).toEqual({
      name: null,
      subtitle: 'Hello',
      description: null,
      copyrightOwner: null,
      copyrightUrl: null,
      copyrightSince: null,
      icp: null,
      icpUrl: null,
      logo: 'l-1',
      favicon: null,
    })
  })

  it('去掉一张图片只改这一张，文字按上次保存的交回', async () => {
    const calls = record()
    const editor = useSiteSettings()
    await editor.load()
    editor.form.name = '还没保存的改动'
    expect(await editor.remove('logo')).toBe(true)
    expect(calls[1]).toMatchObject({ method: 'PUT', url: '/api/site' })
    expect(calls[1]!.body).toMatchObject({ name: 'Contoso', logo: null })
  })
})
