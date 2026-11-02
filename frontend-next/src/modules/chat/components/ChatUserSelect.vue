<script setup lang="ts">
import type {
  ChatUserPickerItem,
} from '../types'
import type { SelectOption } from '~/types'
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import XCombobox from '~/components/common/XCombobox.vue'
import { toast } from '~/composables'
import { getChatApi } from '../api-contract'
import { chatErrorText } from './chat-helpers'

defineOptions({ name: 'ChatUserSelect' })

const props = withDefaults(defineProps<{
  /** 多选（建群/加成员）；false 为单选（发起单聊） */
  multiple?: boolean
  /** 需要从候选中排除的用户ID（已在会话中的成员/自己） */
  excludeUserIds?: string[]
  placeholder?: string
}>(), {
  multiple: false,
  excludeUserIds: () => [],
  placeholder: undefined,
})

const model = defineModel<null | string | string[]>({ default: null })

const { t } = useI18n()

const loading = ref(false)
const options = ref<SelectOption[]>([])
let searchSeq = 0

const excluded = computed(() => new Set(props.excludeUserIds))

async function handleSearch(keyword: string) {
  const seq = ++searchSeq
  loading.value = true
  try {
    const items = await getChatApi().selectUsers(keyword, 20)
    // 仅采纳最后一次检索结果，避免慢响应覆盖新输入
    if (seq !== searchSeq) {
      return
    }
    options.value = items
      .filter(item => !excluded.value.has(item.userId))
      .map((item: ChatUserPickerItem) => ({
        // 6.0 的显示名总有值：与用户名相同时不重复括注
        label: item.nickName && item.nickName !== item.userName ? `${item.nickName}（${item.userName}）` : item.userName,
        value: item.userId,
      }))
  }
  catch (error) {
    if (seq === searchSeq) {
      options.value = []
      toast.danger(chatErrorText(error) || t('common.messages.operation_failed'))
    }
  }
  finally {
    if (seq === searchSeq) {
      loading.value = false
    }
  }
}
</script>

<template>
  <XCombobox
    v-model:value="model"
    :multiple="props.multiple"
    remote
    clearable
    :loading="loading"
    :options="options"
    :placeholder="props.placeholder ?? t('chat.start.user_placeholder')"
    :aria-label="props.placeholder ?? t('chat.start.user_placeholder')"
    @search="handleSearch"
    @focus="() => handleSearch('')"
  />
</template>
