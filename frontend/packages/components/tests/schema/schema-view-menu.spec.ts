/**
 * 个人视图浮层：点名称套用、星标切换默认、删除，底部起名保存（同名的按钮写「覆盖」）
 */
import type { PersonalView } from '~/components/schema/useViewManager'
import { flushPromises, mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { afterEach, beforeEach, describe, expect, it } from 'vitest'
import SchemaViewMenu from '~/components/schema/SchemaViewMenu.vue'
import { i18n } from '~/locales'

const t = i18n.global.t

const views: PersonalView[] = [
  {
    code: 'view-1',
    name: '本周失败的',
    snapshot: { filters: {} },
    isDefault: true,
  },
  {
    code: 'view-2',
    name: '大文件',
    snapshot: { filters: {} },
  },
]

function button(name: string): HTMLButtonElement {
  const found = [
    ...document.body.querySelectorAll('button'),
  ].find(element => element.getAttribute('aria-label') === name || element.textContent?.trim() === name)
  if (!found)
    throw new Error(`no button ${name}`)
  return found
}

async function open(activeCode?: string) {
  const wrapper = mount(SchemaViewMenu, {
    props: { views, activeCode },
    global: {
      plugins: [i18n],
    },
    attachTo: document.body,
  })
  button(t('component.schema_views.title')).click()
  await flushPromises()
  return wrapper
}

beforeEach(() => {
  setActivePinia(createPinia())
})

afterEach(() => {
  document.body.innerHTML = ''
})

describe('schemaViewMenu', () => {
  it('点名称套用，当前视图标为 aria-current', async () => {
    const wrapper = await open('view-2')

    expect(button('大文件').getAttribute('aria-current')).toBe('true')
    expect(button('本周失败的').hasAttribute('aria-current')).toBe(false)
    button('本周失败的').click()
    expect(wrapper.emitted('apply')).toEqual([
      ['view-1'],
    ])
    wrapper.unmount()
  })

  it('星标：默认的再点即取消，别的设为默认；删除按名称', async () => {
    const wrapper = await open()

    button(t('component.schema_views.unset_default', { name: '本周失败的' })).click()
    button(t('component.schema_views.set_default', { name: '大文件' })).click()
    button(t('component.schema_views.remove', { name: '大文件' })).click()
    expect(wrapper.emitted('setDefault')).toEqual([
      [null],
      ['view-2'],
    ])
    expect(wrapper.emitted('remove')).toEqual([
      ['view-2'],
    ])
    wrapper.unmount()
  })

  it('起名保存，名称去空白；同名时按钮写覆盖；没填名称不能保存', async () => {
    const wrapper = await open()
    const input = document.body.querySelector<HTMLInputElement>('input[name="viewName"]')!

    expect(button(t('component.schema_views.save')).disabled).toBe(true)
    input.value = ' 大文件 '
    input.dispatchEvent(new Event('input'))
    await flushPromises()
    expect(button(t('component.schema_views.overwrite')).disabled).toBe(false)

    input.value = ' 新的 '
    input.dispatchEvent(new Event('input'))
    await flushPromises()
    button(t('component.schema_views.save')).click()
    await flushPromises()
    expect(wrapper.emitted('save')).toEqual([
      ['新的'],
    ])
    wrapper.unmount()
  })
})
