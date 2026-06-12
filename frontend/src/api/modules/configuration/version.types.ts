import type { ApiId, BasicDto, DateTimeString, PageRequest } from '../../types'

export interface VersionPageQueryDto extends PageRequest {
  appVersion?: string | null
  dbVersion?: string | null
  isUpgrading?: boolean | null
  keyword?: string | null
  minSupportVersion?: string | null
  upgradeNode?: string | null
  upgradeStartTimeEnd?: DateTimeString | null
  upgradeStartTimeStart?: DateTimeString | null
}

export interface VersionListItemDto extends BasicDto {
  appVersion: string
  createdTime: DateTimeString
  dbVersion: string
  /** 是否升级中（true = 当前存在进行中的升级） */
  isUpgrading: boolean
  minSupportVersion?: string | null
  upgradeNode?: string | null
  upgradeStartTime?: DateTimeString | null
}

export interface VersionDetailDto extends VersionListItemDto {
  createdBy?: string | null
  createdId?: ApiId | null
}

export interface VersionCreateDto {
  appVersion: string
  dbVersion: string
  isUpgrading: boolean
  minSupportVersion?: string | null
  upgradeNode?: string | null
  upgradeStartTime?: DateTimeString | null
}

export interface VersionUpdateDto extends BasicDto {
  appVersion: string
  dbVersion: string
  isUpgrading: boolean
  minSupportVersion?: string | null
  upgradeNode?: string | null
  upgradeStartTime?: DateTimeString | null
}

export interface VersionUpgradeStartDto extends BasicDto {
  upgradeNode?: string | null
  upgradeStartTime?: DateTimeString | null
}

export interface VersionUpgradeFinishDto extends BasicDto {
  appVersion?: string | null
  dbVersion?: string | null
  minSupportVersion?: string | null
}

export interface MigrationHistoryPageQueryDto extends PageRequest {
  executedTimeEnd?: DateTimeString | null
  executedTimeStart?: DateTimeString | null
  keyword?: string | null
  nodeName?: string | null
  scriptName?: string | null
  success?: boolean | null
  version?: string | null
}

export interface MigrationHistoryListItemDto extends BasicDto {
  createdTime: DateTimeString
  executedTime: DateTimeString
  nodeName?: string | null
  scriptName: string
  success: boolean
  version: string
}

export interface MigrationHistoryDetailDto extends MigrationHistoryListItemDto {
  createdBy?: string | null
  createdId?: ApiId | null
}
