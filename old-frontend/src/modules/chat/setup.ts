import type { DepartmentTreeNodeDto } from '@/api/modules/organization'
/**
 * 聊天模块启动注册。
 * 职责：组装 ChatApiContract 注入 @xihan/chat 的 API 缝，并注册聊天壳层扩展
 * （顶栏按钮/全局抽屉/实时集成）；删除本模块目录即聊天能力整体消失。
 */
import type { ChatApiContract, ChatDepartmentPickerNode } from '~/chat'
import { fileApi } from '@/api/modules/files'
import { chatShellExtension, setChatApi } from '~/chat'
import { registerShellExtension } from '~/stores'
import { chatApi } from './api/chat'

function mapDepartmentNode(node: DepartmentTreeNodeDto): ChatDepartmentPickerNode {
  return {
    departmentId: node.basicId,
    departmentName: node.departmentName,
    children: node.children?.map(mapDepartmentNode) ?? null,
  }
}

/** 组装聊天 API 契约并注册壳层扩展。 */
export default function setupChat(): void {
  const composed: ChatApiContract = {
    ...chatApi,
    async selectUsers(keyword: string, limit = 20) {
      // 走聊天专属选人端点（仅需 chat:read）；用户管理的 UserSelect 端点要求 saas:user:read，普通聊天用户会 403
      const items = await chatApi.userOptions(keyword, limit)
      return items.map(u => ({
        userId: u.basicId,
        userName: u.userName,
        nickName: u.nickName ?? u.realName,
        avatar: u.avatar,
      }))
    },
    async departmentTree() {
      // 走聊天专属端点：通用部门树是读共享口径，平台态会列出全部租户的部门
      const nodes = await chatApi.departmentTree()
      return nodes.map(mapDepartmentNode)
    },
    async uploadAttachment(file: File, onProgress?: (percent: number) => void) {
      // 秒传：SHA-256（与后端 FileTransferService 同算法，hex 小写）命中已有文件直接复用 fileId；
      // 未命中/不支持/超大文件（整读内存有风险）回退普通上传
      const FAST_UPLOAD_MAX_BYTES = 200 * 1024 * 1024
      if (file.size > 0 && file.size <= FAST_UPLOAD_MAX_BYTES && globalThis.crypto?.subtle) {
        try {
          const digest = await crypto.subtle.digest('SHA-256', await file.arrayBuffer())
          const fileHash = [...new Uint8Array(digest)].map(b => b.toString(16).padStart(2, '0')).join('')
          // 探测语义：命中返回详情，未命中返回 null（不再抛 4xx）
          const fast = await fileApi.fastUpload({
            fileHash,
            originalName: file.name,
            fileSize: file.size,
            mimeType: file.type || null,
          })
          if (fast) {
            onProgress?.(100)
            return {
              fileId: fast.basicId,
              fileName: fast.originalName || fast.fileName,
              fileSize: fast.fileSize,
            }
          }
        }
        catch {
          // 探测失败不阻断：回退普通上传
        }
      }
      const detail = await fileApi.upload({ file, directory: 'chat' }, onProgress)
      return {
        fileId: detail.basicId,
        fileName: detail.originalName || detail.fileName,
        fileSize: detail.fileSize,
      }
    },
    getFileUrl(fileId: string) {
      return fileApi.generatePresignedUrl(fileId)
    },
  }
  setChatApi(composed)
  registerShellExtension(chatShellExtension)
}
