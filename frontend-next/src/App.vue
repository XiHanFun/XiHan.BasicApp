<script lang="ts" setup>
import type { Locale } from '~/locales'
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { RouterView } from 'vue-router'
import { LOCALES } from '~/locales'
import { useAppStore, useAuthStore } from '~/stores'

defineOptions({ name: 'App' })

const { t } = useI18n()
const auth = useAuthStore()
const app = useAppStore()
const tokenDraft = ref(auth.token ?? '')
const subject = computed(() => auth.claims?.sub ?? '')
const tenant = computed(() => auth.claims?.tenant ?? '')

function applyToken() {
  auth.setToken(tokenDraft.value)
}

function onLocaleChange(event: Event) {
  const value = (event.target as HTMLSelectElement).value as Locale
  void app.setLocale(value)
}
</script>

<template>
  <div class="flex min-h-full flex-col bg-page text-fg">
    <!-- N0 的开发会话栏：粘贴 pnpm dev:token 签出的令牌；N1 的登录页接入后移除 -->
    <header class="flex flex-wrap items-center gap-3 border-b border-border bg-surface px-4 py-2">
      <span class="text-label font-semibold">{{ t('common.app_name') }}</span>
      <span class="text-caption text-fg-muted">{{ t('common.version', { version: '6.0.0-preview.1' }) }}</span>
      <form class="ml-auto flex items-center gap-2" @submit.prevent="applyToken">
        <label class="text-caption text-fg-muted" for="dev-token">{{ t('common.session.token') }}</label>
        <XhTextFieldRoot id="dev-token" v-model:value="tokenDraft" class="w-72" size="sm" :placeholder="t('common.session.token_placeholder')" clearable />
        <XhButton type="submit" size="sm" variant="solid">
          {{ t('common.session.apply') }}
        </XhButton>
        <span v-if="subject" class="text-caption text-fg-muted">{{ t('common.session.signed_in', { subject, tenant: tenant || '-' }) }}</span>
      </form>
      <label class="text-caption text-fg-muted" for="locale">{{ t('common.locale') }}</label>
      <select id="locale" class="rounded-control border border-border bg-surface px-2 py-1 text-caption text-fg" :value="app.locale" @change="onLocaleChange">
        <option v-for="locale in LOCALES" :key="locale" :value="locale">
          {{ locale }}
        </option>
      </select>
    </header>
    <main class="flex-1 p-4">
      <RouterView />
    </main>
  </div>
</template>
