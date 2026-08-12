/**
 * 聊天模块启动注册。
 * 职责：把聊天壳层扩展（顶栏按钮/全局抽屉/实时集成）注册进布局的壳层扩展点。
 */
import { chatShellExtension } from '~/chat'
import { registerShellExtension } from '~/stores'

/** 注册聊天壳层扩展。 */
export default function setupChat(): void {
  registerShellExtension(chatShellExtension)
}
