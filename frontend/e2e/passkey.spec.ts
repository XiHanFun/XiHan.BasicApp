import type { Page } from '@playwright/test'
import { expect, test } from '@playwright/test'
import { SUPER_ADMIN } from './env.ts'
import { expectToast, expectWorkbench, login, RUN } from './support.ts'

const LOGIN_METHODS = 'identity.login-methods'

/** 浏览器里装一个虚拟认证器：自动应答、做用户验证，免得弹系统的通行密钥窗 */
async function useVirtualAuthenticator(page: Page) {
  const cdp = await page.context().newCDPSession(page)
  await cdp.send('WebAuthn.enable')
  await cdp.send('WebAuthn.addVirtualAuthenticator', {
    options: {
      protocol: 'ctap2',
      transport: 'internal',
      hasResidentKey: true,
      hasUserVerification: true,
      isUserVerified: true,
      automaticPresenceSimulation: true,
    },
  })
}

/**
 * 系统参数 identity.login-methods 列出 passkey（库在本机保留，参数在即补上 passkey）
 * 这一步只是准备，经接口做；要测的是个人中心与登录页
 */
async function openPasskeys(page: Page) {
  const token = await page.evaluate(() => (JSON.parse(localStorage.getItem('xihan.next.session') ?? '{}') as { accessToken?: string }).accessToken)
  const headers = { Authorization: `Bearer ${token}` }
  const listed = await page.request.post('/api/configs:list', {
    headers,
    data: {
      keyword: LOGIN_METHODS,
      page: { index: 1, size: 20 },
    },
  })
  expect(listed.ok()).toBe(true)
  const items = (await listed.json() as { items: { id: string, key: string, name: string, dataType: string, value?: null | string }[] }).items
  const existing = items.find(item => item.key === LOGIN_METHODS)
  if (!existing) {
    const created = await page.request.post('/api/configs', {
      headers,
      data: {
        key: LOGIN_METHODS,
        name: '登录方式',
        dataType: 'Text',
        value: 'passkey',
      },
    })
    expect(created.status()).toBe(201)
    return
  }
  const methods = (existing.value ?? '').split(',').map(method => method.trim()).filter(Boolean)
  if (methods.includes('passkey'))
    return
  const updated = await page.request.put(`/api/configs/${existing.id}`, {
    headers,
    data: {
      name: existing.name,
      dataType: existing.dataType,
      value: [...methods, 'passkey'].join(','),
    },
  })
  expect(updated.ok()).toBe(true)
}

async function openSecurity(page: Page) {
  await page.goto('/#/profile')
  await page.getByRole('button', { name: '安全设置' }).click()
  await expect(page.getByText('通行密钥', { exact: true })).toBeVisible()
}

// 超管在安全设置里添加通行密钥（先核对当前密码），退出后在登录页不填账号即可登录；最后删掉，库里不留
test('通行密钥：添加之后不填账号即可登录', async ({ page }) => {
  const name = `E2E ${RUN}`
  await useVirtualAuthenticator(page)
  await login(page, SUPER_ADMIN)
  await openPasskeys(page)

  await openSecurity(page)
  await page.getByRole('button', { name: '添加', exact: true }).click()
  const dialog = page.getByRole('dialog', { name: '添加通行密钥' })
  await dialog.getByRole('textbox', { name: '名称' }).fill(name)
  await dialog.getByRole('textbox', { name: '当前密码', exact: true }).fill(SUPER_ADMIN.password)
  await dialog.getByRole('button', { name: '继续' }).click()
  await expectToast(page, '通行密钥已添加')
  const row = page.locator('.pf-list-item').filter({ hasText: name })
  await expect(row).toBeVisible()

  await page.getByRole('banner').getByRole('button', { name: '超级管理员' }).click()
  await page.getByRole('menuitem', { name: '退出登录' }).click()
  await expect(page).toHaveURL(/#\/auth\/login/)
  await page.getByRole('button', { name: '使用通行密钥登录' }).click()
  await expectWorkbench(page)

  await openSecurity(page)
  await row.getByRole('button', { name: '删除' }).click()
  await page.getByRole('button', { name: '确认' }).click()
  await expectToast(page, '通行密钥已删除')
  await expect(row).toHaveCount(0)
})
