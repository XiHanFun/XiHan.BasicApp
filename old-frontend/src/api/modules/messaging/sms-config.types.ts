import type { ApiId, BasicDto, DateTimeString, PageRequest } from '../../types'

/** 与后端 JsonStringEnumConverter 序列化值一致 */
export enum SmsProviderType {
  Aliyun = 'Aliyun',
  TencentCloud = 'TencentCloud',
}

export interface SmsConfigPageQueryDto extends PageRequest {
  isDefault?: boolean | null
  isEnabled?: boolean | null
  keyword?: string | null
  provider?: SmsProviderType | null
}

/** AccessKeySecret 为敏感字段不下发，仅以 hasAccessKeySecret 标识是否已配置 */
export interface SmsConfigListItemDto extends BasicDto {
  configCode: string
  configName: string
  createdTime: DateTimeString
  hasAccessKeySecret: boolean
  isDefault: boolean
  isEnabled: boolean
  modifiedTime?: DateTimeString | null
  provider: SmsProviderType
  /** 腾讯云 SmsSdkAppId */
  sdkAppId?: string | null
  /** 腾讯云地域，如 ap-guangzhou */
  region?: string | null
  signName: string
  sort: number
}

export interface SmsConfigDetailDto extends SmsConfigListItemDto {
  accessKeyId: string
  createdBy?: string | null
  createdId?: ApiId | null
  modifiedBy?: string | null
  modifiedId?: ApiId | null
  remark?: string | null
  /** JSON：内部模板码 → 服务商模板码 + 参数序 */
  templateMap?: string | null
}

export interface SmsConfigCreateDto {
  accessKeyId: string
  /** 仅写入，读侧不回显 */
  accessKeySecret: string
  configCode: string
  configName: string
  isDefault: boolean
  isEnabled: boolean
  provider: SmsProviderType
  region?: string | null
  remark?: string | null
  /** 腾讯云必填 */
  sdkAppId?: string | null
  signName: string
  sort: number
  templateMap?: string | null
}

export interface SmsConfigUpdateDto extends BasicDto {
  accessKeyId: string
  /** 留空表示保留原密钥 */
  accessKeySecret?: string | null
  configName: string
  provider: SmsProviderType
  region?: string | null
  remark?: string | null
  /** 腾讯云必填 */
  sdkAppId?: string | null
  signName: string
  sort: number
  templateMap?: string | null
}

export interface SmsConfigStatusUpdateDto extends BasicDto {
  isEnabled: boolean
}

export type SmsConfigDefaultUpdateDto = BasicDto
