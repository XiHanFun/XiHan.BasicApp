<script lang="ts" setup>
import type { FormInst, FormRules } from 'naive-ui'
import { Icon } from '@iconify/vue'
import { NButton, NCheckbox, NForm, NFormItem, NIcon, NInput, useMessage } from 'naive-ui'
import { computed, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useTheme } from '~/hooks'

defineOptions({ name: 'RegisterPage' })

const { isDark } = useTheme()
const { t } = useI18n()
const router = useRouter()
const message = useMessage()
const formRef = ref<FormInst | null>(null)
const loading = ref(false)
const showPassword = ref(false)
const showConfirmPassword = ref(false)
const agreePolicy = ref(false)

const formData = ref({
  username: '',
  password: '',
  confirmPassword: '',
})

const passwordStrength = computed(() => {
  const pwd = formData.value.password
  if (!pwd) return 0
  let score = 0
  if (pwd.length >= 8) score++
  if (/[a-z]/.test(pwd) && /[A-Z]/.test(pwd)) score++
  if (/\d/.test(pwd)) score++
  if (/[^a-zA-Z0-9]/.test(pwd)) score++
  return score
})

const strengthLabel = computed(() => {
  const labels = [
    t('page.auth.strength_weak'),
    t('page.auth.strength_weak'),
    t('page.auth.strength_medium'),
    t('page.auth.strength_strong'),
    t('page.auth.strength_very_strong'),
  ]
  return labels[passwordStrength.value] || ''
})

const strengthColor = computed(() => {
  const colors = ['#e53e3e', '#e53e3e', '#dd6b20', '#38a169', '#2b6cb0']
  return colors[passwordStrength.value] || '#e53e3e'
})

const rules: FormRules = {
  username: [
    { required: true, message: () => t('page.login.username_placeholder'), trigger: 'blur' },
    { min: 3, message: () => t('page.auth.username_min_length'), trigger: 'blur' },
  ],
  password: [
    { required: true, message: () => t('page.login.password_placeholder'), trigger: 'blur' },
    { min: 6, message: () => t('page.auth.password_min_length'), trigger: 'blur' },
  ],
  confirmPassword: [
    { required: true, message: () => t('page.auth.confirm_password_placeholder'), trigger: 'blur' },
    {
      validator: (_rule, value) => {
        if (value !== formData.value.password) {
          return new Error(t('page.auth.password_mismatch'))
        }
        return true
      },
      trigger: 'blur',
    },
  ],
}

async function handleRegister() {
  try {
    await formRef.value?.validate()
    if (!agreePolicy.value) {
      message.warning(t('page.auth.agree_required'))
      return
    }
    loading.value = true
    // TODO: call register API
    message.success(t('page.auth.register_success'))
    router.push('/auth/login')
  }
  catch {
    // validation failed
  }
  finally {
    loading.value = false
  }
}

function handleKeydown(e: KeyboardEvent) {
  if (e.key === 'Enter') handleRegister()
}
</script>

<template>
  <div>
    <h1 class="mb-1 text-2xl font-bold">
      {{ t('page.auth.create_account_title') }}
    </h1>
    <p
      class="mb-5 text-sm"
      :class="isDark ? 'text-gray-400' : 'text-[hsl(var(--muted-foreground))]'"
    >
      {{ t('page.auth.register_subtitle') }}
    </p>

    <NForm
      ref="formRef"
      :model="formData"
      :rules="rules"
      label-placement="top"
      size="medium"
      :show-label="false"
      @keydown="handleKeydown"
    >
      <NFormItem path="username" :show-feedback="false" class="!mb-5">
        <NInput
          v-model:value="formData.username"
          :placeholder="t('page.login.username_placeholder')"
          :input-props="{ autocomplete: 'username' }"
        />
      </NFormItem>
      <NFormItem path="password" :show-feedback="false" class="!mb-2">
        <NInput
          v-model:value="formData.password"
          :type="showPassword ? 'text' : 'password'"
          :placeholder="t('page.login.password_placeholder')"
          :input-props="{ autocomplete: 'new-password' }"
        >
          <template #suffix>
            <NIcon
              class="cursor-pointer"
              :class="isDark ? 'text-gray-400' : 'text-[hsl(var(--muted-foreground))]'"
              @click="showPassword = !showPassword"
            >
              <Icon :icon="showPassword ? 'lucide:eye-off' : 'lucide:eye'" width="16" />
            </NIcon>
          </template>
        </NInput>
      </NFormItem>

      <!-- Password strength -->
      <div v-if="formData.password" class="mb-5 flex items-center gap-2">
        <div class="flex flex-1 gap-1">
          <div
            v-for="i in 4"
            :key="i"
            class="h-1 flex-1 rounded-full transition-colors"
            :style="{ backgroundColor: i <= passwordStrength ? strengthColor : (isDark ? '#374151' : '#e5e7eb') }"
          />
        </div>
        <span class="text-xs" :style="{ color: strengthColor }">{{ strengthLabel }}</span>
      </div>
      <div v-else class="mb-3" />

      <NFormItem path="confirmPassword" :show-feedback="false" class="!mb-5">
        <NInput
          v-model:value="formData.confirmPassword"
          :type="showConfirmPassword ? 'text' : 'password'"
          :placeholder="t('page.auth.confirm_password_placeholder')"
          :input-props="{ autocomplete: 'new-password' }"
        >
          <template #suffix>
            <NIcon
              class="cursor-pointer"
              :class="isDark ? 'text-gray-400' : 'text-[hsl(var(--muted-foreground))]'"
              @click="showConfirmPassword = !showConfirmPassword"
            >
              <Icon :icon="showConfirmPassword ? 'lucide:eye-off' : 'lucide:eye'" width="16" />
            </NIcon>
          </template>
        </NInput>
      </NFormItem>

      <div class="mb-5">
        <NCheckbox v-model:checked="agreePolicy">
          <span class="text-sm">
            {{ t('page.auth.agree_text') }}
            <a class="link-primary" href="#">
              {{ t('page.auth.privacy_policy') }}
            </a>
            {{ t('page.auth.and') }}
            <a class="link-primary" href="#">
              {{ t('page.auth.terms_of_service') }}
            </a>
          </span>
        </NCheckbox>
      </div>

      <NButton type="primary" block :loading="loading" @click="handleRegister">
        {{ t('page.auth.register_btn') }}
      </NButton>
    </NForm>

    <p
      class="mt-4 text-center text-sm"
      :class="isDark ? 'text-gray-400' : 'text-[hsl(var(--muted-foreground))]'"
    >
      {{ t('page.auth.already_have_account') }}
      <span class="link-primary cursor-pointer" @click="router.push('/auth/login')">
        {{ t('page.auth.go_to_login') }}
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
