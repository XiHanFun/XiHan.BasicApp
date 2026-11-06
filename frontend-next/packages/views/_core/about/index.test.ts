/**
 * 关于页（4.x 原样搬入）：页面人人可进，后端依赖清单要服务监控的读权限，没有权限不发请求；
 * 前端依赖来自构建期注入，关键词来自 package.json；有权限却加载失败才提示后端文案
 */
import { flushPromises, mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { toast } from '~/composables'
import { i18n } from '~/locales'
import { configureRequest } from '~/request'
import { useAuthStore } from '~/stores'
import AboutPage from './index.vue'

interface Call { method: string, url: string }

function record(respond: (call: Call) => Response): Call[] {
  const calls: Call[] = []
  configureRequest({
    baseUrl: '',
    getToken: () => 't',
    fetch: (async (url: RequestInfo | URL, init?: RequestInit) => {
      const call = {
        method: init?.method ?? 'GET',
        url: String(url),
      }
      calls.push(call)
      return respond(call)
    }) as unknown as typeof fetch,
  })
  return calls
}

function json(status: number, body: object, contentType = 'application/json'): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': contentType },
  })
}

async function render(permissions: string[]) {
  vi.spyOn(useAuthStore(), 'can').mockImplementation(code => permissions.includes(code))
  const wrapper = mount(AboutPage, {
    global: {
      plugins: [i18n],
    },
  })
  await flushPromises()
  return wrapper
}

beforeEach(() => {
  setActivePinia(createPinia())
  i18n.global.locale.value = 'zh-CN'
})

describe('关于页', () => {
  it('有服务监控读权限：取后端依赖清单并列出', async () => {
    const calls = record(() => json(200, {
      items: [
        { packageName: 'SqlSugar', packageVersion: '5.1.4.0' },
        { packageName: 'Serilog', packageVersion: '4.3.0.0' },
      ],
    }))
    const wrapper = await render(['maintenance.server.read'])
    expect(calls).toEqual([
      { method: 'GET', url: '/api/server:packages' },
    ])
    const backend = wrapper.findAll('.ab-card').find(card => card.text().includes('后端生产环境依赖'))!
    expect(backend.findAll('.ab-pkg-item').map(item => [
      item.find('.ab-pkg-name').text(),
      item.find('.ab-pkg-ver').text(),
    ])).toEqual([
      ['SqlSugar', '5.1.4.0'],
      ['Serilog', '4.3.0.0'],
    ])
    expect(backend.find('.ab-pkg-count').text()).toBe('2')
  })

  it('没有权限不发请求，这一块显示暂无数据，其余照常', async () => {
    const calls = record(() => json(200, { items: [] }))
    const wrapper = await render([])
    expect(calls).toEqual([])
    const backend = wrapper.findAll('.ab-card').find(card => card.text().includes('后端生产环境依赖'))!
    expect(backend.find('.ab-empty').text()).toBe('暂无数据')
    expect(wrapper.find('.ab-hero-name').text()).toBe('XiHan.BasicApp')
    expect(wrapper.findAll('.ab-keywords .ab-empty')).toHaveLength(0)
    expect(wrapper.find('.ab-keywords').text()).toContain('#1 Vue')
    expect(wrapper.text()).not.toContain('component.about')
    expect(wrapper.text()).not.toContain('page.about')
  })

  it('前端依赖是构建期解析的真实版本，不是 catalog: 协议串', async () => {
    record(() => json(200, { items: [] }))
    const wrapper = await render([])
    const frontend = wrapper.findAll('.ab-card').find(card => card.text().includes('前端生产环境依赖'))!
    const versions = new Map(frontend.findAll('.ab-pkg-item').map(item => [
      item.find('.ab-pkg-name').text(),
      item.find('.ab-pkg-ver').text(),
    ]))
    expect(versions.get('vue')).toMatch(/^\d+\.\d+\.\d+/)
    expect([
      ...versions.values(),
    ].some(version => version.includes('catalog:'))).toBe(false)
  })

  it('有权限却加载失败：提示后端文案', async () => {
    const danger = vi.spyOn(toast, 'danger').mockImplementation(() => '')
    record(() => json(503, {
      code: 'KERNEL_UNEXPECTED_001',
      status: 503,
      detail: '服务暂不可用',
      traceId: 'x',
    }, 'application/problem+json'))
    await render(['maintenance.server.read'])
    expect(danger).toHaveBeenCalledWith('服务暂不可用')
  })
})
