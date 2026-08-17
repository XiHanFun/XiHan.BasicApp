/**
 * 聊天业务枚举（与后端 JsonStringEnumConverter 序列化值一致）。
 */

/** 聊天会话类型 */
export enum ChatConversationType {
  Single = 'Single',
  Group = 'Group',
  Department = 'Department',
  Assistant = 'Assistant',
}

/** 聊天成员角色 */
export enum ChatMemberRole {
  Owner = 'Owner',
  Admin = 'Admin',
  Member = 'Member',
}

/** 聊天消息类型 */
export enum ChatMessageType {
  Text = 'Text',
  Image = 'Image',
  Voice = 'Voice',
  File = 'File',
  Assistant = 'Assistant',
  System = 'System',
}
