/**
 * 个人中心「个人资料」（ProfileTabInfo.vue，4.x 原样搬入）：
 * 表单照资料填，保存把整份资料交给壳层（生日转 ISO），取消即回到资料；能改用户名时给「修改」，90 天内改过即提示还要等几天；
 * 邮箱 / 手机显示当前值与验证状态（4.x 同）：没验证的给「验证」（发码后输满 6 位确认），有值的给「修改」、没有的给「绑定」（对话框里凭当前密码发码到新的，再输码确认）；
 * 头像先在前端认格式与大小，上传即换好（不再经保存资料写头像），去掉头像先确认再交给壳层；被拒都提示后端文案。
 */
import type { AppContextApis, UserProfile } from '~/types'
import { DOMWrapper, flushPromises, mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { configureDynamicIsland, dialog, prompt, toast } from '~/composables'
import { i18n } from '~/locales'
import { ApiError } from '~/request'
import { registerAppContext } from '~/stores'
import ProfileTabInfo from './ProfileTabInfo.vue'

vi.mock('~/composables', async (importOriginal) => {
  const actual = await importOriginal<typeof import('~/composables')>()
  return { ...actual, prompt: vi.fn() }
})

const apis = {
  updateProfileApi: vi.fn(),
  uploadAvatarApi: vi.fn(),
  removeAvatarApi: vi.fn(),
  changeUserNameApi: vi.fn(),
  getFilePresignedUrlApi: vi.fn(),
  sendEmailVerifyCodeApi: vi.fn(),
  verifyEmailApi: vi.fn(),
  sendChangePhoneCodeApi: vi.fn(),
  confirmChangePhoneApi: vi.fn(),
}

const DAY = 24 * 60 * 60 * 1000

const saved: UserProfile = {
  userId: 'u-1',
  userName: 'alice',
  nickName: 'Alice',
  realName: '张三',
  avatar: 'f-avatar',
  email: 'alice@example.com',
  gender: 2,
  birthday: '1990-05-04T00:00:00Z',
  country: '中国',
  remark: '你好',
  isSystemAccount: false,
  twoFactorEnabled: false,
  twoFactorMethod: 0,
  emailVerified: false,
  phoneVerified: false,
  canChangeUserName: true,
  isLocked: false,
  failedLoginAttempts: 0,
}

beforeEach(() => {
  setActivePinia(createPinia())
  i18n.global.locale.value = 'zh-CN'
  for (const fn of Object.values(apis))
    fn.mockReset()
  vi.mocked(prompt).mockReset()
  registerAppContext({ apis: apis as unknown as AppContextApis })
  apis.getFilePresignedUrlApi.mockResolvedValue('https://cdn.example/avatar.png')
})

const mounted: Array<{ unmount: () => void }> = []

afterEach(() => {
  for (const wrapper of mounted.splice(0))
    wrapper.unmount()
})

async function render(profile: UserProfile = saved) {
  const wrapper = mount(ProfileTabInfo, { props: { profile }, global: { plugins: [i18n] }, attachTo: document.body })
  mounted.push(wrapper)
  await flushPromises()
  return wrapper
}

type Wrapper = Awaited<ReturnType<typeof render>>

function button(wrapper: Wrapper, label: string) {
  return wrapper.findAll('button').find(item => item.text() === label)!
}

function field(wrapper: Wrapper, label: string) {
  return wrapper.find(`[aria-label="${label}"]`)
}

function contactRows(wrapper: Wrapper) {
  return wrapper.findAll('.pf-subgroup')[1]!.findAll('.pf-setting-row')
}

function buttonIn(root: DOMWrapper<Element>, label: string) {
  return root.findAll('button').find(item => item.text() === label)!
}

/** 验证码格子一格一位：逐格输入 */
async function fillCode(root: Pick<DOMWrapper<Element>, 'findAll'>, code: string) {
  const cells = root.findAll('input')
  for (const [index, digit] of [...code].entries())
    await cells[index]!.setValue(digit)
  await flushPromises()
}

function usernameRow(wrapper: Wrapper) {
  return wrapper.findAll('.pf-setting-row').find(row => row.find('.pf-setting-row__label').text().startsWith('用户名'))!
}

async function chooseAvatar(wrapper: Wrapper, file: File) {
  const input = wrapper.find('input[type="file"]')
  Object.defineProperty(input.element, 'files', { value: [file], configurable: true })
  await input.trigger('change')
  await flushPromises()
}

describe('个人资料页签', () => {
  it('表单照资料填，用户名带 @ 并给「修改」；邮箱 / 手机显示当前值与验证状态，没验证的给「验证」，没有的显示「未设置」并给「绑定」', async () => {
    const wrapper = await render()
    expect((field(wrapper, '显示名称').element as HTMLInputElement).value).toBe('Alice')
    expect((field(wrapper, '真实姓名').element as HTMLInputElement).value).toBe('张三')
    expect((field(wrapper, '国家 / 地区').element as HTMLInputElement).value).toBe('中国')
    expect((field(wrapper, '个人简介').element as HTMLTextAreaElement).value).toBe('你好')
    expect(usernameRow(wrapper).find('.pf-setting-row__desc').text()).toBe('@alice')
    expect(usernameRow(wrapper).find('button').text()).toBe('修改')

    const [email, phone] = contactRows(wrapper)
    expect(email!.find('.pf-setting-row__desc').text()).toBe('alice@example.com')
    expect(email!.find('.pf-setting-row__label').text()).toContain('未验证')
    expect(email!.findAll('button').map(item => item.text())).toEqual(['修改', '验证'])
    expect(phone!.find('.pf-setting-row__desc').text()).toBe('未设置')
    expect(phone!.findAll('button').map(item => item.text())).toEqual(['绑定'])
    expect(wrapper.text()).not.toContain('component.profile')
  })

  it('验证当前邮箱：发码后出现验证码格子，输满 6 位才能确认，交给壳层、提示并通知重拉；已验证的不再给「验证」', async () => {
    apis.sendEmailVerifyCodeApi.mockResolvedValue({ expiresInSeconds: 600 })
    apis.verifyEmailApi.mockResolvedValue({})
    const wrapper = await render()
    await button(wrapper, '验证').trigger('click')
    await flushPromises()
    expect(apis.sendEmailVerifyCodeApi).toHaveBeenCalledOnce()
    const confirm = button(wrapper, '确认')
    expect(confirm.attributes('disabled')).toBeDefined()
    await fillCode(wrapper.find('.pf-inline-form'), '123456')
    await button(wrapper, '确认').trigger('click')
    await flushPromises()
    expect(apis.verifyEmailApi).toHaveBeenCalledWith('123456')
    expect(wrapper.emitted('saved')).toHaveLength(1)
    expect(wrapper.find('.pf-inline-form').exists()).toBe(false)

    const verified = await render({ ...saved, emailVerified: true })
    expect(contactRows(verified)[0]!.findAll('button').map(item => item.text())).toEqual(['修改'])
    expect(contactRows(verified)[0]!.find('.pf-setting-row__label').text()).toContain('已验证')
  })

  it('绑定手机号：对话框里填新号与当前密码发码，再输码确认，交给壳层并通知重拉；发码被拒即提示后端文案、留在第一步', async () => {
    const wrapper = await render()
    const danger = vi.spyOn(toast, 'danger').mockImplementation(() => undefined as never)
    apis.sendChangePhoneCodeApi.mockRejectedValueOnce(new ApiError(409, { code: 'IDENTITY_CONFLICT_010', detail: '这个邮箱或手机号已被同一租户的别的账号使用。' }))
    await button(wrapper, '绑定').trigger('click')
    await flushPromises()
    const dialogRoot = () => new DOMWrapper(document.body.querySelector('[data-scope="dialog"][data-part="content"]')!)
    await dialogRoot().find('input[type="password"]').setValue('Pass@123456')
    await dialogRoot().find('input:not([type="password"])').setValue('13900000000')
    await buttonIn(dialogRoot(), '发送验证码').trigger('click')
    await flushPromises()
    expect(danger).toHaveBeenCalledWith('这个邮箱或手机号已被同一租户的别的账号使用。')
    expect(dialogRoot().find('input[type="password"]').exists()).toBe(true)

    apis.sendChangePhoneCodeApi.mockResolvedValue({ expiresInSeconds: 600 })
    apis.confirmChangePhoneApi.mockResolvedValue({})
    await buttonIn(dialogRoot(), '发送验证码').trigger('click')
    await flushPromises()
    expect(apis.sendChangePhoneCodeApi).toHaveBeenLastCalledWith({ newPhone: '13900000000', password: 'Pass@123456' })
    expect(dialogRoot().text()).toContain('13900000000')
    await fillCode(dialogRoot(), '654321')
    await buttonIn(dialogRoot(), '确认').trigger('click')
    await flushPromises()
    expect(apis.confirmChangePhoneApi).toHaveBeenCalledWith('654321')
    expect(wrapper.emitted('saved')).toHaveLength(1)
    danger.mockRestore()
  })

  it('90 天内改过用户名：标签提示还要等几天，不给「修改」', async () => {
    const wrapper = await render({ ...saved, canChangeUserName: false, lastUserNameChangeTime: new Date(Date.now() - 30 * DAY + DAY / 2).toISOString() })
    expect(usernameRow(wrapper).find('.pf-setting-row__label').text()).toContain('还需等待 61 天后可修改')
    expect(usernameRow(wrapper).find('button').exists()).toBe(false)
  })

  it('保存把整份资料交给壳层（生日转 ISO）、提示并通知重拉；取消即回到资料；被拒即提示后端文案', async () => {
    const success = vi.spyOn(toast, 'success').mockImplementation(() => '')
    const danger = vi.spyOn(toast, 'danger').mockImplementation(() => '')
    apis.updateProfileApi.mockResolvedValue(saved)
    const wrapper = await render()

    await field(wrapper, '显示名称').setValue('Alice Z')
    await button(wrapper, '保存更改').trigger('click')
    await flushPromises()
    expect(apis.updateProfileApi).toHaveBeenCalledWith({ nickName: 'Alice Z', realName: '张三', gender: 2, birthday: '1990-05-04T00:00:00.000Z', country: '中国', remark: '你好' })
    expect(success).toHaveBeenCalledWith('个人资料已更新')
    expect(wrapper.emitted('saved')).toHaveLength(1)

    await button(wrapper, '取消').trigger('click')
    expect((field(wrapper, '显示名称').element as HTMLInputElement).value).toBe('Alice')

    apis.updateProfileApi.mockRejectedValue(new ApiError(400, { code: 'KERNEL_VALIDATION_000', detail: '显示名称不能为空。' }))
    await button(wrapper, '保存更改').trigger('click')
    await flushPromises()
    expect(danger).toHaveBeenCalledWith('显示名称不能为空。')
    expect(wrapper.emitted('saved')).toHaveLength(1)
  })

  it('改用户名：弹窗问新用户名与当前密码，没填即提示并留在弹窗；填好交给壳层、提示并通知重拉，被拒提示后端文案并留在弹窗', async () => {
    const warning = vi.spyOn(toast, 'warning').mockImplementation(() => '')
    const success = vi.spyOn(toast, 'success').mockImplementation(() => '')
    const danger = vi.spyOn(toast, 'danger').mockImplementation(() => '')
    const wrapper = await render()

    await usernameRow(wrapper).find('button').trigger('click')
    const options = vi.mocked(prompt).mock.calls[0]![0]
    expect(options).toMatchObject({ title: '修改用户名', fields: [{ key: 'userName', value: 'alice' }, { key: 'password', type: 'password' }] })

    expect(await options.onOk!({ userName: '  ', password: 'pw' })).toBe(false)
    expect(warning).toHaveBeenLastCalledWith('请输入用户名')
    expect(await options.onOk!({ userName: 'bob', password: '' })).toBe(false)
    expect(warning).toHaveBeenLastCalledWith('请输入密码')
    expect(apis.changeUserNameApi).not.toHaveBeenCalled()

    apis.changeUserNameApi.mockResolvedValue(saved)
    expect(await options.onOk!({ userName: ' bob ', password: 'pw' })).toBeUndefined()
    expect(apis.changeUserNameApi).toHaveBeenCalledWith({ userName: 'bob', password: 'pw' })
    expect(success).toHaveBeenCalledWith('用户名已修改')
    expect(wrapper.emitted('saved')).toHaveLength(1)

    apis.changeUserNameApi.mockRejectedValue(new ApiError(409, { code: 'IDENTITY_CONFLICT_001', detail: '用户名已被占用。' }))
    expect(await options.onOk!({ userName: 'carol', password: 'pw' })).toBe(false)
    expect(danger).toHaveBeenCalledWith('用户名已被占用。')
  })

  it('头像：格式或大小不对只提示不上传；合格的交给壳层上传（带进度），上传即换好、不再经保存资料写头像；被拒即在结果里给后端文案', async () => {
    const warning = vi.spyOn(toast, 'warning').mockImplementation(() => '')
    // 灵动岛关着时终态改由轻提示给出：接住它来看结果
    const island = vi.fn()
    configureDynamicIsland({ isEnabled: () => false, message: island })
    apis.uploadAvatarApi.mockImplementation(async (_file: File, onProgress?: (percent: number) => void) => {
      onProgress?.(50)
      return { fileId: 'f-new' }
    })
    const wrapper = await render()

    await chooseAvatar(wrapper, new File(['gif'], 'a.gif', { type: 'image/gif' }))
    expect(warning).toHaveBeenLastCalledWith('仅支持 JPG、PNG 格式的图片')
    await chooseAvatar(wrapper, new File([new Uint8Array(2 * 1024 * 1024 + 1)], 'big.png', { type: 'image/png' }))
    expect(warning).toHaveBeenLastCalledWith('图片大小不能超过 2MB')
    expect(apis.uploadAvatarApi).not.toHaveBeenCalled()

    const avatar = new File([new Uint8Array(16)], 'me.png', { type: 'image/png' })
    await chooseAvatar(wrapper, avatar)
    expect(apis.uploadAvatarApi).toHaveBeenCalledWith(avatar, expect.any(Function))
    expect(apis.updateProfileApi).not.toHaveBeenCalled()
    expect(island).toHaveBeenLastCalledWith('success', '头像已更新')
    expect(wrapper.emitted('saved')).toHaveLength(1)

    apis.uploadAvatarApi.mockRejectedValue(new ApiError(400, { code: 'IDENTITY_VALIDATION_016', detail: '头像只能是不超过 2 MB 的 JPEG 或 PNG 图片。' }))
    await chooseAvatar(wrapper, avatar)
    expect(island).toHaveBeenLastCalledWith('error', '头像只能是不超过 2 MB 的 JPEG 或 PNG 图片。')
    expect(wrapper.emitted('saved')).toHaveLength(1)
  })

  it('去掉头像：确认后交给壳层、提示并通知重拉，被拒即提示后端文案；没有头像时按钮不可点', async () => {
    const success = vi.spyOn(toast, 'success').mockImplementation(() => '')
    const danger = vi.spyOn(toast, 'danger').mockImplementation(() => '')
    const confirm = vi.spyOn(dialog, 'confirm').mockImplementation(async (options) => {
      await options.onOk?.()
      return true
    })
    apis.removeAvatarApi.mockResolvedValue({ ...saved, avatar: undefined })
    const wrapper = await render()

    await button(wrapper, '删除').trigger('click')
    await flushPromises()
    expect(confirm).toHaveBeenCalledWith(expect.objectContaining({ title: '删除头像', content: '确定要删除当前头像吗？删除后将显示默认头像。' }))
    expect(apis.removeAvatarApi).toHaveBeenCalledTimes(1)
    expect(apis.updateProfileApi).not.toHaveBeenCalled()
    expect(success).toHaveBeenCalledWith('头像已删除')
    expect(wrapper.emitted('saved')).toHaveLength(1)

    apis.removeAvatarApi.mockRejectedValue(new ApiError(404, { code: 'IDENTITY_NOTFOUND_001', detail: '用户不存在。' }))
    await button(wrapper, '删除').trigger('click')
    await flushPromises()
    expect(danger).toHaveBeenCalledWith('用户不存在。')

    const { avatar: _avatar, ...withoutAvatar } = saved
    const bare = await render(withoutAvatar)
    expect((button(bare, '删除').element as HTMLButtonElement).disabled).toBe(true)
  })
})
