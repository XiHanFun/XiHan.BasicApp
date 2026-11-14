import type { Locator, Page, Response } from '@playwright/test'
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

/** 各页面进壳层时读到的「新版欢迎页看过没有」，先到的排着等取 */
const welcomeChecks = new WeakMap<Page, boolean[]>()

function isWelcomeSetting(response: Response, method: 'GET' | 'PUT'): boolean {
  const request = response.request()
  const url = new URL(response.url())
  if (request.method() !== method || !url.pathname.endsWith('/api/user-settings'))
    return false
  return method === 'GET'
    ? url.searchParams.get('key') === 'welcome'
    : (request.postData() ?? '').includes('"welcome"')
}

/** 开始记这个页面进壳层时的那次读取；同一个页面只挂一次 */
function watchWelcome(page: Page): boolean[] {
  const watched = welcomeChecks.get(page)
  if (watched)
    return watched
  const checks: boolean[] = []
  welcomeChecks.set(page, checks)
  page.on('response', (response) => {
    if (!isWelcomeSetting(response, 'GET'))
      return
    // 读不到时前端这一次不弹，与看过同样处理
    if (!response.ok()) {
      checks.push(true)
      return
    }
    // 正文取不出来是页面紧接着整页重建了：重建之后会再读一次，这一次不算
    void response.json().then((setting: { value?: null | string }) => checks.push(!!setting.value), () => {})
  })
  return checks
}

/** 新版欢迎页那一层 */
export function welcomeLayer(page: Page): Locator {
  return page.getByRole('dialog', { name: /^欢迎使用/ })
}

/**
 * 等进壳层时的那次读取回来，返回这个账号看过新版欢迎页没有
 * 欢迎页出不出现由它定，等它回来再断言，不靠猜时间
 */
export async function welcomeSeen(page: Page): Promise<boolean> {
  const checks = watchWelcome(page)
  await expect.poll(() => checks.length, { message: '进壳层时应当读一次「新版欢迎页看过没有」' }).toBeGreaterThan(0)
  return checks.shift()!
}

/** 欢迎页关掉时把「看过」记到账号上的那次保存；点关闭之前先挂上，关掉之后等它回来再往下走 */
export function welcomeSaved(page: Page): Promise<Response> {
  return page.waitForResponse(response => isWelcomeSetting(response, 'PUT'))
}

/** 登录页填表提交；落到首页还是强制改密的锁屏由调用方断言 */
export async function submitLogin(page: Page, credentials: Credentials) {
  // 换了账号，上一个账号留下的读取结果不算数
  watchWelcome(page).length = 0
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

/** 等到工作台；没看过新版欢迎页的账号先像用户那样把它关掉 */
export async function expectWorkbench(page: Page) {
  await expect(page).toHaveURL(/#\/workbench\/dashboard/)
  if (!await welcomeSeen(page)) {
    const welcome = welcomeLayer(page)
    await expect(welcome).toBeVisible()
    const saved = welcomeSaved(page)
    await page.keyboard.press('Escape')
    await expect(welcome).toBeHidden()
    await saved
  }
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
