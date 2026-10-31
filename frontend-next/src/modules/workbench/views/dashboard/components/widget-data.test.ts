/**
 * 工作台小组件的取数：带上查看者的时区偏移，天数变了重取，失败只记文案；序列按键取值、字节换单位、本地日解析。
 */
import { afterEach, describe, expect, it } from 'vitest'
import { nextTick, ref } from 'vue'
import { configureRequest } from '~/request'
import { getDisplayTimeZone, setDisplayTimeZone } from '~/utils'
import { byteScale, localDay } from './charts/chart-helpers'
import { lineOf, useWidgetData, viewerOffset } from './widget-data'

function ok(body: object): Response {
  return new Response(JSON.stringify(body), { status: 200, headers: { 'Content-Type': 'application/json' } })
}

function problem(status: number, code: string, detail: string): Response {
  return new Response(JSON.stringify({ status, code, detail, title: detail }), { status, headers: { 'Content-Type': 'application/problem+json' } })
}

function capture(respond: () => Response): string[] {
  const urls: string[] = []
  configureRequest({
    baseUrl: '',
    getToken: () => 't',
    fetch: (async (url: RequestInfo | URL, init?: RequestInit) => {
      urls.push(`${init?.method ?? 'GET'} ${String(url)}`)
      return respond()
    }) as unknown as typeof fetch,
  })
  return urls
}

async function settle() {
  for (let i = 0; i < 5; i++) {
    await Promise.resolve()
    await nextTick()
  }
  await new Promise(resolve => setTimeout(resolve, 0))
}

afterEach(() => {
  configureRequest({ baseUrl: '', getToken: () => null })
})

describe('useWidgetData', () => {
  it('按键与天数取数，带上查看者的时区偏移；天数变了重取', async () => {
    const urls = capture(() => ok({ series: { categories: ['2026-10-30'], lines: [{ key: 'succeeded', values: [3] }] } }))
    const days = ref(7)
    const { data, loading, error } = useWidgetData('auditing.login-trend', days)
    await settle()

    expect(urls).toEqual([`GET /api/workbench:widget-data?key=auditing.login-trend&days=7&offset=${viewerOffset()}`])
    expect(loading.value).toBe(false)
    expect(error.value).toBeNull()
    expect(lineOf(data.value, 'succeeded')).toEqual([3])
    expect(lineOf(data.value, 'failed')).toEqual([])

    days.value = 30
    await settle()
    expect(urls.at(-1)).toContain('days=30')
  })

  it('取数失败只记文案、不抛出，数据清空', async () => {
    capture(() => problem(403, 'WORKBENCH_DENIED_001', '没有查看小组件的权限。'))
    const { data, loading, error } = useWidgetData('maintenance.job-runs', 14)
    await settle()

    expect(loading.value).toBe(false)
    expect(data.value).toBeNull()
    expect(error.value).toBe('没有查看小组件的权限。')
  })
})

describe('辅助换算', () => {
  it('时区偏移是相对 UTC 的分钟数（东为正）', () => {
    // 按应用时区（显示时区）算，不看浏览器所在的时区；夏令时按那一刻算
    const original = getDisplayTimeZone()
    try {
      setDisplayTimeZone('Asia/Shanghai')
      expect(viewerOffset(new Date())).toBe(480)
      setDisplayTimeZone('America/New_York')
      expect(viewerOffset(new Date('2026-07-01T12:00:00Z'))).toBe(-240)
      expect(viewerOffset(new Date('2026-12-01T12:00:00Z'))).toBe(-300)
    }
    finally {
      setDisplayTimeZone(original)
    }
  })

  it('字节按最大的一块挑单位', () => {
    expect(byteScale(512)).toEqual({ divisor: 1, format: { style: 'unit', unit: 'byte' } })
    expect(byteScale(5 * 1024 ** 2).format.unit).toBe('megabyte')
    expect(byteScale(3 * 1024 ** 3).divisor).toBe(1024 ** 3)
    expect(byteScale(0).format.unit).toBe('byte')
  })

  it('本地日按本地零点解析', () => {
    const day = localDay('2026-10-31')
    expect([day.getFullYear(), day.getMonth(), day.getDate(), day.getHours()]).toEqual([2026, 9, 31, 0])
  })
})
