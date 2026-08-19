<script lang="ts" setup>
import type { FormRules } from '@xihan-ui/headless'
import { XhFieldControl, XhFieldErrorText, XhFieldRoot, XhFormFieldGroup, XhFormRoot, XhFormSubmitTrigger } from '@xihan-ui/vue'

import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { XInput } from '~/components'
import { toast } from '~/composables'
import { LOGIN_PATH } from '~/constants'
import { useTheme } from '~/hooks'
import { useAppContext } from '~/stores'

defineOptions({ name: 'ForgetPasswordPage' })

const { isDark } = useTheme()
const { t } = useI18n()
const router = useRouter()
const { apis } = useAppContext()
const loading = ref(false)

const formData = ref({
  email: '',
})

const rules = computed<FormRules>(() => ({
  email: [
    { required: true, message: t('page.auth.email_placeholder') },
    { type: 'email', message: t('page.auth.email_invalid') },
  ],
}))

async function onSubmit() {
  try {
    loading.value = true
    const result = await apis.requestPasswordResetApi(formData.value.email)
    if (result.debugResetUrl) {
      // 开发环境（未配 SMTP）回显重置链接，便于本地联调
      toast.success(`${t('page.auth.reset_link_sent')}（重置链接：${result.debugResetUrl}）`)
    }
    else {
      toast.success(t('page.auth.reset_link_sent'))
    }
  }
  finally {
    loading.value = false
  }
}

function handleKeydown(e: KeyboardEvent) {
  if (e.key === 'Enter')
    onSubmit()
}
</script>

<template>
  <div class="py-1">
    <div class="mb-8">
      <h1 class="text-[32px] font-semibold leading-tight sm:text-[36px]">
        {{ t('page.auth.forget_password_title') }}
      </h1>
      <p
        class="mt-3 text-[15px] leading-7"
        :class="isDark ? 'text-gray-300' : 'text-[hsl(var(--muted-foreground))]'"
      >
        {{ t('page.auth.forget_password_subtitle') }}
      </p>
    </div>

    <XhFormRoot
      v-model:values="formData"
      :rules="rules"
      validate-on="blur"
      @keydown="handleKeydown"
      @submit="onSubmit"
    >
      <XhFormFieldGroup value="email" class="!mb-6">
        <XhFieldRoot>
          <XhFieldControl>
            <XInput
              v-model:value="formData.email"
              size="lg"
              placeholder="example@example.com"
              autocomplete="email"
            />
          </XhFieldControl>
          <XhFieldErrorText />
        </XhFieldRoot>
      </XhFormFieldGroup>

      <XhFormSubmitTrigger class="!h-12 !rounded-xl !text-[15px] !font-semibold" :disabled="loading">
        {{ t('page.auth.send_reset_link') }}
      </XhFormSubmitTrigger>
    </XhFormRoot>

    <p
      class="mt-6 text-sm text-center"
      :class="isDark ? 'text-gray-400' : 'text-[hsl(var(--muted-foreground))]'"
    >
      <span class="cursor-pointer link-primary" @click="router.push(LOGIN_PATH)">
        {{ t('page.auth.back_to_login') }}
      </span>
    </p>
  </div>
</template>

<style scoped>
.link-primary {
  color: hsl(var(--primary));
}

.link-primary:hover {
  text-decoration: underline;
}
</style>
