import type { Component } from 'vue'

type ViewLoader = () => Promise<{ default: Component }>

// 全部页面：src/modules/<包>/views/**/index.vue。后端菜单的 component 字段是 kebab 路径 `<包>/<路径>/index`，去掉模块段后在这张表里精确匹配，不做多候选猜测（方案 6.5）
const views = import.meta.glob<{ default: Component }>('/src/modules/*/views/**/index.vue')

/** 把组件路径（如 `identity/users/index`）解析成页面加载器；找不到即抛错，而不是静默回退到空页。 */
export function resolveView(component: string): ViewLoader {
  const [module, ...rest] = component.split('/')
  const key = `/src/modules/${module}/views/${rest.join('/')}.vue`
  const loader = views[key]
  if (!loader)
    throw new Error(`View '${component}' is not found at ${key}`)
  return loader
}

/** 已登记的全部组件路径（门禁与菜单校验用）。 */
export function viewKeys(): string[] {
  return Object.keys(views).map(key => key.replace(/^\/src\/modules\/([^/]+)\/views\/(.+)\.vue$/, '$1/$2'))
}
