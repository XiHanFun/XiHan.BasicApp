<!--
  Copyright (c) 2021-Present XiHanFun and contributors.
  Licensed under the MIT License. See LICENSE in the project root for license information.
-->

<script setup lang="ts">
import {
  XhButton,
  XhButtonIndicator,
  XhButtonLabel,
  XhDialogBody,
  XhDialogCloseTrigger,
  XhDialogContent,
  XhDialogFooter,
  XhDialogHeader,
  XhDialogRoot,
  XhDialogTitle,
} from '@xihan-ui/vue'
import { ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { toast } from '~/composables'
import { getChatApi } from '../api-contract'
import { chatErrorText } from './chat-helpers'
import ChatMemberPicker from './ChatMemberPicker.vue'

/** 往群里加成员：已在群里的人照样列出、标注而不可选 */
defineOptions({ name: 'ChatAddMembersDialog' })

const props = defineProps<{
  conversationId: null | string
  /** 现有成员（列出但不可选） */
  memberIds: string[]
}>()

const emit = defineEmits<{
  added: []
}>()

const show = defineModel<boolean>('show', { default: false })

const { t } = useI18n()

const userIds = ref<string[]>([])
const adding = ref(false)
// 打开的次数：选人按它换一份新的
const session = ref(0)

watch(show, (visible) => {
  if (visible) {
    session.value++
    userIds.value = []
  }
})

async function handleConfirm() {
  const id = props.conversationId
  if (!id || !userIds.value.length || adding.value) {
    return
  }
  adding.value = true
  try {
    await getChatApi().addMembers(id, userIds.value)
    show.value = false
    emit('added')
  }
  catch (error) {
    toast.danger(chatErrorText(error) || t('common.messages.operation_failed'))
  }
  finally {
    adding.value = false
  }
}
</script>

<template>
  <XhDialogRoot v-model:open="show">
    <XhDialogContent style="--xh-dialog-max-w: 680px">
      <XhDialogHeader>
        <XhDialogTitle>{{ t('chat.members.add') }}</XhDialogTitle>
      </XhDialogHeader>
      <XhDialogCloseTrigger />
      <XhDialogBody>
        <!-- 选人随第一次打开才挂上，之后每次打开换一份新的：搜索、范围与已选不带到下一次；关着时留着，退场动画里正文不塌 -->
        <ChatMemberPicker
          v-if="session > 0"
          :key="session"
          v-model:value="userIds"
          :locked-user-ids="props.memberIds"
          :locked-label="t('chat.picker.already_member')"
        />
      </XhDialogBody>
      <XhDialogFooter>
        <XhButton variant="subtle" size="sm" :disabled="adding" @click="show = false">
          {{ t('chat.start.cancel') }}
        </XhButton>
        <XhButton
          variant="subtle"
          size="sm"
          tone="brand"
          :loading="adding"
          :disabled="!userIds.length"
          @click="handleConfirm"
        >
          <XhButtonIndicator />
          <XhButtonLabel>{{ t('chat.start.confirm') }}</XhButtonLabel>
        </XhButton>
      </XhDialogFooter>
    </XhDialogContent>
  </XhDialogRoot>
</template>
