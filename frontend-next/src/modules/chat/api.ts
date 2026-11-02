import type {
  ChatApiContract,
  ChatAttachmentUploadResult,
  ChatDepartmentPickerNode,
} from './types'
import type { components } from '~/contracts'
import { chatConversations, chatMessages } from '~/contracts'
import { endpointUrl, uploadForm } from '~/request'

type ChatAttachmentResponse = components['schemas']['ChatAttachmentResponse']
type ChatAttachmentUploadResponse = components['schemas']['ChatAttachmentUploadResponse']
type ChatDepartmentNode = components['schemas']['ChatDepartmentNode']

/** 普通上传端点（multipart 字段 file，只要聊天的发送权限；不在操作路由表里） */
export const CHAT_ATTACHMENT_UPLOAD_PATH = '/api/chat-messages/attachments'

/** 秒传探测的大小上限：整份读进内存算 SHA-256，超大文件直接走普通上传 */
export const FAST_UPLOAD_MAX_BYTES = 200 * 1024 * 1024

/** 批量操作的结果里只有失败项的错误码：逐项拒绝的原因拼成一条报错抛出，而不是当成功吞掉 */
function throwOnFailed(result: { failed: { code: string }[] }): void {
  if (result.failed.length > 0)
    throw new Error(result.failed.map(item => item.code).join('; '))
}

function mapDepartment(node: ChatDepartmentNode): ChatDepartmentPickerNode {
  return {
    departmentId: node.departmentId,
    departmentName: node.departmentName,
    children: node.children.length > 0 ? node.children.map(mapDepartment) : null,
  }
}

function toUploadResult(attachment: ChatAttachmentResponse): ChatAttachmentUploadResult {
  return { fileId: attachment.fileId, fileName: attachment.fileName, fileSize: attachment.fileSize }
}

/** SHA-256（十六进制小写，与后端秒传同一算法） */
async function sha256Hex(file: File): Promise<string> {
  const digest = await crypto.subtle.digest('SHA-256', await file.arrayBuffer())
  return [...new Uint8Array(digest)].map(b => b.toString(16).padStart(2, '0')).join('')
}

/**
 * 用生成的操作客户端组装聊天 API 契约（4.x 由 src/modules/chat/api/chat.ts 的 Dynamic API 客户端承担）。
 * 与 4.x 的不同：移出与禁言是批量操作、逐项失败的抛出；置顶与免打扰传新值；转发走服务端；
 * 附件只走聊天自己的上传、秒传与签名（方案 H2：附件以会话为访问范围，不再要通用文件权限）。
 */
export function createChatApi(): ChatApiContract {
  return {
    openSingleConversation: peerUserId => chatConversations.openSingle({ peerUserId }),
    createGroupConversation: (conversationName, memberUserIds) => chatConversations.createGroup({ conversationName, memberUserIds }),
    openDepartmentConversation: departmentId => chatConversations.openDepartment({ departmentId }),
    addMembers: async (conversationId, userIds) => throwOnFailed(await chatConversations.addMembers({ conversationId, userIds })),
    removeMember: async (conversationId, userId) => throwOnFailed(await chatConversations.removeMembers({ conversationId, userIds: [userId] })),
    leaveConversation: async (conversationId) => {
      await chatConversations.leave({ conversationId })
    },
    sendMessage: input => chatMessages.send(input),
    recallMessage: async (messageId) => {
      await chatMessages.recall({ messageId })
    },
    markRead: async (conversationId, upToMessageId) => {
      await chatConversations.markRead({ conversationId, upToMessageId: upToMessageId ?? null })
    },
    editMessage: (messageId, content) => chatMessages.edit({ messageId, content }),
    toggleReaction: (messageId, emoji) => chatMessages.react({ messageId, emoji }),
    forwardMessage: async (messageId, conversationIds) => throwOnFailed(await chatMessages.forward({ messageId, conversationIds })),
    pinMessage: async (messageId) => {
      await chatMessages.pin({ messageId })
    },
    unpinMessage: async (messageId) => {
      await chatMessages.unpin({ messageId })
    },
    setConversationPinned: (conversationId, isPinned) => chatConversations.pin({ conversationId, isPinned }),
    setConversationMuted: (conversationId, isMuted) => chatConversations.mute({ conversationId, isMuted }),
    updateConversationInfo: async (input) => {
      await chatConversations.updateInfo(input)
    },
    transferOwner: async (conversationId, newOwnerUserId) => {
      await chatConversations.transferOwner({ conversationId, newOwnerUserId })
    },
    setMemberSilence: async (conversationId, userId, isSilenced) => throwOnFailed(await chatConversations.silenceMembers({ conversationId, userIds: [userId], isSilenced })),
    setMemberRole: async (conversationId, userId, memberRole) => {
      await chatConversations.setMemberRole({ conversationId, userId, memberRole })
    },
    myConversations: async () => (await chatConversations.mine({})).items,
    messageHistory: query => chatMessages.history(query),
    searchMessages: query => chatMessages.search(query),
    members: async conversationId => (await chatConversations.members({ conversationId })).items,
    readPositions: async conversationId => (await chatConversations.readPositions({ conversationId })).items,
    pinnedMessages: async conversationId => (await chatMessages.pinned({ conversationId })).items,
    // 聊天专属选人端点（只要会话读权限）；显示名放进 4.x 的 nickName 槽位，头像是文件主键
    selectUsers: async (keyword, limit = 20) => (await chatConversations.userOptions({ keyword: keyword.trim() || null, limit })).items.map(user => ({
      userId: user.userId,
      userName: user.userName,
      nickName: user.displayName,
      avatar: user.avatarFileId ?? null,
    })),
    // 聊天专属部门树：当前一侧启用的部门（平台侧没有部门）
    departmentTree: async () => (await chatConversations.departmentTree({})).items.map(mapDepartment),
    async uploadAttachment(file, onProgress) {
      // 秒传：同一侧已有内容相同的文件即直接得到附件；没命中、浏览器不支持或文件超大时走普通上传
      if (file.size > 0 && file.size <= FAST_UPLOAD_MAX_BYTES && globalThis.crypto?.subtle) {
        try {
          const fast = await chatMessages.fastUpload({ hash: await sha256Hex(file), size: file.size, fileName: file.name, contentType: file.type || null })
          if (fast.attachment) {
            onProgress?.(100)
            return toUploadResult(fast.attachment)
          }
        }
        catch {
          // 秒传只是探测（4.x 同）：探测失败不阻断，照常走普通上传，普通上传的失败照常抛出
        }
      }
      const form = new FormData()
      form.append('file', file, file.name)
      const uploaded = await uploadForm<ChatAttachmentUploadResponse>(CHAT_ATTACHMENT_UPLOAD_PATH, form, { onProgress })
      if (!uploaded.attachment)
        throw new Error('chat-messages.upload returned no attachment')
      return toUploadResult(uploaded.attachment)
    },
    // 后端签发的地址原样使用（不能再拼参数）；相对地址补上接口前缀
    getFileUrl: async (conversationId, fileId, download = false) => {
      const signed = await chatMessages.presign({ conversationId, fileId, download })
      return { url: endpointUrl(signed.url), expiresAt: signed.expiresAt }
    },
  }
}
