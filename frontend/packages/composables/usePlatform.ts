/**
 * 平台相关：Mac 检测 + 快捷键标签格式化（供命令面板、偏好快捷键等复用）。
 *
 * isMac 在一次会话内恒定（userAgent 不变），故用常量而非响应式。
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
 * 把 "Ctrl+K" 这类组合按平台格式化：
 * - Mac：用符号（⌘/⌥/⇧）且去掉 `+` 分隔（如 `⌘K`、`⌥L`），符合 Mac 习惯；
 * - 其它平台：原样返回（如 `Ctrl+K`）。
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
