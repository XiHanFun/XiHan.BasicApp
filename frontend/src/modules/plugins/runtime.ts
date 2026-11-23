/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import type { ResponseOf } from '~/contracts'
import type { RouteDef } from '~/request'
import type { ViewSource } from '~/router'
import { defineComponent, h, shallowRef, watch } from 'vue'
import { pluginViews } from '~/contracts'
import { i18n } from '~/locales'
import { invoke } from '~/request'

export type PluginView = ResponseOf<'plugin-views.list'>['plugins'][number]
export type PluginViewPage = PluginView['pages'][number]

/** 插件页面的组件路径前缀：`plugins/<短名>/<页>/index` */
const COMPONENT_PREFIX = 'plugins/'

/** 本会话能打开的插件页面：装路由之前取一次，换语言时重取（文案随区域） */
const plugins = shallowRef<readonly PluginView[]>([])

let watchingLocale = false

/** 当前取到的插件页面描述 */
export function loadedPlugins(): readonly PluginView[] {
  return plugins.value
}

/** 按组件路径找插件页面 */
export function findPluginPage(component: string): { plugin: PluginView, page: PluginViewPage } | undefined {
  for (const plugin of plugins.value) {
    const page = plugin.pages.find(item => item.component === component)
    if (page)
      return { plugin, page }
  }
  return undefined
}

/**
 * 点号连接的文案键还原成 vue-i18n 的嵌套对象
 * 后端已核对键在短名之下、互不嵌套；`@` 是 vue-i18n 链接消息的保留字符，插件文案里的按字面量写出
 */
export function nestTexts(texts: Readonly<Record<string, string>>): Record<string, unknown> {
  const root: Record<string, unknown> = {}
  for (const [key, text] of Object.entries(texts)) {
    const segments = key.split('.')
    const leaf = segments.pop()
    if (!leaf)
      continue
    let node = root
    for (const segment of segments) {
      const next = node[segment]
      if (typeof next === 'object' && next !== null) {
        node = next as Record<string, unknown>
      }
      else {
        const created: Record<string, unknown> = {}
        node[segment] = created
        node = created
      }
    }
    node[leaf] = text.replaceAll('@', '{\'@\'}')
  }
  return root
}

async function fetchPluginViews(): Promise<void> {
  const locale = i18n.global.locale.value
  const response = await pluginViews.list({})
  for (const plugin of response.plugins)
    i18n.global.mergeLocaleMessage(locale, nestTexts(plugin.texts))
  plugins.value = response.plugins
}

/** 取插件页面描述；取不到时插件页面回落到 404 并记下原因，内置页面照常可用 */
async function refresh(): Promise<void> {
  try {
    await fetchPluginViews()
  }
  catch (error) {
    plugins.value = []
    console.error('[plugins] 插件页面描述取不到，插件页面回落到 404', error)
  }
}

/** 插件页面的路由形状：方法、模板与模板里的路由值；其余字段 GET / DELETE 进查询串，别的进请求体 */
export function pluginRoute(plugin: PluginView, operationId: string): RouteDef {
  const operation = plugin.operations.find(item => item.id === operationId)
  if (!operation)
    throw new Error(`Operation '${operationId}' of plugin '${plugin.name}' is not published over HTTP`)
  const template = operation.path.replace(/\{([^}:]+):[^}]*\}/g, '{$1}')
  return {
    method: operation.method.toUpperCase() as RouteDef['method'],
    template,
    path: [
      ...template.matchAll(/\{([^}]+)\}/g),
    ].map(match => match[1]!),
    query: [],
    success: 200,
  }
}

/** 调插件的一个操作：与内置操作走同一个请求层（认证、区域、错误解包） */
export function invokePlugin<TResponse>(plugin: PluginView, operationId: string, request: Record<string, unknown>): Promise<TResponse> {
  return invoke<Record<string, unknown>, TResponse>(operationId, pluginRoute(plugin, operationId), request)
}

/** 插件操作要的权限码；没有权限要求为 undefined */
export function pluginPermission(plugin: PluginView, operationId: string | null | undefined): string | undefined {
  if (!operationId)
    return undefined
  return plugin.operations.find(item => item.id === operationId)?.permission ?? undefined
}

/** 插件页面的视图来源：菜单里有 `plugins/` 开头、模块视图表认不得的组件路径时才取插件页面描述 */
export const pluginViewSource: ViewSource = {
  async prepare(components) {
    if (!components.some(component => component.startsWith(COMPONENT_PREFIX))) {
      plugins.value = []
      return
    }
    await refresh()
    if (!watchingLocale) {
      watchingLocale = true
      watch(() => i18n.global.locale.value, () => {
        if (plugins.value.length > 0)
          void refresh()
      })
    }
  },
  resolve(component) {
    if (!findPluginPage(component))
      return undefined
    return async () => {
      const { default: PluginSchemaView } = await import('./components/PluginSchemaView.vue')
      return {
        default: defineComponent({
          name: 'PluginPageView',
          setup: () => () => h(PluginSchemaView, { component }),
        }),
      }
    }
  },
}
