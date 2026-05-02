export enum TenantConfigStatus {
  Pending = 0,
  Configuring = 1,
  Configured = 2,
  Failed = 3,
  Disabled = 4,
}

export enum TenantIsolationMode {
  Field = 0,
  Database = 1,
  Schema = 2,
}

export enum TenantStatus {
  Normal = 0,
  Suspended = 1,
  Expired = 2,
  Disabled = 3,
}

export enum TenantMemberInviteStatus {
  Pending = 0,
  Accepted = 1,
  Rejected = 2,
  Revoked = 3,
  Expired = 4,
}

export enum TenantMemberType {
  Owner = 0,
  Admin = 1,
  Member = 2,
  External = 3,
  Guest = 4,
  Consultant = 5,
  PlatformAdmin = 99,
}
