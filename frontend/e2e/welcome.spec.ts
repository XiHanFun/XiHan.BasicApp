import { expect, test } from '@playwright/test'
import { INITIAL_PASSWORD, SUPER_ADMIN } from './env.ts'
import { createUser, login, RUN, submitLogin, welcomeLayer, welcomeSaved, welcomeSeen } from './support.ts'

// 新版欢迎页：没看过这条版本线的账号登录后出现一次，看完记到账号上，再进来不再出现；之后在「关于」里可以重看
// 后一步用前一步建的账号，串行跑，前一步失败后面跳过
test.describe.configure({ mode: 'serial' })

const newcomer = {
  username: `welcome-${RUN}`,
  displayName: `E2E 欢迎 ${RUN}`,
  email: `welcome-${RUN}@e2e.test`,
  password: INITIAL_PASSWORD,
}

function pad(value: number): string {
  return String(value).padStart(2, '0')
}

test('超管建一个没看过欢迎页的账号', async ({ page }) => {
  await login(page, SUPER_ADMIN)
  await createUser(page, newcomer)
})

test('新账号首次登录看到欢迎页，一路看完之后再进来不再出现', async ({ page }) => {
  await submitLogin(page, newcomer)
  await expect(page).toHaveURL(/#\/workbench\/dashboard/)
  expect(await welcomeSeen(page)).toBe(false)
  const welcome = welcomeLayer(page)
  await expect(welcome).toBeVisible()

  // 各国问候还在聚散：回车直接到欢迎主画面
  await page.keyboard.press('Enter')
  await expect(welcome.getByRole('heading', { name: /^欢迎使用/ })).toBeVisible()
  await welcome.getByRole('button', { name: '看看新变化' }).click()

  // 亮点导览：有几屏取决于这个账号看得到哪些模块，从页码里读出来，用方向键一屏屏翻完
  await expect(welcome.getByRole('heading', { name: '界面，按你的习惯来' })).toBeVisible()
  const counter = await welcome.getByText(/^01 \/ \d{2}$/).textContent()
  const total = Number(counter?.trim().slice(-2))
  expect(total).toBeGreaterThan(0)
  for (let index = 2; index <= total; index++) {
    await page.keyboard.press('ArrowRight')
    await expect(welcome.getByText(`${pad(index)} / ${pad(total)}`, { exact: true })).toBeVisible()
  }
  await welcome.getByRole('button', { name: '完成' }).click()

  // 收尾：开始使用，欢迎页退场，「看过」记到账号上
  await expect(welcome.getByRole('heading', { name: '一切就绪' })).toBeVisible()
  const saved = welcomeSaved(page)
  await welcome.getByRole('button', { name: '开始使用' }).click()
  await expect(welcome).toBeHidden()
  expect((await saved).ok()).toBe(true)
  await expect(page.getByRole('navigation', { name: '侧边栏' })).toBeVisible()

  // 再进来：读到看过，不再出现
  await page.reload()
  expect(await welcomeSeen(page)).toBe(true)
  await expect(page.getByRole('navigation', { name: '侧边栏' })).toBeVisible()
  await expect(welcome).toBeHidden()
})

test('看过之后在「关于」里重看，关掉后焦点回到入口', async ({ page }) => {
  await login(page, SUPER_ADMIN)
  await page.goto('/#/about/project')
  const replay = page.getByRole('button', { name: '新版亮点' })
  await replay.click()

  const welcome = welcomeLayer(page)
  await expect(welcome).toBeVisible()
  await page.keyboard.press('Escape')
  await expect(welcome).toBeHidden()
  await expect(replay).toBeFocused()
})
