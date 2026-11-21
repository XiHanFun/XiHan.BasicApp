/**
 * 文件引用上传：字段只存文件 ID
 *
 * 回填的 ID 按 getFileApi 显示原始文件名，取不到时如实标为不可用（不显示一个打不开的 ID）；
 * 移除发出空值，由表单决定报文里发不发；上传显示进度，中断了留下续传提示与「继续上传」
 */
import type { VueWrapper } from '@vue/test-utils'
import { flushPromises, mount } from '@vue/test-utils'
import { describe, expect, it, vi } from 'vitest'
import FileRefUpload from '~/components/common/FileRefUpload.vue'
import { toast } from '~/composables'
import { ResumableUploadError } from '~/files'
import { $t, i18n } from '~/locales'
import { registerAppContext } from '~/stores/app-context'

type UploadFileApi = (file: File, options?: { onProgress?: (percent: number) => void, signal?: AbortSignal }) => Promise<{ id: string, originalName: string }>

function mountWith(apis: { getFileApi?: (fileId: string) => Promise<{ originalName: string }>, uploadFileApi?: UploadFileApi }, props: { value?: null | string, kind?: 'file' | 'image', maxSizeMb?: number } = {}) {
  registerAppContext({
    apis: {
      getFileApi: apis.getFileApi ?? vi.fn(async () => ({ originalName: 'a.txt' })),
      getFilePresignedUrlApi: vi.fn(async () => ''),
      uploadFileApi: apis.uploadFileApi ?? vi.fn(),
    } as never,
  })
  return mount(FileRefUpload, {
    props: { value: null, kind: 'file', ...props },
    global: {
      plugins: [i18n],
    },
  })
}

async function choose(wrapper: VueWrapper, file: File) {
  const input = wrapper.find('input[type="file"]')
  Object.defineProperty(input.element, 'files', {
    value: [file],
    configurable: true,
  })
  await input.trigger('change')
  await flushPromises()
}

function deferred<T>() {
  let resolve!: (value: T) => void
  const promise = new Promise<T>((done) => {
    resolve = done
  })
  return { promise, resolve }
}

function buttonWith(wrapper: VueWrapper, label: string) {
  return wrapper.findAll('button').find(button => button.text().includes(label))
}

const big = new File([
  new Uint8Array(32),
], 'big.bin', { type: 'application/octet-stream' })

describe('xFileRefUpload', () => {
  it('回填的文件 ID 显示原始文件名', async () => {
    const getFileApi = vi.fn(async () => ({ originalName: '合同.pdf' }))
    const wrapper = mountWith({ getFileApi }, { value: 'f-1' })
    await flushPromises()

    expect(getFileApi).toHaveBeenCalledWith('f-1')
    expect(wrapper.find('.x-file-ref-upload__name').text()).toBe('合同.pdf')
  })

  it('文件没有了或看不到时标为不可用，也不出打开入口', async () => {
    const wrapper = mountWith({
      getFileApi: vi.fn(async () => {
        throw new Error('FILES_NOTFOUND_001')
      }),
    }, { value: 'f-2' })
    await flushPromises()

    expect(wrapper.find('.x-file-ref-upload__name').text()).toBe(i18n.global.t('component.file_ref_upload.unavailable'))
    expect(wrapper.findAll('button').some(button => button.text() === i18n.global.t('component.file_ref_upload.open'))).toBe(false)
  })

  it('移除发出空值', async () => {
    const wrapper = mountWith({}, { value: 'f-3' })
    await flushPromises()

    await buttonWith(wrapper, i18n.global.t('component.file_ref_upload.remove'))!.trigger('click')

    expect(wrapper.emitted('update:value')).toEqual([
      [null],
    ])
  })

  it('上传时显示进度，传完只回填文件 ID', async () => {
    const done = deferred<{ id: string, originalName: string }>()
    const uploadFileApi = vi.fn<UploadFileApi>(async (_file, options) => {
      options?.onProgress?.(40)
      return done.promise
    })
    const wrapper = mountWith({ uploadFileApi })

    await choose(wrapper, big)

    expect(uploadFileApi).toHaveBeenCalledWith(big, expect.objectContaining({
      signal: expect.any(AbortSignal),
    }))
    expect(wrapper.find('.x-file-ref-upload__percent').text()).toBe(i18n.global.t('component.file_ref_upload.uploading', { percent: 40 }))
    expect(wrapper.find('[role="progressbar"]').attributes('aria-valuenow')).toBe('40')

    done.resolve({ id: 'f-9', originalName: 'big.bin' })
    await flushPromises()

    expect(wrapper.emitted('update:value')).toEqual([
      ['f-9'],
    ])
    expect(wrapper.find('.x-file-ref-upload__progress').exists()).toBe(false)
  })

  it('中断了留下续传提示，「继续上传」再传同一个文件', async () => {
    const danger = vi.spyOn(toast, 'danger')
    const uploadFileApi = vi.fn<UploadFileApi>()
      .mockRejectedValueOnce(new ResumableUploadError(new TypeError('Network request failed')))
      .mockResolvedValueOnce({ id: 'f-10', originalName: 'big.bin' })
    const wrapper = mountWith({ uploadFileApi })

    await choose(wrapper, big)

    expect(wrapper.text()).toContain(i18n.global.t('component.file_ref_upload.interrupted', { name: 'big.bin' }))
    expect(wrapper.text()).toContain($t('file_upload.interrupted'))
    expect(danger).not.toHaveBeenCalled()
    expect(wrapper.emitted('update:value')).toBeUndefined()

    await buttonWith(wrapper, i18n.global.t('component.file_ref_upload.resume'))!.trigger('click')
    await flushPromises()

    expect(uploadFileApi).toHaveBeenCalledTimes(2)
    expect(uploadFileApi.mock.calls[1]![0]).toBe(big)
    expect(wrapper.emitted('update:value')).toEqual([
      ['f-10'],
    ])
    expect(wrapper.text()).not.toContain($t('file_upload.interrupted'))
  })

  it('别的失败轻提示原因，不出续传提示', async () => {
    const danger = vi.spyOn(toast, 'danger')
    const wrapper = mountWith({
      uploadFileApi: vi.fn(async () => {
        throw new Error('文件太大')
      }),
    })

    await choose(wrapper, big)

    expect(danger).toHaveBeenCalledWith('文件太大')
    expect(buttonWith(wrapper, i18n.global.t('component.file_ref_upload.resume'))).toBeUndefined()
  })

  it('字段卸载时中止正在传的文件', async () => {
    let signal: AbortSignal | undefined
    const wrapper = mountWith({
      uploadFileApi: vi.fn<UploadFileApi>((_file, options) => {
        signal = options?.signal
        return new Promise(() => {})
      }),
    })

    await choose(wrapper, big)
    expect(signal?.aborted).toBe(false)

    wrapper.unmount()

    expect(signal?.aborted).toBe(true)
  })

  it('文件缺省不在前端限大小，图片缺省 10MB，给了上限就按它', async () => {
    expect(mountWith({}).find('.x-file-ref-upload__hint').exists()).toBe(false)
    expect(mountWith({}, { kind: 'image' }).find('.x-file-ref-upload__hint').text()).toBe(i18n.global.t('component.file_ref_upload.hint_image', { size: 10 }))
    expect(mountWith({}, { maxSizeMb: 50 }).find('.x-file-ref-upload__hint').text()).toBe(i18n.global.t('component.file_ref_upload.hint_file', { size: 50 }))
  })

  it('拒收按原因提示：类型不符报类型，超过上限报大小，都不上传', async () => {
    const danger = vi.spyOn(toast, 'danger')
    const uploadFileApi = vi.fn<UploadFileApi>()
    const pdf = new File([
      new Uint8Array(8),
    ], '合同.pdf', { type: 'application/pdf' })

    await choose(mountWith({ uploadFileApi }, { kind: 'image' }), pdf)
    expect(danger).toHaveBeenLastCalledWith(i18n.global.t('component.file_ref_upload.type_rejected', { name: '合同.pdf' }))

    await choose(mountWith({ uploadFileApi }, { maxSizeMb: 1 }), new File([
      new Uint8Array(2 * 1024 * 1024),
    ], 'big.bin'))
    expect(danger).toHaveBeenLastCalledWith(i18n.global.t('component.file_ref_upload.too_large', { size: 1 }))

    expect(danger).toHaveBeenCalledTimes(2)
    expect(uploadFileApi).not.toHaveBeenCalled()
  })

  it('一次给了多个文件只传第一个，其余提示一次只能选一个', async () => {
    const danger = vi.spyOn(toast, 'danger')
    const uploadFileApi = vi.fn<UploadFileApi>(async () => ({ id: 'f-11', originalName: 'a.txt' }))
    const wrapper = mountWith({ uploadFileApi })
    const input = wrapper.find('input[type="file"]')
    Object.defineProperty(input.element, 'files', {
      value: [
        new File(['a'], 'a.txt'),
        new File(['b'], 'b.txt'),
        new File(['c'], 'c.txt'),
      ],
      configurable: true,
    })
    await input.trigger('change')
    await flushPromises()

    expect(uploadFileApi).toHaveBeenCalledOnce()
    expect(uploadFileApi.mock.calls[0]![0].name).toBe('a.txt')
    expect(danger.mock.calls).toEqual([
      [
        i18n.global.t('component.file_ref_upload.too_many'),
      ],
    ])
  })
})
