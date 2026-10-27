// 由 scripts/gen-contracts.mjs 从 contracts/openapi.json 生成；不要手改，改后端契约后执行 pnpm gen:contracts。
/* eslint-disable */
export interface paths {
    "/api/auth:login": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Sign in with user name and password (tenant optional; platform accounts leave it empty); returns access and refresh tokens. */
        post: operations["auth.login"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/auth:logout": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Revoke the session of the current token. */
        post: operations["auth.logout"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/auth:me": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Return the current user, tenant, session and roles. */
        post: operations["auth.me"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/auth:permissions": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Return the current principal's effective permissions, roles, menu tree and page codes. */
        post: operations["auth.permissions"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/auth:refresh": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Exchange a refresh token for new tokens; the refresh token rotates and the old one stops working. */
        post: operations["auth.refresh"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/departments": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Create a department anywhere in the tree (code unique within the tenant; parent must exist; leader must be a user of this tenant). Tenant side only; requires organization.departments.create. */
        post: operations["departments.create"];
        /** Delete departments in batch; departments that still have children or members are rejected item by item. Requires organization.departments.delete. */
        delete: operations["departments.delete"];
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/departments/{id}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Get a department with its ancestors, leader user name, child count and member count. Requires organization.departments.read. */
        get: operations["departments.get"];
        /** Update a department's name, leader, order, description and status (the parent is changed with departments.move). Requires organization.departments.update. */
        put: operations["departments.update"];
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/departments/{id}:move": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Move a department under another parent or to the top level; moving under itself or a descendant is rejected. Requires organization.departments.update. */
        post: operations["departments.move"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/departments:tree": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** The whole department tree of the current tenant with direct member counts. Requires organization.departments.read. */
        post: operations["departments.tree"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/memberships": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Add a user of this tenant to a department, optionally with a position; the first membership of a user becomes the primary one. Requires organization.memberships.create. */
        post: operations["memberships.create"];
        /** Remove users from departments in batch; when a primary membership is removed the next one becomes primary. Requires organization.memberships.delete. */
        delete: operations["memberships.delete"];
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/memberships/{id}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        /** Update a membership's position, primary flag, employee number, grade, hire date and order. Requires organization.memberships.update. */
        put: operations["memberships.update"];
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/memberships:list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** List memberships filtered by department, user, position or primary flag, with user, department and position names. Requires organization.memberships.read. */
        post: operations["memberships.list"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/permissions": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** List the permission catalog grantable on the current side. Requires access.permissions.read. */
        get: operations["permissions.list"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/positions": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Create a position (code unique within the tenant). Requires organization.positions.create. */
        post: operations["positions.create"];
        /** Delete positions in batch; positions still referenced by memberships are rejected item by item. Requires organization.positions.delete. */
        delete: operations["positions.delete"];
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/positions/{id}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Get a position. Requires organization.positions.read. */
        get: operations["positions.get"];
        /** Update a position's name, order, description and status. Requires organization.positions.update. */
        put: operations["positions.update"];
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/positions:list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** List positions with paging, keyword, filters and sorting. Requires organization.positions.read. */
        post: operations["positions.list"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/profile:change-password": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Change your own password; requires the current password and revokes your other sessions. */
        post: operations["profile.change-password"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/roles": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Create a role on the current side (platform or tenant); the code is unique per side and cannot be changed. Requires access.roles.create. */
        post: operations["roles.create"];
        /** Delete roles in batch together with their members and permissions; built-in roles cannot be deleted. Requires access.roles.delete. */
        delete: operations["roles.delete"];
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/roles/{id}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Get a role with its granted permissions and member count. Requires access.roles.read. */
        get: operations["roles.get"];
        /** Update a role's name, description, order and status. Requires access.roles.update. */
        put: operations["roles.update"];
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/roles/{id}:assign": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Assign users to a role; users must belong to the role's side. Takes effect at the user's next token refresh. Requires access.roles.assign. */
        post: operations["roles.assign"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/roles/{id}:grant": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Grant permissions to a role; codes must exist in the catalog and match the role's side. Requires access.roles.grant. */
        post: operations["roles.grant"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/roles/{id}:members": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** List the members of a role. Requires access.roles.read. */
        post: operations["roles.members"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/roles/{id}:revoke": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Revoke permissions from a role. Requires access.roles.grant. */
        post: operations["roles.revoke"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/roles/{id}:unassign": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Remove users from a role. Takes effect at the user's next token refresh. Requires access.roles.assign. */
        post: operations["roles.unassign"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/roles:by-user": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** List the roles a user holds on the current side (platform or tenant). Requires access.roles.read. */
        post: operations["roles.by-user"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/roles:list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** List roles of the current side with paging, filters, keyword and sorting. Requires access.roles.read. */
        post: operations["roles.list"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/tenants": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Provision a tenant: write the tenant and register it (enabled) in the tenant directory; the administrator is created with tenants.initialize-admin. Platform side only; requires tenancy.tenants.create. */
        post: operations["tenants.create"];
        /** Delete tenants in batch and remove them from the directory; tenants that still have users are rejected item by item. Requires tenancy.tenants.delete. */
        delete: operations["tenants.delete"];
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/tenants/{id}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Get a tenant with its administrator's user name and user count. Requires tenancy.tenants.read. */
        get: operations["tenants.get"];
        /** Update a tenant's name, description, expiry and order; the name is mirrored to the directory. Requires tenancy.tenants.update. */
        put: operations["tenants.update"];
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/tenants/{id}:initialize-admin": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Create the first administrator of a tenant with every tenant-side permission; the password must be changed at first sign-in. Once per tenant. Requires tenancy.tenants.initialize-admin. */
        post: operations["tenants.initialize-admin"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/tenants:list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** List tenants with paging, filters, keyword and sorting. Requires tenancy.tenants.read. */
        post: operations["tenants.list"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/tenants:resume": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Resume suspended tenants in batch. Requires tenancy.tenants.suspend. */
        post: operations["tenants.resume"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/tenants:suspend": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Suspend tenants in batch: the directory marks them disabled, their users cannot sign in and calls carrying the tenant are rejected. Requires tenancy.tenants.suspend. */
        post: operations["tenants.suspend"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/users": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Create a user: a tenant principal creates one in its own tenant; a platform principal may name the home tenant or create a platform account. Requires identity.users.create. */
        post: operations["users.create"];
        /** Delete users in batch (soft delete) and revoke their sessions; you cannot delete yourself. Requires identity.users.delete. */
        delete: operations["users.delete"];
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/users/{id}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        /** Get a user by ID; tenant principals only see their own tenant. Requires identity.users.read. */
        get: operations["users.get"];
        /** Update a user's display name, email and phone number. Requires identity.users.update. */
        put: operations["users.update"];
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/users/{id}:reset-password": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Set a new password for a user, revoke their sessions and require a change at next login. Requires identity.users.reset-password. */
        post: operations["users.reset-password"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/users:list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** List users with paging, filters, keyword and sorting; tenant principals only see their own tenant. Requires identity.users.read. */
        post: operations["users.list"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/users:lock": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Lock users in batch and revoke their sessions; you cannot lock yourself. Requires identity.users.lock. */
        post: operations["users.lock"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/api/users:unlock": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Unlock users in batch. Requires identity.users.lock. */
        post: operations["users.unlock"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
}
export type webhooks = Record<string, never>;
export interface components {
    schemas: {
        ProblemDetails: {
            type: string;
            title: string;
            detail?: string;
            instance?: string;
            code: string;
            traceId?: string;
            status: number;
            errors?: {
                [key: string]: string[];
            };
            details?: {
                [key: string]: string | number | boolean | null;
            };
        };
        BatchFailure: {
            /** Format: uuid */
            id: string;
            code: string;
        };
        /** @enum {string} */
        DataScope: "SelfOnly" | "DepartmentOnly" | "DepartmentAndChildren" | "All" | "Custom";
        DataScopeDepartmentRequest: {
            /** Format: uuid */
            departmentId: string;
            includeChildren?: boolean;
        };
        DepartmentNode: {
            department: components["schemas"]["DepartmentResponse"];
            /** Format: int64 */
            memberCount: number;
            children: components["schemas"]["DepartmentNode"][];
        };
        DepartmentResponse: {
            /** Format: uuid */
            id: string;
            code: string;
            name: string;
            /** Format: uuid */
            parentId?: string | null;
            /** Format: uuid */
            leaderUserId?: string | null;
            status: components["schemas"]["OrganizationStatus"];
            /** Format: int32 */
            order: number;
            description?: string | null;
            /** Format: date-time */
            createdAt: string;
        };
        MembershipResponse: {
            /** Format: uuid */
            id: string;
            /** Format: uuid */
            userId: string;
            userName: string;
            displayName: string;
            /** Format: uuid */
            departmentId: string;
            departmentName: string;
            /** Format: uuid */
            positionId?: string | null;
            positionName?: string | null;
            isPrimary: boolean;
            employeeNumber?: string | null;
            jobGrade?: string | null;
            /** Format: date-time */
            hiredAt?: string | null;
            /** Format: int32 */
            order: number;
            /** Format: date-time */
            createdAt: string;
        };
        MenuNode: {
            code: string;
            route: string;
            component: string;
            titleKey: string;
            icon?: string | null;
            /** Format: int32 */
            order: number;
            permission?: string | null;
            children: components["schemas"]["MenuNode"][];
        };
        /** @enum {string} */
        OrganizationStatus: "Active" | "Disabled";
        PageFilter: {
            field: string;
            operator: string;
            value?: string | null;
        };
        PageInfo: {
            /** Format: int32 */
            index?: number;
            /** Format: int32 */
            size?: number;
        };
        PageSort: {
            field: string;
            descending?: boolean;
        };
        PermissionFailure: {
            permission: string;
            code: string;
        };
        PermissionResponse: {
            code: string;
            displayName: string;
            description?: string | null;
            package: string;
            resource: string;
            action: string;
            scope: components["schemas"]["PermissionScope"];
            audited: boolean;
        };
        /** @enum {string} */
        PermissionScope: "Platform" | "Tenant" | "Both";
        PositionResponse: {
            /** Format: uuid */
            id: string;
            code: string;
            name: string;
            status: components["schemas"]["OrganizationStatus"];
            /** Format: int32 */
            order: number;
            description?: string | null;
            /** Format: date-time */
            createdAt: string;
        };
        RoleMemberResponse: {
            /** Format: uuid */
            userId: string;
            userName: string;
            displayName: string;
            status: components["schemas"]["UserStatus"];
            /** Format: date-time */
            assignedAt: string;
        };
        RoleResponse: {
            /** Format: uuid */
            id: string;
            ownerTenantId: string;
            code: string;
            name: string;
            description?: string | null;
            /** Format: int32 */
            order: number;
            isBuiltIn: boolean;
            status: components["schemas"]["RoleStatus"];
            dataScope: components["schemas"]["DataScope"];
            /** Format: date-time */
            createdAt: string;
        };
        /** @enum {string} */
        RoleStatus: "Active" | "Disabled";
        RoleSummary: {
            /** Format: uuid */
            id: string;
            code: string;
            name: string;
        };
        TenantResponse: {
            /** Format: uuid */
            id: string;
            tenantId: string;
            name: string;
            description?: string | null;
            status: components["schemas"]["TenantStatus"];
            /** Format: date-time */
            expiresAt?: string | null;
            /** Format: uuid */
            adminUserId?: string | null;
            /** Format: int32 */
            order: number;
            /** Format: date-time */
            createdAt: string;
        };
        /** @enum {string} */
        TenantStatus: "Active" | "Suspended";
        UserResponse: {
            /** Format: uuid */
            id: string;
            userName: string;
            displayName: string;
            email?: string | null;
            phoneNumber?: string | null;
            homeTenantId: string;
            status: components["schemas"]["UserStatus"];
            /** Format: date-time */
            lastLoginAt?: string | null;
            /** Format: date-time */
            createdAt: string;
            createdBy?: string | null;
        };
        /** @enum {string} */
        UserStatus: "Active" | "Disabled" | "Locked";
    };
    responses: never;
    parameters: never;
    requestBodies: never;
    headers: never;
    pathItems: never;
}
export type $defs = Record<string, never>;
export interface operations {
    "auth.login": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    userName: string;
                    password: string;
                    tenant?: string | null;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        accessToken: string;
                        /** Format: date-time */
                        accessExpiresAt: string;
                        refreshToken: string;
                        /** Format: date-time */
                        refreshExpiresAt: string;
                        /** Format: uuid */
                        sessionId: string;
                        tenantId?: string | null;
                        mustChangePassword: boolean;
                        user: components["schemas"]["UserResponse"];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "auth.logout": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": Record<string, never>;
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        sessionId: string;
                        /** Format: date-time */
                        revokedAt: string;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "auth.me": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": Record<string, never>;
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        user: components["schemas"]["UserResponse"];
                        tenantId?: string | null;
                        /** Format: uuid */
                        sessionId?: string | null;
                        roles: string[];
                        mustChangePassword: boolean;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "auth.permissions": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": Record<string, never>;
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        permissions: string[];
                        roles: components["schemas"]["RoleSummary"][];
                        menus: components["schemas"]["MenuNode"][];
                        pages: string[];
                        isPlatform: boolean;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "auth.refresh": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    refreshToken: string;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        accessToken: string;
                        /** Format: date-time */
                        accessExpiresAt: string;
                        refreshToken: string;
                        /** Format: date-time */
                        refreshExpiresAt: string;
                        /** Format: uuid */
                        sessionId: string;
                        tenantId?: string | null;
                        mustChangePassword: boolean;
                        user: components["schemas"]["UserResponse"];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "departments.create": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    code: string;
                    name: string;
                    /** Format: uuid */
                    parentId?: string | null;
                    /** Format: uuid */
                    leaderUserId?: string | null;
                    /** Format: int32 */
                    order?: number;
                    description?: string | null;
                };
            };
        };
        responses: {
            /** @description Success */
            201: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        code: string;
                        name: string;
                        /** Format: uuid */
                        parentId?: string | null;
                        /** Format: uuid */
                        leaderUserId?: string | null;
                        status: components["schemas"]["OrganizationStatus"];
                        /** Format: int32 */
                        order: number;
                        description?: string | null;
                        /** Format: date-time */
                        createdAt: string;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "departments.delete": {
        parameters: {
            query: {
                ids: string[];
            };
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        succeeded: string[];
                        failed: components["schemas"]["BatchFailure"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "departments.get": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        department: components["schemas"]["DepartmentResponse"];
                        ancestors: components["schemas"]["DepartmentResponse"][];
                        leaderUserName?: string | null;
                        /** Format: int64 */
                        childCount: number;
                        /** Format: int64 */
                        memberCount: number;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "departments.update": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    id: string;
                    name: string;
                    /** Format: uuid */
                    leaderUserId?: string | null;
                    /** Format: int32 */
                    order?: number;
                    description?: string | null;
                    status?: components["schemas"]["OrganizationStatus"];
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        code: string;
                        name: string;
                        /** Format: uuid */
                        parentId?: string | null;
                        /** Format: uuid */
                        leaderUserId?: string | null;
                        status: components["schemas"]["OrganizationStatus"];
                        /** Format: int32 */
                        order: number;
                        description?: string | null;
                        /** Format: date-time */
                        createdAt: string;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "departments.move": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    id: string;
                    /** Format: uuid */
                    parentId?: string | null;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        code: string;
                        name: string;
                        /** Format: uuid */
                        parentId?: string | null;
                        /** Format: uuid */
                        leaderUserId?: string | null;
                        status: components["schemas"]["OrganizationStatus"];
                        /** Format: int32 */
                        order: number;
                        description?: string | null;
                        /** Format: date-time */
                        createdAt: string;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "departments.tree": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": Record<string, never>;
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        items: components["schemas"]["DepartmentNode"][];
                        /** Format: int64 */
                        total: number;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "memberships.create": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    userId: string;
                    /** Format: uuid */
                    departmentId: string;
                    /** Format: uuid */
                    positionId?: string | null;
                    isPrimary?: boolean;
                    employeeNumber?: string | null;
                    jobGrade?: string | null;
                    /** Format: date-time */
                    hiredAt?: string | null;
                    /** Format: int32 */
                    order?: number;
                };
            };
        };
        responses: {
            /** @description Success */
            201: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        /** Format: uuid */
                        userId: string;
                        userName: string;
                        displayName: string;
                        /** Format: uuid */
                        departmentId: string;
                        departmentName: string;
                        /** Format: uuid */
                        positionId?: string | null;
                        positionName?: string | null;
                        isPrimary: boolean;
                        employeeNumber?: string | null;
                        jobGrade?: string | null;
                        /** Format: date-time */
                        hiredAt?: string | null;
                        /** Format: int32 */
                        order: number;
                        /** Format: date-time */
                        createdAt: string;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "memberships.delete": {
        parameters: {
            query: {
                ids: string[];
            };
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        succeeded: string[];
                        failed: components["schemas"]["BatchFailure"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "memberships.update": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    id: string;
                    /** Format: uuid */
                    positionId?: string | null;
                    isPrimary?: boolean;
                    employeeNumber?: string | null;
                    jobGrade?: string | null;
                    /** Format: date-time */
                    hiredAt?: string | null;
                    /** Format: int32 */
                    order?: number;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        /** Format: uuid */
                        userId: string;
                        userName: string;
                        displayName: string;
                        /** Format: uuid */
                        departmentId: string;
                        departmentName: string;
                        /** Format: uuid */
                        positionId?: string | null;
                        positionName?: string | null;
                        isPrimary: boolean;
                        employeeNumber?: string | null;
                        jobGrade?: string | null;
                        /** Format: date-time */
                        hiredAt?: string | null;
                        /** Format: int32 */
                        order: number;
                        /** Format: date-time */
                        createdAt: string;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "memberships.list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    page?: components["schemas"]["PageInfo"];
                    filters?: components["schemas"]["PageFilter"][];
                    keyword?: string | null;
                    sorts?: components["schemas"]["PageSort"][];
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        items: components["schemas"]["MembershipResponse"][];
                        /** Format: int64 */
                        total: number;
                        page: components["schemas"]["PageInfo"];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "permissions.list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        items: components["schemas"]["PermissionResponse"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "positions.create": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    code: string;
                    name: string;
                    /** Format: int32 */
                    order?: number;
                    description?: string | null;
                };
            };
        };
        responses: {
            /** @description Success */
            201: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        code: string;
                        name: string;
                        status: components["schemas"]["OrganizationStatus"];
                        /** Format: int32 */
                        order: number;
                        description?: string | null;
                        /** Format: date-time */
                        createdAt: string;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "positions.delete": {
        parameters: {
            query: {
                ids: string[];
            };
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        succeeded: string[];
                        failed: components["schemas"]["BatchFailure"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "positions.get": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        code: string;
                        name: string;
                        status: components["schemas"]["OrganizationStatus"];
                        /** Format: int32 */
                        order: number;
                        description?: string | null;
                        /** Format: date-time */
                        createdAt: string;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "positions.update": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    id: string;
                    name: string;
                    /** Format: int32 */
                    order?: number;
                    description?: string | null;
                    status?: components["schemas"]["OrganizationStatus"];
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        code: string;
                        name: string;
                        status: components["schemas"]["OrganizationStatus"];
                        /** Format: int32 */
                        order: number;
                        description?: string | null;
                        /** Format: date-time */
                        createdAt: string;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "positions.list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    page?: components["schemas"]["PageInfo"];
                    filters?: components["schemas"]["PageFilter"][];
                    keyword?: string | null;
                    sorts?: components["schemas"]["PageSort"][];
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        items: components["schemas"]["PositionResponse"][];
                        /** Format: int64 */
                        total: number;
                        page: components["schemas"]["PageInfo"];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "profile.change-password": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    currentPassword: string;
                    newPassword: string;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        userId: string;
                        /** Format: date-time */
                        changedAt: string;
                        sessionsRevoked: boolean;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "roles.create": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    code: string;
                    name: string;
                    description?: string | null;
                    /** Format: int32 */
                    order?: number;
                    dataScope?: components["schemas"]["DataScope"] | null;
                };
            };
        };
        responses: {
            /** @description Success */
            201: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        ownerTenantId: string;
                        code: string;
                        name: string;
                        description?: string | null;
                        /** Format: int32 */
                        order: number;
                        isBuiltIn: boolean;
                        status: components["schemas"]["RoleStatus"];
                        dataScope: components["schemas"]["DataScope"];
                        /** Format: date-time */
                        createdAt: string;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "roles.delete": {
        parameters: {
            query: {
                ids: string[];
            };
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        succeeded: string[];
                        failed: components["schemas"]["BatchFailure"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "roles.get": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        role: components["schemas"]["RoleResponse"];
                        permissions: string[];
                        /** Format: int64 */
                        memberCount: number;
                        customDepartments: components["schemas"]["DataScopeDepartmentRequest"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "roles.update": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    id: string;
                    name: string;
                    description?: string | null;
                    /** Format: int32 */
                    order?: number;
                    status?: components["schemas"]["RoleStatus"];
                    dataScope?: components["schemas"]["DataScope"] | null;
                    customDepartments?: components["schemas"]["DataScopeDepartmentRequest"][] | null;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        ownerTenantId: string;
                        code: string;
                        name: string;
                        description?: string | null;
                        /** Format: int32 */
                        order: number;
                        isBuiltIn: boolean;
                        status: components["schemas"]["RoleStatus"];
                        dataScope: components["schemas"]["DataScope"];
                        /** Format: date-time */
                        createdAt: string;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "roles.assign": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    id: string;
                    userIds: string[];
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        succeeded: string[];
                        failed: components["schemas"]["BatchFailure"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "roles.grant": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    id: string;
                    permissions: string[];
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        succeeded: string[];
                        failed: components["schemas"]["PermissionFailure"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "roles.members": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    id: string;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        items: components["schemas"]["RoleMemberResponse"][];
                        /** Format: int64 */
                        total: number;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "roles.revoke": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    id: string;
                    permissions: string[];
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        succeeded: string[];
                        failed: components["schemas"]["PermissionFailure"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "roles.unassign": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    id: string;
                    userIds: string[];
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        succeeded: string[];
                        failed: components["schemas"]["BatchFailure"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "roles.by-user": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    userId: string;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        items: components["schemas"]["RoleResponse"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "roles.list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    page?: components["schemas"]["PageInfo"];
                    filters?: components["schemas"]["PageFilter"][];
                    keyword?: string | null;
                    sorts?: components["schemas"]["PageSort"][];
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        items: components["schemas"]["RoleResponse"][];
                        /** Format: int64 */
                        total: number;
                        page: components["schemas"]["PageInfo"];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "tenants.create": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    tenantId: string;
                    name: string;
                    description?: string | null;
                    /** Format: date-time */
                    expiresAt?: string | null;
                    /** Format: int32 */
                    order?: number;
                };
            };
        };
        responses: {
            /** @description Success */
            201: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        tenantId: string;
                        name: string;
                        description?: string | null;
                        status: components["schemas"]["TenantStatus"];
                        /** Format: date-time */
                        expiresAt?: string | null;
                        /** Format: uuid */
                        adminUserId?: string | null;
                        /** Format: int32 */
                        order: number;
                        /** Format: date-time */
                        createdAt: string;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "tenants.delete": {
        parameters: {
            query: {
                ids: string[];
            };
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        succeeded: string[];
                        failed: components["schemas"]["BatchFailure"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "tenants.get": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        tenant: components["schemas"]["TenantResponse"];
                        adminUserName?: string | null;
                        /** Format: int64 */
                        userCount: number;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "tenants.update": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    id: string;
                    name: string;
                    description?: string | null;
                    /** Format: date-time */
                    expiresAt?: string | null;
                    /** Format: int32 */
                    order?: number;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        tenantId: string;
                        name: string;
                        description?: string | null;
                        status: components["schemas"]["TenantStatus"];
                        /** Format: date-time */
                        expiresAt?: string | null;
                        /** Format: uuid */
                        adminUserId?: string | null;
                        /** Format: int32 */
                        order: number;
                        /** Format: date-time */
                        createdAt: string;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "tenants.initialize-admin": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    id: string;
                    userName: string;
                    password: string;
                    displayName?: string | null;
                    email?: string | null;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        userName: string;
                        displayName: string;
                        email?: string | null;
                        phoneNumber?: string | null;
                        homeTenantId: string;
                        status: components["schemas"]["UserStatus"];
                        /** Format: date-time */
                        lastLoginAt?: string | null;
                        /** Format: date-time */
                        createdAt: string;
                        createdBy?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "tenants.list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    page?: components["schemas"]["PageInfo"];
                    filters?: components["schemas"]["PageFilter"][];
                    keyword?: string | null;
                    sorts?: components["schemas"]["PageSort"][];
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        items: components["schemas"]["TenantResponse"][];
                        /** Format: int64 */
                        total: number;
                        page: components["schemas"]["PageInfo"];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "tenants.resume": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    ids: string[];
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        succeeded: string[];
                        failed: components["schemas"]["BatchFailure"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "tenants.suspend": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    ids: string[];
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        succeeded: string[];
                        failed: components["schemas"]["BatchFailure"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "users.create": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    userName: string;
                    displayName: string;
                    email?: string | null;
                    phoneNumber?: string | null;
                    password?: string | null;
                    homeTenantId?: string | null;
                };
            };
        };
        responses: {
            /** @description Success */
            201: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        userName: string;
                        displayName: string;
                        email?: string | null;
                        phoneNumber?: string | null;
                        homeTenantId: string;
                        status: components["schemas"]["UserStatus"];
                        /** Format: date-time */
                        lastLoginAt?: string | null;
                        /** Format: date-time */
                        createdAt: string;
                        createdBy?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "users.delete": {
        parameters: {
            query: {
                ids: string[];
            };
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        succeeded: string[];
                        failed: components["schemas"]["BatchFailure"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "users.get": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        userName: string;
                        displayName: string;
                        email?: string | null;
                        phoneNumber?: string | null;
                        homeTenantId: string;
                        status: components["schemas"]["UserStatus"];
                        /** Format: date-time */
                        lastLoginAt?: string | null;
                        /** Format: date-time */
                        createdAt: string;
                        createdBy?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "users.update": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    id: string;
                    displayName: string;
                    email?: string | null;
                    phoneNumber?: string | null;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        id: string;
                        userName: string;
                        displayName: string;
                        email?: string | null;
                        phoneNumber?: string | null;
                        homeTenantId: string;
                        status: components["schemas"]["UserStatus"];
                        /** Format: date-time */
                        lastLoginAt?: string | null;
                        /** Format: date-time */
                        createdAt: string;
                        createdBy?: string | null;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "users.reset-password": {
        parameters: {
            query?: never;
            header?: never;
            path: {
                id: string;
            };
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    /** Format: uuid */
                    id: string;
                    newPassword: string;
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        /** Format: uuid */
                        userId: string;
                        /** Format: date-time */
                        changedAt: string;
                        sessionsRevoked: boolean;
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "users.list": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    page?: components["schemas"]["PageInfo"];
                    filters?: components["schemas"]["PageFilter"][];
                    keyword?: string | null;
                    sorts?: components["schemas"]["PageSort"][];
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        items: components["schemas"]["UserResponse"][];
                        /** Format: int64 */
                        total: number;
                        page: components["schemas"]["PageInfo"];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "users.lock": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    ids: string[];
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        succeeded: string[];
                        failed: components["schemas"]["BatchFailure"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
    "users.unlock": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": {
                    ids: string[];
                };
            };
        };
        responses: {
            /** @description Success */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": {
                        succeeded: string[];
                        failed: components["schemas"]["BatchFailure"][];
                    };
                };
            };
            /** @description Error */
            default: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/problem+json": components["schemas"]["ProblemDetails"];
                };
            };
        };
    };
}
