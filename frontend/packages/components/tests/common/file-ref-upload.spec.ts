/**
 * 文件引用上传：字段只存文件 ID
 *
 * 回填的 ID 按 getFileApi 显示原始文件名，取不到时如实标为不可用（不显示一个打不开的 ID）；
 * 移除发出空值，由表单决定报文里发不发
 */
import { flushPromises, mount } from '@vue/test-utils'
import { describe, expect, it, vi } from 'vitest'
import FileRefUpload from '~/components/common/FileRefUpload.vue'
import { i18n } from '~/locales'
import { registerAppContext } from '~/stores/app-context'

function mountWith(getFileApi: (fileId: string) => Promise<{ originalName: string }>, value: string) {
  registerAppContext({
    apis: {
      getFileApi,
      getFilePresignedUrlApi: vi.fn(async () => ''),
    } as never,
  })
  return mount(FileRefUpload, {
    props: { value, kind: 'file' },
    global: {
      plugins: [i18n],
    },
  })
}

describe('xFileRefUpload', () => {
  it('回填的文件 ID 显示原始文件名', async () => {
    const getFileApi = vi.fn(async () => ({ originalName: '合同.pdf' }))
    const wrapper = mountWith(getFileApi, 'f-1')
    await flushPromises()

    expect(getFileApi).toHaveBeenCalledWith('f-1')
    expect(wrapper.find('.x-file-ref-upload__name').text()).toBe('合同.pdf')
  })

  it('文件没有了或看不到时标为不可用，也不出打开入口', async () => {
    const wrapper = mountWith(vi.fn(async () => {
      throw new Error('FILES_NOTFOUND_001')
    }), 'f-2')
    await flushPromises()

    expect(wrapper.find('.x-file-ref-upload__name').text()).toBe(i18n.global.t('component.file_ref_upload.unavailable'))
    expect(wrapper.findAll('button').some(button => button.text() === i18n.global.t('component.file_ref_upload.open'))).toBe(false)
  })

  it('移除发出空值', async () => {
    const wrapper = mountWith(vi.fn(async () => ({ originalName: 'a.txt' })), 'f-3')
    await flushPromises()

    const remove = wrapper.findAll('button').find(button => button.text().includes(i18n.global.t('component.file_ref_upload.remove')))
    await remove!.trigger('click')

    expect(wrapper.emitted('update:value')).toEqual([
      [null],
    ])
  })
})
