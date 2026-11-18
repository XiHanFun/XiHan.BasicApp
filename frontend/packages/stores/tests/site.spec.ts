import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it } from 'vitest'
import { configureRequest } from '~/request'
import { DEFAULT_SITE_LOGO, DEFAULT_SITE_NAME, useSiteStore } from '~/stores/site'

interface Call { method: string, url: string }

function respond(body: object): Call[] {
  const calls: Call[] = []
  configureRequest({
    baseUrl: 'https://api.example.test',
    getToken: () => null,
    fetch: (async (url: RequestInfo | URL, init?: RequestInit) => {
      calls.push({
        method: init?.method ?? 'GET',
        url: String(url),
      })
      return new Response(JSON.stringify(body), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      })
    }) as typeof fetch,
  })
  return calls
}

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

beforeEach(() => {
  setActivePinia(createPinia())
  document.head.querySelectorAll('link[rel~="icon"]').forEach(link => link.remove())
})

describe('站点设置', () => {
  it('没取到之前用前端自带的名称与图标', () => {
    const site = useSiteStore()
    expect([
      site.name,
      site.logo,
      site.favicon,
      site.tenantId,
    ]).toEqual([
      DEFAULT_SITE_NAME,
      DEFAULT_SITE_LOGO,
      DEFAULT_SITE_LOGO,
      null,
    ])
  })

  it('按当前主机名取，图片地址按接口源补全，浏览器图标随之换上', async () => {
    const calls = respond({
      profile: profile({
        name: 'Contoso Portal',
        subtitle: 'One console',
        logo: { id: 'l-1', url: '/api/site/assets/l-1' },
        copyrightOwner: 'Contoso',
        icp: 'ICP-1',
      }),
      tenantId: 'contoso',
    })
    const site = useSiteStore()
    await site.load()

    expect(calls).toEqual([
      { method: 'GET', url: `https://api.example.test/api/site:resolve?host=${encodeURIComponent(window.location.host)}` },
    ])
    expect([site.name, site.subtitle, site.tenantId]).toEqual(['Contoso Portal', 'One console', 'contoso'])
    expect(site.logo).toBe('https://api.example.test/api/site/assets/l-1')
    // 没传浏览器图标即用 Logo
    expect(site.favicon).toBe(site.logo)
    expect(site.copyright).toEqual({
      owner: 'Contoso',
      url: null,
      since: null,
      icp: 'ICP-1',
      icpUrl: null,
    })
    expect(document.head.querySelector<HTMLLinkElement>('link[rel~="icon"]')?.href).toBe('https://api.example.test/api/site/assets/l-1')
  })

  it('系统名为空即回到自带的名称，换了侧再取会替换掉上一份', () => {
    const site = useSiteStore()
    site.apply({
      profile: profile({
        name: 'A',
        favicon: { id: 'f-1', url: 'https://cdn.example.test/f.ico' },
      }),
      tenantId: 'a',
    })
    expect([site.name, site.favicon]).toEqual(['A', 'https://cdn.example.test/f.ico'])

    site.apply({
      profile: profile(),
      tenantId: null,
    })
    expect([site.name, site.logo, site.tenantId]).toEqual([DEFAULT_SITE_NAME, DEFAULT_SITE_LOGO, null])
    expect(document.head.querySelector<HTMLLinkElement>('link[rel~="icon"]')?.getAttribute('href')).toBe(DEFAULT_SITE_LOGO)
  })
})
