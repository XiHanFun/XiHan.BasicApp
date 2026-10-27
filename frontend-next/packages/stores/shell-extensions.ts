/**
 * 壳层扩展注册点。
 * 职责：可选功能模块把顶栏按钮、布局级浮层与布局集成钩子注册进来，
 * 布局层只渲染/调用注册项——布局不依赖任何具体功能模块，删除模块目录即扩展消失。
 */
import type { Component } from 'vue'
import { shallowReactive } from 'vue'

/** 单个壳层扩展的三类挂载物（均可省略） */
export interface ShellExtension {
  /** 顶栏工具条按钮组件（渲染在内置按钮之前） */
  headerToolbarItems?: Component[]
  /** 布局级浮层组件（抽屉/全局对话框，挂在布局根部） */
  overlays?: Component[]
  /** 布局 setup 顶层调用的集成钩子（可用组件生命周期 API） */
  integrations?: (() => void)[]
}

const extensions = shallowReactive<ShellExtension[]>([])

/**
 * 注册壳层扩展；模块 setup 钩子（app.mount 之前）调用。
 * @param extension 扩展定义。
 * @returns 无返回值。
 */
export function registerShellExtension(extension: ShellExtension): void {
  extensions.push(extension)
}

/**
 * 读取全部已注册壳层扩展；布局层渲染与集成时消费。
 * @returns 扩展列表（注册顺序）。
 */
export function useShellExtensions(): readonly ShellExtension[] {
  return extensions
}
