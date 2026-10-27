// 由 scripts/gen-contracts.mjs 从 contracts/openapi.json 生成；不要手改，改后端契约后执行 pnpm gen:contracts。
/* eslint-disable */
export interface paths {
    "/api/users": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        /** Create a user in the current tenant; requires the identity.users.create permission. */
        post: operations["users.create"];
        delete?: never;
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
        /** Get a user of the current tenant by ID; requires the identity.users.read permission. */
        get: operations["users.get"];
        put?: never;
        post?: never;
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
        /** List users of the current tenant with paging, filters, keyword and sorting; requires the identity.users.read permission. */
        post: operations["users.list"];
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
            /** Format: int32 */
            skip?: number;
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
            homeTenantId: string;
            /** @enum {string} */
            status: "Active" | "Disabled" | "Locked";
            /** Format: date-time */
            createdAt: string;
            createdBy?: string | null;
        };
    };
    responses: never;
    parameters: never;
    requestBodies: never;
    headers: never;
    pathItems: never;
}
export type $defs = Record<string, never>;
export interface operations {
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
                        homeTenantId: string;
                        /** @enum {string} */
                        status: "Active" | "Disabled" | "Locked";
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
                        homeTenantId: string;
                        /** @enum {string} */
                        status: "Active" | "Disabled" | "Locked";
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
}
