import { defineStore } from 'pinia'
import { createLayoutSlice } from './app/layout'
import { createPreferencesSlice } from './app/preferences'
import { createThemeSlice } from './app/theme'
import { SetupStoreId } from './store-ids'

/**
 * 全局应用 Store（组合门面）。
 * 内部按领域拆分为 theme / layout / preferences 三个 slice，
 * 对外保持原有 API 不变，消费者无需修改导入。
 */
export const useAppStore = defineStore(SetupStoreId.App, () => {
  const theme = createThemeSlice()
  const layout = createLayoutSlice()
  const preferences = createPreferencesSlice()

  return {
    ...theme,
    ...layout,
    ...preferences,
  }
})
