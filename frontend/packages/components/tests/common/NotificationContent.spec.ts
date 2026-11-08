import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'
import NotificationContent from '~/components/common/NotificationContent.vue'
import { NotificationContentFormat } from '~/types/enums'

function html(content: string): string {
  return mount(NotificationContent, {
    props: { content, format: NotificationContentFormat.Html },
  }).find('.notif-content-html').element.innerHTML
}

describe('通知 HTML 正文的净化', () => {
  it('脚本与事件处理器被去掉，正文留着', () => {
    const rendered = html('<p onclick="alert(1)">正文</p><script>alert(2)</script><img src="x.png" onerror="alert(3)">')
    expect(rendered).toContain('正文')
    expect(rendered).toContain('x.png')
    expect(rendered).not.toMatch(/script|onclick|onerror|alert/)
  })

  it('javascript: 链接被去掉，普通链接留着', () => {
    const rendered = html('<a href="javascript:alert(1)">坏</a><a href="https://example.com/">好</a>')
    expect(rendered).not.toContain('javascript:')
    expect(rendered).toContain('https://example.com/')
  })

  it('能改全局样式的 style 与能钓鱼的表单控件被去掉', () => {
    const rendered = html('<style>body{display:none}</style><form action="https://evil.test"><input name="password"><button>登录</button></form><iframe src="https://evil.test"></iframe>')
    expect(rendered).not.toMatch(/<style|<form|<input|<button|<iframe/)
  })

  it('行内样式留着，能设颜色', () => {
    expect(html('<span style="color: red">红字</span>')).toContain('color: red')
  })

  it('纯文本不当 HTML', () => {
    const wrapper = mount(NotificationContent, {
      props: { content: '<script>alert(1)</script>', format: NotificationContentFormat.Text },
    })
    expect(wrapper.find('.notif-content-text').text()).toBe('<script>alert(1)</script>')
    expect(wrapper.find('script').exists()).toBe(false)
  })
})
