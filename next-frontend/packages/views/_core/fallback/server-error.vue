<script lang="ts" setup>
import { XhButton, XhResultAction, XhResultDescription, XhResultIcon, XhResultRoot, XhResultTitle } from '@xihan-ui/vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { Icon } from '~/iconify'
import { goHome } from './use-fallback-actions'

defineOptions({ name: 'ServerErrorPage' })

const router = useRouter()
const { t } = useI18n()
</script>

<template>
  <div class="flex-col-center h-full min-h-[400px]">
    <XhResultRoot status="500">
      <XhResultIcon>
        <Icon icon="lucide:server-crash" width="48" height="48" />
      </XhResultIcon>
      <XhResultTitle>{{ t('error.server_error') }}</XhResultTitle>
      <XhResultDescription>{{ t('error.server_error_desc') }}</XhResultDescription>
      <XhResultAction>
        <XhButton variant="solid" tone="brand" @click="goHome(router)">
          {{ t('error.back_home') }}
        </XhButton>
        <XhButton variant="outline" @click="router.back()">
          {{ t('error.back_prev') }}
        </XhButton>
      </XhResultAction>
    </XhResultRoot>
  </div>
</template>
