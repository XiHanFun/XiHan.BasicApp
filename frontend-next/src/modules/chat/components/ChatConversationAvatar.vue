<script lang="ts" setup>
import type { ChatConversationType } from '~/contracts'
import { computed } from 'vue'
import XUserAvatar from '~/components/common/UserAvatar.vue'
import { useChatFileUrl } from '../composables/useChatFileUrl'

defineOptions({ name: 'ChatConversationAvatar' })

const props = withDefaults(defineProps<{
  conversationId: string
  conversationType: ChatConversationType
  /** 头像原始值：单聊是对端用户的头像文件，群与部门群是文件主键或直链 */
  avatar?: null | string
  /** 首字母回退用的名称 */
  name?: null | string
  /** 尺寸（px） */
  size?: number
  /** 是否圆形 */
  round?: boolean
}>(), {
  avatar: null,
  name: null,
  size: 40,
  round: true,
})

const FILE_ID = /^[\da-f]{8}-[\da-f]{4}-[\da-f]{4}-[\da-f]{4}-[\da-f]{12}$/i

/** 群与部门群的文件头像以会话为访问范围（方案 H2）：凭会话换签名地址，不要通用文件权限 */
const groupFile = computed(() => {
  const grouped = props.conversationType === 'Group' || props.conversationType === 'Department'
  return grouped && props.avatar && FILE_ID.test(props.avatar)
    ? { conversationId: props.conversationId, fileId: props.avatar }
    : null
})
const groupUrl = useChatFileUrl(groupFile)

/** 交给 XUserAvatar 的值：群的文件头像给换好的地址，单聊的用户头像与直链原样（XUserAvatar 自己换） */
const value = computed(() => (groupFile.value
  ? groupUrl.value || null
  : props.avatar))
</script>

<template>
  <XUserAvatar :avatar="value" :name="name" :size="size" :round="round" />
</template>
