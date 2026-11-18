/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import type { ResponseOf } from '~/contracts'
import { reactive, ref, shallowRef } from 'vue'
import { site as siteApi } from '~/contracts'
import { ApiError, endpointUrl, uploadForm } from '~/request'
import { useSiteStore } from '~/stores'

export type SiteSettings = ResponseOf<'site.get'>
export type SiteProfile = SiteSettings['own']
export type SiteAsset = NonNullable<SiteProfile['logo']>

/** 文字字段：表单里都是字符串，空串即没设 */
export const SITE_TEXT_FIELDS = [
  'name',
  'subtitle',
  'description',
  'copyrightOwner',
  'copyrightUrl',
  'copyrightSince',
  'icp',
  'icpUrl',
] as const
export type SiteTextField = typeof SITE_TEXT_FIELDS[number]

/** 图片槽位：表单字段与上传端点的路径段 */
export const SITE_ASSET_SLOTS = [
  { field: 'logo', slot: 'logo' },
  { field: 'favicon', slot: 'favicon' },
] as const
export type SiteAssetField = typeof SITE_ASSET_SLOTS[number]['field']

/** 上传端点收的图片 */
export const SITE_ASSET_ACCEPT = 'image/png,image/jpeg,image/webp,image/x-icon,.ico'

/** 站点图片的地址（站内相对路径按接口源补全） */
export function siteAssetUrl(asset: null | SiteAsset | undefined): null | string {
  return asset
    ? endpointUrl(asset.url)
    : null
}

function blankToNull(value: string): null | string {
  return value.trim() === ''
    ? null
    : value.trim()
}

/**
 * 站点设置页：平台改站点设置，租户改自己的品牌（没设的沿用平台的）
 * 文字一起保存；图片选了即上传换上、去掉即保存，旧图由后端回收；改完刷新壳层的站点设置
 */
export function useSiteSettings() {
  const site = useSiteStore()
  const loading = ref(false)
  const saving = ref(false)
  const uploading = ref<null | SiteAssetField>(null)
  const loadError = ref<null | string>(null)
  const submitError = ref<null | string>(null)
  const fieldErrors = ref<Record<string, string[]>>({})
  const settings = shallowRef<null | SiteSettings>(null)
  const form = reactive<Record<SiteTextField, string>>(Object.fromEntries(SITE_TEXT_FIELDS.map(field => [field, ''])) as Record<SiteTextField, string>)

  function fill(next: SiteSettings): void {
    settings.value = next
    for (const field of SITE_TEXT_FIELDS)
      form[field] = next.own[field] ?? ''
  }

  function fieldError(field: string): string | undefined {
    return fieldErrors.value[field]?.[0]
  }

  /** 租户没设时沿用的平台值（输入框的占位） */
  function inherited(field: SiteTextField): string {
    return settings.value?.inherited?.[field] ?? ''
  }

  async function load(): Promise<void> {
    loading.value = true
    loadError.value = null
    try {
      fill(await siteApi.get({}))
    }
    catch (error) {
      loadError.value = (error as Error).message
    }
    finally {
      loading.value = false
    }
  }

  function payload(images: Partial<Record<SiteAssetField, null | string>> = {}) {
    const own = settings.value?.own
    return {
      ...Object.fromEntries(SITE_TEXT_FIELDS.map(field => [
        field,
        blankToNull(form[field]),
      ])) as Record<SiteTextField, null | string>,
      logo: images.logo === undefined
        ? own?.logo?.id ?? null
        : images.logo,
      favicon: images.favicon === undefined
        ? own?.favicon?.id ?? null
        : images.favicon,
    }
  }

  async function run(action: () => Promise<SiteSettings>): Promise<boolean> {
    submitError.value = null
    fieldErrors.value = {}
    try {
      fill(await action())
      await site.load()
      return true
    }
    catch (error) {
      if (error instanceof ApiError) {
        fieldErrors.value = error.errors ?? {}
        if (!error.hasFieldErrors)
          submitError.value = error.detail
      }
      else {
        submitError.value = String(error)
      }
      return false
    }
  }

  async function save(): Promise<boolean> {
    saving.value = true
    try {
      return await run(() => siteApi.update(payload()))
    }
    finally {
      saving.value = false
    }
  }

  /** 选了图片即上传换上（文字的改动不随之保存） */
  async function upload(field: SiteAssetField, file: File): Promise<boolean> {
    const slot = SITE_ASSET_SLOTS.find(item => item.field === field)!.slot
    const body = new FormData()
    body.append('file', file)
    uploading.value = field
    try {
      return await run(() => uploadForm<SiteSettings>(`/api/site/assets/${slot}`, body))
    }
    finally {
      uploading.value = null
    }
  }

  /** 去掉一张图片：文字按上次保存的交回，只改这一张 */
  async function remove(field: SiteAssetField): Promise<boolean> {
    const own = settings.value?.own
    if (!own)
      return false
    uploading.value = field
    try {
      return await run(() => siteApi.update({
        ...Object.fromEntries(SITE_TEXT_FIELDS.map(name => [name, own[name] ?? null])) as Record<SiteTextField, null | string>,
        logo: field === 'logo'
          ? null
          : own.logo?.id ?? null,
        favicon: field === 'favicon'
          ? null
          : own.favicon?.id ?? null,
      }))
    }
    finally {
      uploading.value = null
    }
  }

  return {
    loading,
    saving,
    uploading,
    loadError,
    submitError,
    settings,
    form,
    fieldError,
    inherited,
    load,
    save,
    upload,
    remove,
  }
}
