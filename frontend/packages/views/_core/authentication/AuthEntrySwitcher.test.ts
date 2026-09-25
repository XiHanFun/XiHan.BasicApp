/**
 * 登录方式切换器（AuthEntrySwitcher）文案。
 *
 * 职责边界：切换器用专属短标签 page.auth.entry.*，而不是借用页面标题键——
 * 登录卡片右栏固定宽，德文 / 日文等长标题会把整排挤出容器，最右侧的扫码入口被裁掉看不见。
 * 页面标题（浏览器标签页）仍用完整说法，不受影响。
 */
import { mount } from '@vue/test-utils'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { createMemoryHistory, createRouter } from 'vue-router'
import { LOGIN_PATH } from '~/constants'
import { i18n } from '~/locales'
import AuthEntrySwitcher from './AuthEntrySwitcher.vue'

const captured: { size: string } = { size: '' }

/** 标签带拿到的宽度：jsdom 不排版，由这里拨好；缺省取宽屏与平板竖屏的表单栏宽 */
const strip = vi.hoisted(() => ({ width: 460 }))

vi.mock('@vueuse/core', async (importOriginal) => {
  const { ref } = await import('vue')
  return {
    ...await importOriginal<typeof import('@vueuse/core')>(),
    useElementSize: () => ({ width: ref(strip.width), height: ref(0), stop: () => {} }),
  }
})

async function labelsFor(locale: string): Promise<string[]> {
  i18n.global.locale.value = locale as typeof i18n.global.locale.value
  const router = createRouter({ history: createMemoryHistory(), routes: [{ path: '/:p(.*)*', component: { render: () => null } }] })
  await router.push(LOGIN_PATH)
  const wrapper = mount(AuthEntrySwitcher, { props: { enabled: true }, global: { plugins: [i18n, router] } })
  captured.size = wrapper.find('[data-scope="tabs"][data-part="root"]').attributes('data-size') ?? ''
  return wrapper.findAll('[data-scope="tabs"][data-part="trigger"]').map(item => item.text())
}

afterEach(() => {
  i18n.global.locale.value = 'zh-CN'
  strip.width = 460
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

  it('档位随标签带宽度走：放得下就用 lg，手机窄栏依次降到 md、sm，四条标签仍排在一行', async () => {
    await labelsFor('zh-CN')
    expect(captured.size).toBe('lg')

    // 375 宽视口的表单栏
    strip.width = 304
    await labelsFor('zh-CN')
    expect(captured.size).toBe('md')

    // 360 宽视口的表单栏：日文四条 md 合计 294，再窄就得降档
    strip.width = 289
    await labelsFor('zh-CN')
    expect(captured.size).toBe('sm')
  })

  it('页面标题键不受影响，浏览器标签页仍显示完整说法', () => {
    i18n.global.locale.value = 'de-DE'
    expect(i18n.global.t('page.auth.qrcode_login')).toBe('QR-Code-Anmeldung')
  })
})
