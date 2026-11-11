import { expect, test } from '@playwright/test'
import { SUPER_ADMIN } from './env.ts'
import { login, submitLogin } from './support.ts'

test('超管登录进入工作台，退出回到登录页', async ({ page }) => {
  await login(page, SUPER_ADMIN)
  await expect(page.getByRole('heading', { name: '欢迎使用 XiHan BasicApp' })).toBeVisible()

  await page.getByRole('banner').getByRole('button', { name: '超级管理员' }).click()
  await page.getByRole('menuitem', { name: '退出登录' }).click()
  await expect(page).toHaveURL(/#\/auth\/login/)
  await expect(page.getByRole('button', { name: '登录', exact: true })).toBeVisible()
})

test('密码错误时停在登录页并提示', async ({ page }) => {
  // 用不存在的账号，不累加真实账号的失败计数
  await submitLogin(page, { username: `e2e-probe-${Date.now().toString(36)}`, password: 'Wrong-Password@1' })
  await expect(page.getByText('用户名或密码不正确').first()).toBeVisible()
  await expect(page).toHaveURL(/#\/auth\/login/)
})
