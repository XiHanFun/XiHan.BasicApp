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
  <!-- 账号登录表单：入口标签带由认证壳（AuthEntrySwitcher）按登录设置给，这里只有提示语与字段；字段靠占位文案表意，标签只留给读屏 -->
  <section class="login" data-testid="login">
    <p class="login__subtitle">
      {{ t('identity.login.subtitle') }}
    </p>
    <!-- 校验以后端为准（novalidate 关掉浏览器原生校验），Schema 的字段级 errors 落到对应字段，其余错误整体显示 -->
    <form class="login__form" novalidate @submit.prevent="submit">
      <XhFieldRoot :label="t('identity.login.fields.tenant')" :error-text="login.fieldError('tenant')" :invalid="!!login.fieldError('tenant')" class="login__field">
        <template #control>
          <XhTextFieldRoot v-model:value="login.form.tenant" name="tenant" size="lg" autocomplete="organization" :placeholder="t('identity.login.tenant_placeholder')" />
        </template>
      </XhFieldRoot>
      <XhFieldRoot :label="t('identity.login.fields.user_name')" :error-text="login.fieldError('userName')" :invalid="!!login.fieldError('userName')" required class="login__field">
        <template #control>
          <XhTextFieldRoot v-model:value="login.form.userName" name="userName" size="lg" autocomplete="username" :placeholder="t('identity.login.user_name_placeholder')" />
        </template>
      </XhFieldRoot>
      <XhFieldRoot :label="t('identity.login.fields.password')" :error-text="login.fieldError('password')" :invalid="!!login.fieldError('password')" required class="login__field">
        <template #control>
          <XhTextFieldRoot v-model:value="login.form.password" name="password" type="password" size="lg" autocomplete="current-password" :placeholder="t('identity.login.password_placeholder')" />
        </template>
      </XhFieldRoot>
      <p v-if="login.error.value" class="login__error" role="alert">
        {{ login.error.value }}
      </p>
      <XhButton type="submit" variant="solid" tone="brand" size="lg" full-width class="login__submit" :loading="login.submitting.value" :disabled="login.submitting.value">
        {{ t('identity.login.actions.sign_in') }}
      </XhButton>
    </form>
  </section>
</template>

<style scoped>
.login {
  display: flex;
  flex-direction: column;
  padding-block: var(--xh-space-1);
}
.login__subtitle {
  margin: 0 0 var(--xh-space-8);
  color: hsl(var(--muted-foreground));
  font-size: var(--xh-text-body-size);
}
.login__form {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-6);
}
/* 标签只给读屏：视觉上由占位文案表意 */
.login__field :deep([data-scope='field'][data-part='label']) {
  position: absolute;
  inline-size: 1px;
  block-size: 1px;
  margin: -1px;
  overflow: hidden;
  clip-path: inset(50%);
  white-space: nowrap;
}
.login__error {
  margin: 0;
  color: hsl(var(--destructive));
  font-size: var(--xh-text-caption-size);
}
.login__submit {
  margin-block-start: var(--xh-space-2);
}
</style>
