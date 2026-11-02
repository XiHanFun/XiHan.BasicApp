import { ref } from 'vue'
import { permissions as permissionsApi } from '~/contracts'
import { useAppStore, useAuthStore } from '~/stores'

/** 取名函数：权限码 → 显示名 */
export type PermissionNameOf = (code: string) => string

/**
 * 权限的区域显示名：权限码 → 当前界面语言的显示名。
 *
 * 取的是权限目录（permissions.list，后端按 Accept-Language 给显示名）；生成物里的权限定义表只有英文，
 * 角色详情、委托、授权条件这些只拿到权限码的地方都按它换名字。同一界面语言只取一次，换了语言再取；
 * 没有读权限目录的权限、或码不在本侧目录里（套餐收窄后不再列出、另一侧的码）时原样显示权限码。
 */
export function usePermissionNames() {
  const names = ref<ReadonlyMap<string, string>>(new Map())
  let loadedLocale: null | string = null

  async function load(): Promise<void> {
    const locale = useAppStore().locale
    if (loadedLocale === locale)
      return
    if (!useAuthStore().can('access.permissions.read')) {
      names.value = new Map()
      return
    }
    const catalog = await permissionsApi.list({})
    names.value = new Map(catalog.items.map(item => [item.code, item.displayName]))
    loadedLocale = locale
  }

  const nameOf: PermissionNameOf = code => names.value.get(code) ?? code

  return { names, load, nameOf }
}
