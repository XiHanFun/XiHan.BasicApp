import type { Page } from '@playwright/test'
import { expect } from '@playwright/test'

export interface Credentials {
  /** 租户 ID；平台账号不填 */
  tenant?: string
  username: string
  password: string
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

/** 列表页按关键字查询，返回命中的行 */
export async function search(page: Page, keyword: string) {
  await page.getByRole('textbox', { name: '搜索', exact: true }).fill(keyword)
  await page.getByRole('button', { name: '查询', exact: true }).click()
  return page.getByRole('row').filter({ hasText: keyword })
}

/** 等到某条提示出现（成功、失败都是 toast） */
export async function expectToast(page: Page, text: string | RegExp) {
  await expect(page.getByText(text).first()).toBeVisible()
}
