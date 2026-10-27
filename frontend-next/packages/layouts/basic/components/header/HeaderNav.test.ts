/**
 * 顶栏面包屑的过渡列表 key。
 *
 * 各级与分隔符同是 TransitionGroup 的直接子项，key 由 v-for 模板的 key 拼上子项自己的 key 得来；
 * 子项 key 撞了，Vue 每次换路由都报 Duplicate keys，出入场也会认错节点。
 */
import type { LayoutBreadcrumbItem } from '../../contracts'
import { mount } from '@vue/test-utils'
import { afterEach, describe, expect, it } from 'vitest'
import { defineComponent } from 'vue'
import { createMemoryHistory, createRouter } from 'vue-router'
import HeaderNav from './HeaderNav.vue'

const appStore = {
  breadcrumbShowHome: false,
  breadcrumbHideOnlyOne: false,
  breadcrumbShowIcon: false,
  breadcrumbStyle: 'normal',
}

function crumbs(...paths: string[]): LayoutBreadcrumbItem[] {
  return paths.map(path => ({ title: path, path, siblings: [] }))
}

async function mountNav(breadcrumbs: LayoutBreadcrumbItem[]) {
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [{ path: '/:pathMatch(.*)*', component: defineComponent({ render: () => null }) }],
  })
  await router.push('/')
  const warnings: string[] = []
  const wrapper = mount(HeaderNav, {
    props: { appStore: appStore as never, breadcrumbs },
    global: {
      plugins: [router],
      // test-utils 缺省把 TransitionGroup 换成桩，桩不按 Vue 的规则拼子项 key，撞 key 就测不出来
      stubs: { 'transition-group': false },
      config: { warnHandler: msg => warnings.push(msg) },
    },
    attachTo: document.body,
  })
  return { warnings, wrapper }
}

afterEach(() => {
  document.body.innerHTML = ''
})

describe('headerNav 面包屑过渡列表', () => {
  it('换一组层级时不报重复 key：条目与它后面的分隔符各有各的 key', async () => {
    const { warnings, wrapper } = await mountNav(crumbs('/workbench', '/workbench/dashboard'))

    await wrapper.setProps({ breadcrumbs: crumbs('/tenant', '/tenant/list') })

    expect(warnings.filter(msg => msg.includes('Duplicate keys'))).toEqual([])
    // 退场的旧层级由 .crumb-leave-active 当场 display: none，这里只数留在列表里的
    const staying = (part: string) => wrapper.findAll(`[data-scope="breadcrumb"][data-part="${part}"]:not(.crumb-leave-active)`)
    expect(staying('item').map(item => item.text())).toEqual(['/tenant', '/tenant/list'])
    expect(staying('separator')).toHaveLength(1)
  })
})
