import { describe, expect, it } from 'vitest'
import { NotificationContentFormat } from '~/types/enums'
import { notificationSummary } from './notification-text'

describe('notificationSummary 通知正文的一句话摘要', () => {
  it('markdown 去掉标题、强调、列表、引用、链接与分隔线的记号', () => {
    const markdown = [
      '## 七、定时任务调度',
      '',
      '- **存储配置**：支持本地存储，详见 [文档](https://example.com)。',
      '> 如需帮助，请联系管理员',
      '---',
      '1. 首登改密：使用 `admin` 登录',
    ].join('\n')

    expect(notificationSummary(markdown, NotificationContentFormat.Markdown)).toBe(
      '七、定时任务调度 存储配置：支持本地存储，详见 文档。 如需帮助，请联系管理员 首登改密：使用 admin 登录',
    )
  })

  it('html 取文字、解码实体，相邻块之间留空格', () => {
    expect(notificationSummary('<h2>标题</h2><p>正文&amp;说明</p>', NotificationContentFormat.Html)).toBe('标题 正文&说明')
  })

  it('纯文本只压空白，不动星号等字符', () => {
    expect(notificationSummary('a  *b*\n\nc', NotificationContentFormat.Text)).toBe('a *b* c')
  })

  it('超长时按字数截断并补省略号', () => {
    const summary = notificationSummary('一'.repeat(200), NotificationContentFormat.Text, 10)

    expect(summary).toBe(`${'一'.repeat(10)}…`)
  })

  it('空正文得到空串', () => {
    expect(notificationSummary(null, NotificationContentFormat.Markdown)).toBe('')
    expect(notificationSummary(undefined, undefined)).toBe('')
  })
})
