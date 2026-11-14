import type { Page } from '@playwright/test'
import { expect } from '@playwright/test'

export interface Credentials {
  /** 租户 ID；平台账号不填 */
  tenant?: string
  username: string
  password: string
}

/** 用例里新建的用户 */
export interface NewUser extends Credentials {
  displayName: string
  email: string
}

/** 本次运行独有的后缀：库在本机多次运行之间保留，开通的租户与账号靠它不重名 */
export const RUN = Date.now().toString(36)

/** 登录页填表提交；落到首页还是强制改密的锁屏由调用方断言 */
export async function submitLogin(page: Page, credentials: Credentials) {
  await page.goto('/#/auth/login')
  // 登录配置取到之前提交钮是禁用的，页面还在挂载，先填的值可能被换掉
  const submit = page.getByRole('button', { name: '登录', exact: true })
  await expect(submit).toBeEnabled()
  const fields: [string, string][] = [
    ['租户 ID（平台账号留空）', credentials.tenant ?? ''],
    ['请输入用户名或邮箱', credentials.username],
    ['请输入密码', credentials.password],
  ]
  for (const [placeholder, value] of fields) {
    const input = page.getByPlaceholder(placeholder)
    await input.fill(value)
    await expect(input).toHaveValue(value)
  }
  await submit.click()
}

/** 登录并等到工作台 */
export async function login(page: Page, credentials: Credentials) {
  await submitLogin(page, credentials)
  await expectWorkbench(page)
}

export async function expectWorkbench(page: Page) {
  await expect(page).toHaveURL(/#\/workbench\/dashboard/)
  await expect(page.getByRole('navigation', { name: '侧边栏' })).toBeVisible()
}

/** 打开某个列表页：直接进路由，等页面的表格出来 */
export async function openPage(page: Page, path: string) {
  await page.goto(`/#${path}`)
  await expect(page.getByRole('grid').first()).toBeVisible()
}

/** 列表页按关键字查询，返回命中的行；field 是关键字搜索框的标签，各页不同 */
export async function search(page: Page, keyword: string, field = '搜索') {
  await page.getByRole('textbox', { name: field, exact: true }).fill(keyword)
  await page.getByRole('button', { name: '查询', exact: true }).click()
  return page.getByRole('row').filter({ hasText: keyword })
}

/** 在用户页新建一个用户；调用方已登录且能建用户 */
export async function createUser(page: Page, user: NewUser) {
  await openPage(page, '/identity/users')
  await page.getByRole('button', { name: '新建用户' }).click()
  const dialog = page.getByRole('dialog', { name: '新建用户' })
  await dialog.getByRole('textbox', { name: '用户名' }).fill(user.username)
  await dialog.getByRole('textbox', { name: '显示名' }).fill(user.displayName)
  await dialog.getByRole('textbox', { name: '邮箱' }).fill(user.email)
  await dialog.getByRole('textbox', { name: '初始密码' }).fill(user.password)
  await dialog.getByRole('button', { name: '保存' }).click()
  await expect(dialog).toBeHidden()
}

/** 在用户页给一个用户直授几个权限码，等到保存成功；抽屉保存后不关，留着接着改 */
export async function grantDirectly(page: Page, user: Pick<NewUser, 'displayName' | 'username'>, codes: string[]) {
  // 平台一侧的用户随每次运行越积越多，先按用户名查出那一行
  await search(page, user.username, '关键字')
  await page.getByRole('row').filter({ hasText: `@${user.username}` }).getByRole('button', { name: '更多' }).click()
  await page.getByRole('menuitem', { name: '权限直授' }).click()
  const grant = page.getByRole('dialog', { name: `权限直授 · ${user.displayName}` })
  for (const code of codes) {
    await grant.getByRole('textbox', { name: '可授予' }).fill(code)
    await grant.getByRole('listbox', { name: '可授予' }).getByRole('option', { name: code }).click()
    await grant.getByRole('button', { name: '将选中项移到「已授予」' }).click()
  }
  await expect(grant.getByRole('listbox', { name: '已授予' }).getByRole('option')).toHaveCount(codes.length)
  await grant.getByRole('button', { name: '保存直授' }).click()
  await expectToast(page, `已保存（授予 ${codes.length} 项，拒绝 0 项，撤销 0 项）`)
}

/** 等到某条提示出现（成功、失败都是 toast） */
export async function expectToast(page: Page, text: string | RegExp) {
  await expect(page.getByText(text).first()).toBeVisible()
}
