/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import { ref } from 'vue'
import { permissions as permissionsApi } from '~/contracts'
import { useAppStore, useAuthStore } from '~/stores'

/** 取名函数：权限码 → 显示名 */
export type PermissionNameOf = (code: string) => string

/**
 * 权限码 → 当前界面语言的显示名，同一语言只取一次
 * 生成物的权限定义表只有英文，只拿到权限码的地方按权限目录换名字
 * 没有读权限目录的权限、或码不在本侧目录里（套餐收窄、另一侧的码）时原样显示权限码
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
