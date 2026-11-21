/**
 * 菜单项的尾部图标（use-layout-menu-domain 的 buildMenuOptionsFromRoutes）
 *
 * 职责边界：外链（meta.link）与内嵌（meta.iframeSrc）菜单在标签末尾追加图标，告诉用户点开后在哪里打开；
 * 普通页面不追加。图标由侧栏与顶栏共用的 MENU_TRAILING_ICONS 给出
 */
import type { VNodeChild } from 'vue'
import type { MenuTrailingKind } from '~/layouts/basic/composables'
import type { LayoutRouteRecord } from '~/layouts/basic/contracts'
import { mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { describe, expect, it } from 'vitest'
import { defineComponent, h } from 'vue'
import { createMemoryHistory, createRouter } from 'vue-router'
import { MENU_TRAILING_ICONS, useLayoutMenuDomain } from '~/layouts/basic/composables'

function menuDomain() {
  setActivePinia(createPinia())
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [
      {
        path: '/',
        component: {
          render: () => null,
        },
      },
    ],
  })
  let domain: ReturnType<typeof useLayoutMenuDomain> | undefined
  mount(defineComponent({
    setup() {
      domain = useLayoutMenuDomain()
      return () => null
    },
  }), {
    global: {
      plugins: [router],
    },
  })
  return domain!
}

const routes = [
  {
    path: '/identity/users',
    name: 'identity.users',
    meta: { title: 'Users' },
  },
  {
    path: '/custom.github',
    name: 'custom.github',
    meta: { title: 'GitHub', link: 'https://github.com' },
  },
  {
    path: '/embed/about-docs',
    name: 'custom.about-docs',
    meta: { title: 'Docs', iframeSrc: 'https://basicapp.docs.xihanfun.com/' },
  },
] as unknown as LayoutRouteRecord[]

/** 把选项的 label 渲染成 HTML，看末尾挂了哪个图标 */
function labelHtml(label: unknown): string {
  return mount(defineComponent({
    render: () => (typeof label === 'function'
      ? (label as () => VNodeChild)()
      : label) as VNodeChild,
  })).html()
}

describe('menu trailing icons', () => {
  it('外链追加外链图标、内嵌追加窗口图标，普通页面不追加', () => {
    const { buildMenuOptionsFromRoutes } = menuDomain()
    const kinds: MenuTrailingKind[] = []
    const options = buildMenuOptionsFromRoutes(routes, {
      keyBy: 'path',
      translate: (title: string) => title,
      trailingIcon: (kind: MenuTrailingKind) => {
        kinds.push(kind)
        return h('i', { 'data-icon': MENU_TRAILING_ICONS[kind] })
      },
    })

    expect(options[0]?.label).toBe('Users')
    expect(labelHtml(options[1]?.label)).toContain('data-icon="lucide:external-link"')
    expect(labelHtml(options[2]?.label)).toContain('data-icon="lucide:app-window"')
    expect(kinds).toEqual(['link', 'embed'])
  })

  it('外链与内嵌的图标不同', () => {
    expect(MENU_TRAILING_ICONS.link).not.toBe(MENU_TRAILING_ICONS.embed)
  })
})
