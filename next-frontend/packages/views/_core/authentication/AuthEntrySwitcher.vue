<script lang="ts" setup>
import type { TabsNode } from '@xihan-ui/headless'
import { XhTabsRoot } from '@xihan-ui/vue'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import { CODE_LOGIN_PATH, EMAIL_LOGIN_PATH, LOGIN_PATH, QRCODE_LOGIN_PATH } from '~/constants'

defineOptions({ name: 'AuthEntrySwitcher' })

const route = useRoute()
const router = useRouter()
const { t } = useI18n()

const entryList = computed(() => [
  { path: LOGIN_PATH, label: t('page.login.title') },
  { path: CODE_LOGIN_PATH, label: t('page.auth.mobile_login') },
  { path: EMAIL_LOGIN_PATH, label: t('page.auth.email_login') },
  { path: QRCODE_LOGIN_PATH, label: t('page.auth.qrcode_login') },
])

const activePath = computed(() => {
  const path = route.path
  return entryList.value.some(item => item.path === path) ? path : LOGIN_PATH
})

const tabCollection = computed<TabsNode[]>(() =>
  entryList.value.map(item => ({ value: item.path, label: item.label })),
)

function goTo(path: string) {
  if (route.path === path)
    return
  router.push(path)
}
</script>

<template>
  <!-- 只有一排标签、没有面板：collection 铺开即可，面板插槽不给 -->
  <XhTabsRoot
    class="entry-switcher"
    variant="line"
    :collection="tabCollection"
    :value="activePath"
    @update:value="(next: string | null) => next && goTo(next)"
  />
</template>

<style scoped>
.entry-switcher {
  width: 100%;
}

.entry-switcher :deep([data-scope='tabs'][data-part='list']) {
  margin-bottom: 0 !important;
}

.entry-switcher :deep([data-scope='tabs'][data-part='trigger']) {
  font-size: 15px;
  font-weight: 500;
  color: hsl(var(--muted-foreground)) !important;
}

.entry-switcher :deep([data-scope='tabs'][data-part='trigger']:hover:not([data-state='active'])) {
  color: hsl(var(--foreground)) !important;
}

.entry-switcher :deep([data-scope='tabs'][data-part='trigger'][data-state='active']) {
  color: hsl(var(--primary)) !important;
  font-weight: 600;
}

/* 选中下划线：line 档只换文字色，选中态与未选中只差一档灰，这里把指示条补回来 */
.entry-switcher :deep([data-scope='tabs'][data-part='trigger'][data-state='active'])::after {
  content: '';
  position: absolute;
  inset-inline: 0;
  inset-block-end: calc(-1 * var(--xh-stroke-thin));
  block-size: 2px;
  border-radius: 1px;
  background: hsl(var(--primary));
}

.entry-switcher :deep([data-scope='tabs'][data-part='content']) {
  display: none;
}
</style>
