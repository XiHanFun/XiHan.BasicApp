/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import type { Component } from 'vue'

export type ViewLoader = () => Promise<{ default: Component }>

/**
 * 模块视图表之外的视图来源（插件页面）
 * 装路由之前按菜单准备一次，此后按组件路径给出加载器；不认得的路径给 undefined
 */
export interface ViewSource {
  /** 参数是模块视图表里找不到的组件路径；没有自己要的路径即什么都不做 */
  prepare: (components: readonly string[]) => Promise<void>
  resolve: (component: string) => ViewLoader | undefined
}

// 全部页面：src/modules/<包>/views/**/index.vue。后端菜单的 component 字段是 kebab 路径 `<包>/<路径>/index`，去掉模块段后在这张表里精确匹配，不做多候选猜测（方案 6.5）
const views = import.meta.glob<{ default: Component }>('/src/modules/*/views/**/index.vue')

// 壳层自带、不归任何能力包的页面：后端照样登记进菜单，视图在 packages/views/_core 下
const coreViews: Record<string, ViewLoader> = {
  'about/project/index': () => import('~/views/_core/about/index.vue'),
}

const sources: ViewSource[] = []

/** 登记视图来源（模块的 setup.ts 调用） */
export function registerViewSource(source: ViewSource): void {
  sources.push(source)
}

function moduleView(component: string): ViewLoader | undefined {
  const core = coreViews[component]
  if (core)
    return core
  const [module, ...rest] = component.split('/')
  return views[`/src/modules/${module}/views/${rest.join('/')}.vue`]
}

/** 装路由之前调用：把模块视图表里找不到的组件路径交给各来源准备 */
export async function prepareViewSources(components: readonly string[]): Promise<void> {
  const unresolved = [
    ...new Set(components),
  ].filter(component => !moduleView(component))
  if (unresolved.length > 0)
    await Promise.all(sources.map(source => source.prepare(unresolved)))
}

/** 组件路径解析成页面加载器：先找模块视图表，再问各来源；都找不到即抛错，不静默回退到空页 */
export function resolveView(component: string): ViewLoader {
  const loader = moduleView(component)
    ?? sources.map(source => source.resolve(component)).find(found => found !== undefined)
  if (!loader)
    throw new Error(`View '${component}' is not found in the module views or any view source`)
  return loader
}

/** 已登记的全部组件路径（门禁与菜单校验用），不含视图来源给出的 */
export function viewKeys(): string[] {
  return [
    ...Object.keys(coreViews),
    ...Object.keys(views).map(key => key.replace(/^\/src\/modules\/([^/]+)\/views\/(.+)\.vue$/, '$1/$2')),
  ]
}
