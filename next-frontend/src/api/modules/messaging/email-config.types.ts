import type { ApiId, BasicDto, DateTimeString, PageRequest } from '../../types'

export interface EmailConfigPageQueryDto extends PageRequest {
  isDefault?: boolean | null
  isEnabled?: boolean | null
  keyword?: string | null
}

/** Password 为敏感字段不下发，仅以 hasPassword 标识是否已配置 */
export interface EmailConfigListItemDto extends BasicDto {
  configCode: string
  configName: string
  createdTime: DateTimeString
  fromEmail: string
  fromName: string
  hasPassword: boolean
  isDefault: boolean
  isEnabled: boolean
  modifiedTime?: DateTimeString | null
  smtpHost: string
  smtpPort: number
  sort: number
  useSsl: boolean
}

export interface EmailConfigDetailDto extends EmailConfigListItemDto {
  acceptInvalidCertificate: boolean
  createdBy?: string | null
  createdId?: ApiId | null
  isBodyHtml: boolean
  modifiedBy?: string | null
  modifiedId?: ApiId | null
  remark?: string | null
  userName?: string | null
}

export interface EmailConfigCreateDto {
  acceptInvalidCertificate: boolean
  configCode: string
  configName: string
  fromEmail: string
  fromName: string
  isBodyHtml: boolean
  isDefault: boolean
  isEnabled: boolean
  /** 仅写入，读侧不回显 */
  password?: string | null
  remark?: string | null
  smtpHost: string
  smtpPort: number
  sort: number
  userName?: string | null
  useSsl: boolean
}

export interface EmailConfigUpdateDto extends BasicDto {
  acceptInvalidCertificate: boolean
  configName: string
  fromEmail: string
  fromName: string
  isBodyHtml: boolean
  /** 留空表示保留原密码 */
  password?: string | null
  remark?: string | null
  smtpHost: string
  smtpPort: number
  sort: number
  userName?: string | null
  useSsl: boolean
}

export interface EmailConfigStatusUpdateDto extends BasicDto {
  isEnabled: boolean
}

export type EmailConfigDefaultUpdateDto = BasicDto
