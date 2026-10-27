<script lang="ts" setup>
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import { useLogin } from './use-login'

defineOptions({ name: 'IdentityLoginView' })

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const login = useLogin()

async function submit() {
  if (!await login.submit())
    return
  // 只回跳站内路径，避免开放重定向
  const redirect = typeof route.query.redirect === 'string' && route.query.redirect.startsWith('/') && !route.query.redirect.startsWith('//') ? route.query.redirect : '/'
  await router.replace(redirect)
}
</script>

<template>
  <section class="w-full max-w-sm rounded-surface border border-border bg-surface p-6 shadow-sm" data-testid="login">
    <h1 class="text-heading-3 font-semibold">
      {{ t('identity.login.title') }}
    </h1>
    <p class="mt-1 text-caption text-fg-muted">
      {{ t('identity.login.description') }}
    </p>
    <!-- 每个字段有可见标签；校验以后端为准（novalidate 关掉浏览器原生校验），Schema 的字段级 errors 落到对应字段，其余错误整体显示 -->
    <form class="mt-4 grid gap-3" novalidate @submit.prevent="submit">
      <XhFieldRoot :label="t('identity.login.fields.tenant')" :error-text="login.fieldError('tenant')" :invalid="!!login.fieldError('tenant')">
        <template #control>
          <XhTextFieldRoot v-model:value="login.form.tenant" name="tenant" autocomplete="organization" :placeholder="t('identity.login.tenant_placeholder')" />
        </template>
      </XhFieldRoot>
      <XhFieldRoot :label="t('identity.login.fields.user_name')" :error-text="login.fieldError('userName')" :invalid="!!login.fieldError('userName')" required>
        <template #control>
          <XhTextFieldRoot v-model:value="login.form.userName" name="userName" autocomplete="username" />
        </template>
      </XhFieldRoot>
      <XhFieldRoot :label="t('identity.login.fields.password')" :error-text="login.fieldError('password')" :invalid="!!login.fieldError('password')" required>
        <template #control>
          <XhTextFieldRoot v-model:value="login.form.password" name="password" type="password" autocomplete="current-password" />
        </template>
      </XhFieldRoot>
      <p v-if="login.error.value" class="text-caption text-fg-danger" role="alert">
        {{ login.error.value }}
      </p>
      <XhButton type="submit" variant="solid" :loading="login.submitting.value" :disabled="login.submitting.value">
        {{ t('identity.login.actions.sign_in') }}
      </XhButton>
    </form>
  </section>
</template>
