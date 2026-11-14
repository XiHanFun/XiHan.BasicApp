/**
 * 欢迎页问候顺序（welcomeGreetings）单元测试
 * 职责边界：一共五句、互不重复、最后一句落在当前界面语言上；认不出的语言按英语收尾
 */
import { describe, expect, it } from 'vitest'
import { WELCOME_GREETINGS, welcomeGreetings } from '~/constants/welcome'

describe('问候顺序', () => {
  it.each(Object.keys(WELCOME_GREETINGS))('%s：五句互不重复，最后一句是它自己的问候', (locale) => {
    const greetings = welcomeGreetings(locale)

    expect(greetings).toHaveLength(5)
    expect(new Set(greetings).size).toBe(5)
    expect(greetings.at(-1)).toBe(WELCOME_GREETINGS[locale as keyof typeof WELCOME_GREETINGS])
  })

  it('繁体中文与简体中文的问候同字：前面不再出现一遍', () => {
    const greetings = welcomeGreetings('zh-TW')

    expect(greetings.filter(greeting => greeting === '你好')).toHaveLength(1)
  })

  it('认不出的语言按英语收尾', () => {
    expect(welcomeGreetings('xx-XX').at(-1)).toBe('Hello')
  })
})
