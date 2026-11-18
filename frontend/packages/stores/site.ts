/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import type { ResponseOf } from '~/contracts'
import { defineStore } from 'pinia'
import { computed, ref, shallowRef } from 'vue'
import { site as siteApi } from '~/contracts'
import { endpointUrl } from '~/request'
import { SetupStoreId } from './store-ids'

/** 生效的站点设置（后端 site.resolve） */
export type SiteProfile = ResponseOf<'site.resolve'>['profile']

/** 站点设置里没有系统名时的名称 */
export const DEFAULT_SITE_NAME = 'XiHan.BasicApp'

/** 站点设置里没有 Logo 时用前端自带的图标 */
export const DEFAULT_SITE_LOGO = '/favicon.png'

function assetUrl(asset: { url: string } | null | undefined): null | string {
  return asset
    ? endpointUrl(asset.url)
    : null
}

/** 把浏览器图标换成站点的 */
function applyFavicon(href: string): void {
  if (typeof document === 'undefined')
    return
  let link = document.querySelector<HTMLLinkElement>('link[rel~="icon"]')
  if (!link) {
    link = document.createElement('link')
    link.rel = 'icon'
    document.head.appendChild(link)
  }
  if (link.getAttribute('href') !== href)
    link.href = href
}

/**
 * 站点设置与租户品牌：系统名、Logo、浏览器图标、登录页与页脚
 * 后端是真源：启动时与登录态、所在租户变化时取 site.resolve（没登录按访问的域名找租户，只用于展示与预填租户框）
 */
export const useSiteStore = defineStore(SetupStoreId.Site, () => {
  const profile = shallowRef<null | SiteProfile>(null)
  /** 没登录时按域名找到的租户，登录页预填进租户框 */
  const tenantId = ref<null | string>(null)

  const name = computed(() => profile.value?.name || DEFAULT_SITE_NAME)
  const subtitle = computed(() => profile.value?.subtitle ?? null)
  const description = computed(() => profile.value?.description ?? null)
  const logo = computed(() => assetUrl(profile.value?.logo) ?? DEFAULT_SITE_LOGO)
  const favicon = computed(() => assetUrl(profile.value?.favicon) ?? logo.value)
  const copyright = computed(() => ({
    owner: profile.value?.copyrightOwner ?? null,
    url: profile.value?.copyrightUrl ?? null,
    since: profile.value?.copyrightSince ?? null,
    icp: profile.value?.icp ?? null,
    icpUrl: profile.value?.icpUrl ?? null,
  }))

  /** 用取到的设置替换当前的，并换上浏览器图标 */
  function apply(resolved: ResponseOf<'site.resolve'>): void {
    profile.value = resolved.profile
    tenantId.value = resolved.tenantId ?? null
    applyFavicon(favicon.value)
  }

  /** 取生效的站点设置；登录了按令牌里的租户，没登录按当前访问的主机名 */
  async function load(): Promise<void> {
    apply(await siteApi.resolve({ host: window.location.host }))
  }

  return {
    profile,
    tenantId,
    name,
    subtitle,
    description,
    logo,
    favicon,
    copyright,
    apply,
    load,
  }
})
