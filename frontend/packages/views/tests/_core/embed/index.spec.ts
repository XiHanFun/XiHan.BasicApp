/**
 * 内嵌菜单的页面（embed/index.vue）
 *
 * 职责边界：地址与标题取自路由 meta（iframeSrc、title，标题是文案键时翻译），加载完成前盖一层转圈；
 * 地址在建立时取一次：页面被缓存后切到别的路由，iframe 不跟着变
 */
import { flushPromises, mount } from '@vue/test-utils'
import { afterEach, describe, expect, it } from 'vitest'
import { createMemoryHistory, createRouter } from 'vue-router'
import { i18n } from '~/locales'
import EmbedPage from '~/views/_core/embed/index.vue'

async function mountAt(path: string) {
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [
      {
        path: '/embed/wiki',
        component: {
          render: () => null,
        },
        meta: { title: 'Wiki', iframeSrc: 'https://docs.xihanfun.com' },
      },
      {
        path: '/embed/keyed',
        component: {
          render: () => null,
        },
        meta: { title: 'common.loading', iframeSrc: 'https://example.com' },
      },
      {
        path: '/embed/none',
        component: {
          render: () => null,
        },
        meta: { title: 'None' },
      },
      {
        path: '/other',
        component: {
          render: () => null,
        },
        meta: { title: 'Other' },
      },
    ],
  })
  await router.push(path)
  const wrapper = mount(EmbedPage, {
    global: {
      plugins: [router, i18n],
    },
    attachTo: document.body,
  })
  return { router, wrapper }
}

afterEach(() => {
  document.body.innerHTML = ''
})

describe('embed page', () => {
  it('铺一个 iframe：地址与标题取自路由，加载完成前有转圈，加载完撤掉', async () => {
    const { wrapper } = await mountAt('/embed/wiki')
    const frame = wrapper.find('iframe')
    expect(frame.attributes('src')).toBe('https://docs.xihanfun.com')
    expect(frame.attributes('title')).toBe('Wiki')
    expect(wrapper.find('[data-scope="spinner"]').exists()).toBe(true)

    await frame.trigger('load')

    expect(wrapper.find('[data-scope="spinner"]').exists()).toBe(false)
  })

  it('标题是文案键时按当前语言翻译', async () => {
    const { wrapper } = await mountAt('/embed/keyed')
    expect(wrapper.find('iframe').attributes('title')).toBe(i18n.global.t('common.loading'))
  })

  it('切到别的路由后地址不变（被缓存的页面不跟着当前路由走）', async () => {
    const { router, wrapper } = await mountAt('/embed/wiki')
    await router.push('/other')
    await flushPromises()
    expect(wrapper.find('iframe').attributes('src')).toBe('https://docs.xihanfun.com')
  })

  it('没有地址时不出 iframe，也不转圈', async () => {
    const { wrapper } = await mountAt('/embed/none')
    expect(wrapper.find('iframe').exists()).toBe(false)
    expect(wrapper.find('[data-scope="spinner"]').exists()).toBe(false)
  })
})
