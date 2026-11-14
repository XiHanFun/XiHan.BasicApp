/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

/** 用户设置里记「欢迎页看到哪一版」的键（偏好场景下） */
export const WELCOME_SETTING_KEY = 'welcome'

/**
 * 各区域的问候语：欢迎页开场按它们轮流聚散
 * 每一句都用它自己的语言，不随界面语言翻译，所以不放进文案文件
 */
export const WELCOME_GREETINGS = {
  'en-US': 'Hello',
  'zh-CN': '你好',
  'zh-TW': '你好',
  'ja-JP': 'こんにちは',
  'ko-KR': '안녕하세요',
  'de-DE': 'Hallo',
  'es-ES': 'Hola',
  'fr-FR': 'Bonjour',
  'pt-BR': 'Olá',
  'ru-RU': 'Привет',
} as const

/** 开场先出现的几种语言，最后一句落在当前界面语言上 */
const WELCOME_GREETING_LEAD = [
  'en-US',
  'ja-JP',
  'fr-FR',
  'ko-KR',
  'zh-CN',
] as const

/** 开场一共几句问候 */
const WELCOME_GREETING_COUNT = 5

/**
 * 开场的问候顺序：先是别的语言，最后一句是当前界面语言
 * @param locale 当前界面语言
 * @returns 依次出现的问候语
 */
export function welcomeGreetings(locale: string): string[] {
  const own = WELCOME_GREETINGS[locale as keyof typeof WELCOME_GREETINGS] ?? WELCOME_GREETINGS['en-US']
  const lead = WELCOME_GREETING_LEAD
    .map(code => WELCOME_GREETINGS[code])
    .filter(greeting => greeting !== own)
    .slice(0, WELCOME_GREETING_COUNT - 1)
  return [...lead, own]
}
