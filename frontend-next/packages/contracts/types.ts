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
