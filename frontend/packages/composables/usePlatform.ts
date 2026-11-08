/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

/**
 * 平台相关：Mac 检测 + 快捷键标签格式化（供命令面板、偏好快捷键等复用）
 *
 * isMac 在一次会话内恒定（userAgent 不变），故用常量而非响应式
 */

const isMac = typeof navigator !== 'undefined' && /mac/i.test(navigator.userAgent)

/** Mac 修饰键 → 符号映射 */
const MAC_KEYS: Record<string, string> = {
  ctrl: '⌘',
  control: '⌘',
  cmd: '⌘',
  command: '⌘',
  meta: '⌘',
  alt: '⌥',
  option: '⌥',
  shift: '⇧',
}

/**
 * 按平台格式化 "Ctrl+K" 这类组合
 * Mac 换成符号（⌘/⌥/⇧）并去掉 `+`（如 `⌘K`），其它平台原样返回
 */
function formatShortcut(combo: string): string {
  if (!isMac) {
    return combo
  }
  return combo
    .split('+')
    .map(part => MAC_KEYS[part.trim().toLowerCase()] ?? part.trim())
    .join('')
}

export function usePlatform() {
  return { isMac, formatShortcut }
}
