/**
 * 登录方式切换器（AuthEntrySwitcher）文案。
 *
 * 职责边界：切换器用专属短标签 page.auth.entry.*，而不是借用页面标题键——
 * 登录卡片右栏固定宽，德文 / 日文等长标题会把整排挤出容器，最右侧的扫码入口被裁掉看不见。
 * 页面标题（浏览器标签页）仍用完整说法，不受影响。
 */
import { mount } from '@vue/test-utils'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { defineComponent, h } from 'vue'
import { createMemoryHistory, createRouter } from 'vue-router'
import { LOGIN_PATH } from '~/constants'
import { i18n } from '~/locales'
import AuthEntrySwitcher from './AuthEntrySwitcher.vue'

const captured: { options: ReadonlyArray<{ label: string }>, size: string, style: Record<string, string> | undefined } = { options: [], size: '', style: undefined }

/** 小屏开关：每个用例挂载前拨好，切换器只在挂载时读一次档位 */
const viewport = vi.hoisted(() => ({ mobile: false }))

vi.mock('~/composables', async () => {
  const { computed } = await import('vue')
  return { useIsMobile: () => ({ isMobile: computed(() => viewport.mobile) }) }
})

vi.mock('~/components', () => ({
  XSegmented: defineComponent({
    name: 'XSegmented',
    props: { options: { type: Array, required: true }, value: { type: String, default: '' }, size: { type: String, default: '' } },
    setup(props, { attrs }) {
      return () => {
        captured.options = props.options as ReadonlyArray<{ label: string }>
        captured.size = props.size
        captured.style = attrs.style as Record<string, string> | undefined
        return h('div')
      }
    },
  }),
}))

async function labelsFor(locale: string): Promise<string[]> {
  i18n.global.locale.value = locale as typeof i18n.global.locale.value
  const router = createRouter({ history: createMemoryHistory(), routes: [{ path: '/:p(.*)*', component: { render: () => null } }] })
  await router.push(LOGIN_PATH)
  mount(AuthEntrySwitcher, { global: { plugins: [i18n, router] } })
  return captured.options.map(option => option.label)
}

afterEach(() => {
  i18n.global.locale.value = 'zh-CN'
  viewport.mobile = false
})

describe('登录方式切换器文案', () => {
  it('德文使用短标签，四个入口都放得进登录卡片', async () => {
    expect(await labelsFor('de-DE')).toEqual(['Konto', 'Telefon', 'E-Mail', 'QR-Code'])
  })

  it('日文使用短标签', async () => {
    expect(await labelsFor('ja-JP')).toEqual(['アカウント', '電話番号', 'メール', 'QRコード'])
  })

  it('英文使用短标签', async () => {
    expect(await labelsFor('en-US')).toEqual(['Account', 'Phone', 'Email', 'QR Code'])
  })

  it('韩文与印地语使用短标签', async () => {
    expect(await labelsFor('ko-KR')).toEqual(['계정', '휴대폰', '이메일', 'QR 코드'])
    expect(await labelsFor('hi-IN')).toEqual(['खाता', 'फ़ोन', 'ईमेल', 'QR कोड'])
  })

  it('中文四字一组：小屏一行只放得下四个四字段', async () => {
    expect(await labelsFor('zh-CN')).toEqual(['账号登录', '手机登录', '邮箱登录', '扫码登录'])
    expect(await labelsFor('zh-TW')).toEqual(['帳號登入', '手機登入', '郵件登入', '掃碼登入'])
  })

  it('宽屏用 lg 档、不收衬距；小屏降到 md 档并把段的衬距收到 sm 档，四段仍排在一行', async () => {
    await labelsFor('zh-CN')
    expect(captured.size).toBe('lg')
    expect(captured.style).toBeUndefined()

    viewport.mobile = true
    await labelsFor('zh-CN')
    expect(captured.size).toBe('md')
    expect(captured.style).toEqual({ '--xh-segmented-item-px': 'var(--xh-control-px-sm)' })
  })

  it('页面标题键不受影响，浏览器标签页仍显示完整说法', () => {
    i18n.global.locale.value = 'de-DE'
    expect(i18n.global.t('page.auth.qrcode_login')).toBe('QR-Code-Anmeldung')
  })
})
