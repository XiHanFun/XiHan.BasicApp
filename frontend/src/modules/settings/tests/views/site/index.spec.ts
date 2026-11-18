/**
 * 站点设置页（views/site/index.vue）：
 * 选了图片先进裁剪弹窗（标题带图片名），Logo 与浏览器图标锁 1:1；
 * 确认后把裁好的那张传到对应槽位，成功即收起并回收，被拒提示后端文案、弹窗留着；读不出来的图片只提示；
 * 页首提示平台侧与租户侧都是信息色
 */
import type { ImageCropSource } from '~/components'
import { DOMWrapper, flushPromises, mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { reactive, ref, shallowRef } from 'vue'
import SiteView from '@/modules/settings/views/site/index.vue'
import { exportCroppedImage, loadImageCropSource, releaseImageCropSource } from '~/components/common/image-crop'
import { toast } from '~/composables'
import { i18n } from '~/locales'
import { useAuthStore } from '~/stores'

// jsdom 不解码图片也没有画布：解码与出图换成桩，裁切框的初值照旧算
vi.mock('~/components/common/image-crop', async (importOriginal) => {
  const actual = await importOriginal<typeof import('~/components/common/image-crop')>()
  return {
    ...actual,
    loadImageCropSource: vi.fn(),
    exportCroppedImage: vi.fn(),
    releaseImageCropSource: vi.fn(),
  }
})

function emptyProfile() {
  return {
    name: null,
    subtitle: null,
    description: null,
    logo: null,
    favicon: null,
    copyrightOwner: null,
    copyrightUrl: null,
    copyrightSince: null,
    icp: null,
    icpUrl: null,
  }
}

/** 页面只经这个编辑器读写站点设置：换成假的，看页面把什么交给它 */
const editor = {
  loading: ref(false),
  saving: ref(false),
  uploading: ref<null | string>(null),
  loadError: ref<null | string>(null),
  submitError: ref<null | string>(null),
  settings: shallowRef<{ isPlatform: boolean, own: ReturnType<typeof emptyProfile>, inherited: null | ReturnType<typeof emptyProfile> }>({
    isPlatform: true,
    own: emptyProfile(),
    inherited: null,
  }),
  form: reactive(Object.fromEntries(Object.keys(emptyProfile()).map(key => [key, '']))),
  load: vi.fn(async () => {}),
  save: vi.fn(async () => true),
  upload: vi.fn<(field: string, file: File) => Promise<boolean>>(),
  remove: vi.fn(async () => true),
  fieldError: () => undefined,
  inherited: () => '',
}

vi.mock('@/modules/settings/views/site/use-site', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@/modules/settings/views/site/use-site')>()
  return {
    ...actual,
    useSiteSettings: () => editor,
  }
})

const mounted: Array<{ unmount: () => void }> = []

beforeEach(() => {
  setActivePinia(createPinia())
  i18n.global.locale.value = 'zh-CN'
  useAuthStore().permissions = {
    permissions: ['settings.site.read', 'settings.site.update'],
    roles: [],
    menus: [],
    pages: [],
    isPlatform: true,
  } as never
  editor.upload.mockReset()
  editor.submitError.value = null
  editor.settings.value = {
    isPlatform: true,
    own: emptyProfile(),
    inherited: null,
  }
  vi.mocked(loadImageCropSource).mockReset()
  vi.mocked(exportCroppedImage).mockReset()
  vi.mocked(releaseImageCropSource).mockReset()
})

afterEach(() => {
  for (const wrapper of mounted.splice(0))
    wrapper.unmount()
  vi.restoreAllMocks()
})

async function render() {
  const wrapper = mount(SiteView, {
    global: {
      plugins: [i18n],
    },
    attachTo: document.body,
  })
  mounted.push(wrapper)
  await flushPromises()
  return wrapper
}

function sourceOf(file: File): ImageCropSource {
  return {
    file,
    image: { naturalWidth: 800, naturalHeight: 600 } as HTMLImageElement,
    url: 'blob:site',
  }
}

/** 点某张图的「上传」再选中文件 */
async function choose(wrapper: Awaited<ReturnType<typeof render>>, index: number, file: File) {
  const card = wrapper.findAll('.site-settings__image')[index]!
  await card.findAll('button').find(button => button.text() === '上传')!.trigger('click')
  const input = wrapper.find('input[type="file"]')
  Object.defineProperty(input.element, 'files', {
    value: [file],
    configurable: true,
  })
  await input.trigger('change')
  await flushPromises()
}

function openDialog(): DOMWrapper<Element> | null {
  const element = document.body.querySelector('[role="dialog"]')
  return element
    ? new DOMWrapper(element)
    : null
}

/** 等裁剪弹窗挂出来：文件里头一次开对话框要多等几轮 */
async function cropDialog(): Promise<DOMWrapper<Element>> {
  await vi.waitFor(() => expect(openDialog()).not.toBeNull())
  return openDialog()!
}

function buttonIn(scope: DOMWrapper<Element>, text: string): DOMWrapper<Element> {
  return scope.findAll('button').find(button => button.text() === text)!
}

describe('站点图片上传先裁剪', () => {
  it('logo：进裁剪弹窗、锁 1:1，确认后传裁好的那张到 Logo 槽位，成功即收起并回收', async () => {
    const success = vi.spyOn(toast, 'success').mockImplementation(() => '')
    const original = new File([
      new Uint8Array(16),
    ], 'logo.png', { type: 'image/png' })
    const cropped = new File([
      new Uint8Array(8),
    ], 'logo.png', { type: 'image/png' })
    const source = sourceOf(original)
    vi.mocked(loadImageCropSource).mockResolvedValue(source)
    vi.mocked(exportCroppedImage).mockResolvedValue(cropped)
    editor.upload.mockResolvedValue(true)
    const wrapper = await render()

    await choose(wrapper, 0, original)
    expect((await cropDialog()).text()).toContain('裁剪图片：Logo')
    expect(openDialog()!.findAll('[data-scope="image-cropper"][data-part="crop-handle"]')).toHaveLength(4)

    await buttonIn(openDialog()!, '上传').trigger('click')
    await flushPromises()
    expect(exportCroppedImage).toHaveBeenLastCalledWith(source, {
      x: 100,
      y: 0,
      width: 600,
      height: 600,
    }, { maxWidth: 512, opaque: false })
    expect(editor.upload).toHaveBeenCalledWith('logo', cropped)
    expect(success).toHaveBeenLastCalledWith('图片已更换')
    expect(openDialog()).toBeNull()
    expect(releaseImageCropSource).toHaveBeenCalledWith(source)
  })

  it('上传被拒：提示后端文案，弹窗留着可再传；取消才收起并回收', async () => {
    const danger = vi.spyOn(toast, 'danger').mockImplementation(() => '')
    const original = new File([
      new Uint8Array(16),
    ], 'icon.png', { type: 'image/png' })
    const source = sourceOf(original)
    vi.mocked(loadImageCropSource).mockResolvedValue(source)
    vi.mocked(exportCroppedImage).mockResolvedValue(new File([
      new Uint8Array(8),
    ], 'icon.png', { type: 'image/png' }))
    editor.upload.mockImplementation(async () => {
      editor.submitError.value = '站点图片只能是不超过 2 MB 的 PNG、JPEG、WebP 或 ICO。'
      return false
    })
    const wrapper = await render()

    await choose(wrapper, 1, original)
    await buttonIn(await cropDialog(), '上传').trigger('click')
    await flushPromises()
    expect(editor.upload).toHaveBeenCalledWith('favicon', expect.any(File))
    expect(danger).toHaveBeenLastCalledWith('站点图片只能是不超过 2 MB 的 PNG、JPEG、WebP 或 ICO。')
    expect(openDialog()).not.toBeNull()
    expect(releaseImageCropSource).not.toHaveBeenCalled()

    await buttonIn(openDialog()!, '取消').trigger('click')
    await flushPromises()
    expect(openDialog()).toBeNull()
    expect(releaseImageCropSource).toHaveBeenCalledWith(source)
  })

  it('图片读不出来：只提示，不开裁剪也不上传', async () => {
    const warning = vi.spyOn(toast, 'warning').mockImplementation(() => '')
    vi.mocked(loadImageCropSource).mockRejectedValue(new Error('decode failed'))
    const wrapper = await render()

    await choose(wrapper, 0, new File([
      new Uint8Array(4),
    ], 'broken.png', { type: 'image/png' }))
    expect(warning).toHaveBeenLastCalledWith('图片无法读取，请换一张')
    expect(openDialog()).toBeNull()
    expect(editor.upload).not.toHaveBeenCalled()
  })
})

describe('页首提示', () => {
  it('平台侧与租户侧都是信息色，各说各的', async () => {
    const platform = await render()
    const platformAlert = platform.find('[data-scope="alert"][data-part="root"]')
    expect(platformAlert.attributes('data-tone')).toBe('info')
    expect(platformAlert.text()).toContain('整个平台的缺省')

    editor.settings.value = {
      isPlatform: false,
      own: emptyProfile(),
      inherited: emptyProfile(),
    }
    const tenant = await render()
    const tenantAlert = tenant.find('[data-scope="alert"][data-part="root"]')
    expect(tenantAlert.attributes('data-tone')).toBe('info')
    expect(tenantAlert.text()).toContain('本租户的品牌')
  })
})
