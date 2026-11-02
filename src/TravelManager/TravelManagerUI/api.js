
const API_BASE_URL = "http://localhost:5000";

class ApiError extends Error {
    constructor(message, status, data = null, retryAfter = null) {
        super(message);
        this.name = "ApiError";
        this.status = status;
        this.data = data;
        this.retryAfter = retryAfter;
    }
}

const TravelApi = (() => {
    let refreshPromise = null;

    function getAccessToken() {
        return localStorage.getItem("accessToken");
    }

    function getRefreshToken() {
        return localStorage.getItem("refreshToken");
    }

    function saveTokens(data) {
        if (!data?.accessToken) {
            throw new Error("Сервер не повернув access token.");
        }

        localStorage.setItem("accessToken", data.accessToken);

        if (data.refreshToken) {
            localStorage.setItem("refreshToken", data.refreshToken);
        }

        if (data.expiresAt) {
            localStorage.setItem("expiresAt", data.expiresAt);
        }
    }

    function clearTokens() {
        localStorage.removeItem("accessToken");
        localStorage.removeItem("refreshToken");
        localStorage.removeItem("expiresAt");
    }

    function parseRetryAfter(value) {
        if (!value) return null;

        const seconds = Number(value);

        if (Number.isFinite(seconds)) {
            return Math.max(0, seconds * 1000);
        }

        const date = new Date(value).getTime();

        if (!Number.isNaN(date)) {
            return Math.max(0, date - Date.now());
        }

        return null;
    }

    function getErrorMessage(data, fallback) {
        if (typeof data === "string" && data.trim()) {
            return data;
        }

        if (data?.message) return data.message;
        if (data?.title) return data.title;
        if (data?.detail) return data.detail;

        if (data?.errors) {
            return Object.values(data.errors)
                .flat()
                .join("\n");
        }

        return fallback;
    }

    async function parseResponse(response) {
        const text = await response.text();

        if (!text) return null;

        try {
            return JSON.parse(text);
        } catch {
            return text;
        }
    }

    async function send(path, options = {}, token = null) {
        const controller = new AbortController();
        const timeout = setTimeout(() => controller.abort(), options.timeout || 12000);

        const headers = new Headers(options.headers || {});

        if (options.body !== undefined && !(options.body instanceof FormData)) {
            headers.set("Content-Type", "application/json");
        }

        if (token) {
            headers.set("Authorization", `Bearer ${token}`);
        }

        if (options.idempotencyKey) {
            headers.set("Idempotency-Key", options.idempotencyKey);
        }

        let body = options.body;

        if (body !== undefined && !(body instanceof FormData) && typeof body !== "string") {
            body = JSON.stringify(body);
        }

        try {
            return await fetch(`${API_BASE_URL}${path}`, {
                method: options.method || "GET",
                headers,
                body,
                signal: controller.signal
            });
        } catch (error) {
            if (error.name === "AbortError") {
                throw new ApiError(
                    "Час очікування відповіді сервера вичерпано. Спробуйте ще раз.",
                    0
                );
            }

            throw new ApiError(
                "Не вдалося з'єднатися із сервером. Перевірте, чи запущений API Gateway.",
                0
            );
        } finally {
            clearTimeout(timeout);
        }
    }

    async function refreshAccessToken() {
        if (refreshPromise) return refreshPromise;

        refreshPromise = (async () => {
            const refreshToken = getRefreshToken();

            if (!refreshToken) {
                clearTokens();
                return false;
            }

            try {
                const response = await send(
                    "/api/v1/auth/refresh",
                    {
                        method: "POST",
                        body: { refreshToken }
                    }
                );

                const data = await parseResponse(response);

                if (!response.ok || !data?.accessToken) {
                    clearTokens();
                    return false;
                }

                saveTokens(data);
                return true;
            } catch {
                clearTokens();
                return false;
            } finally {
                refreshPromise = null;
            }
        })();

        return refreshPromise;
    }

    async function request(path, options = {}) {
        const method = (options.method || "GET").toUpperCase();
        const requiresAuth = options.auth !== false;
        const isAuthRequest = path.startsWith("/api/v1/auth/");

        let token = requiresAuth ? getAccessToken() : null;

        let response = await send(path, options, token);
        let data = await parseResponse(response);

        if (response.status === 401 && requiresAuth && !isAuthRequest) {
            const refreshed = await refreshAccessToken();

            if (refreshed) {
                token = getAccessToken();
                response = await send(path, options, token);
                data = await parseResponse(response);
            } else {
                clearTokens();

                if (!options.suppressAuthRedirect) {
                    window.location.href = "index.html";
                }

                throw new ApiError("Сесія завершилася. Увійдіть повторно.", 401, data);
            }
        }

        if (response.status === 429) {
    const retryAfter = parseRetryAfter(
        response.headers.get("Retry-After")
    );

    throw new ApiError(
        retryAfter !== null
            ? `Забагато запитів. Повторіть спробу через ${Math.ceil(retryAfter / 1000)} с.`
            : "Забагато запитів. Спробуйте пізніше.",
        429,
        data,
        retryAfter
    );
}

        if (!response.ok) {
            const retryAfter = parseRetryAfter(response.headers.get("Retry-After"));

            throw new ApiError(
                getErrorMessage(data, `Помилка сервера: HTTP ${response.status}`),
                response.status,
                data,
                retryAfter
            );
        }

        return data;
    }

    function get(path, options = {}) {
        return request(path, { ...options, method: "GET" });
    }

    function post(path, body, options = {}) {
        return request(path, { ...options, method: "POST", body });
    }

    function put(path, body, options = {}) {
        return request(path, { ...options, method: "PUT", body });
    }

    function deleteRequest(path, options = {}) {
        return request(path, { ...options, method: "DELETE" });
    }

    function getRole() {
        const token = getAccessToken();

        if (!token) return null;

        try {
            const payload = token.split(".")[1]
                .replace(/-/g, "+")
                .replace(/_/g, "/");

            const decoded = decodeURIComponent(
                atob(payload)
                    .split("")
                    .map(char => `%${char.charCodeAt(0).toString(16).padStart(2, "0")}`)
                    .join("")
            );

            const claims = JSON.parse(decoded);

            return claims["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"]
                || claims["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/role"]
                || claims.role
                || claims.roles
                || null;
        } catch {
            return null;
        }
    }

    async function login(email, password) {
        const data = await post(
            "/api/v1/auth/login",
            { email, password },
            { auth: false }
        );

        saveTokens(data);
        return data;
    }

    async function register(userData) {
        const data = await post(
            "/api/v1/auth/register",
            userData,
            { auth: false }
        );

        if (data?.accessToken) {
            saveTokens(data);
        }

        return data;
    }

    async function logout() {
        const refreshToken = getRefreshToken();

        try {
            if (refreshToken && getAccessToken()) {
                await post(
                    "/api/v1/auth/logout",
                    { refreshToken }
                );
            }
        } finally {
            clearTokens();
        }
    }

    return {
        get,
        post,
        put,
        delete: deleteRequest,
        login,
        register,
        logout,
        getRole,
        getAccessToken,
        getRefreshToken,
        clearTokens,
        isAuthenticated: () => Boolean(getAccessToken())
    };
})();

window.TravelApi = TravelApi;
window.ApiError = ApiError;
