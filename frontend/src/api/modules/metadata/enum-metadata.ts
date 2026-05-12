import { createDynamicApiClient } from '../../base'

const enumApi = createDynamicApiClient('EnumMetadata')

export interface EnumItem {
  name: string
  value: number
  displayName: string
  description?: string | null
}

export interface EnumMetadata {
  enumTypeName: string
  displayName: string
  items: EnumItem[]
}

export const enumMetadataApi = {
  getAll() {
    return enumApi.get<EnumMetadata[]>('GetAllEnums')
  },
  getByName(enumTypeName: string) {
    return enumApi.get<EnumMetadata>(`GetEnum/${encodeURIComponent(enumTypeName)}`)
  },
}
