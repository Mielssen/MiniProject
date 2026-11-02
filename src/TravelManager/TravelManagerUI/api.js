const API_BASE_URL = "http://localhost:5000";
const REQUEST_TIMEOUT = 12000;

class ApiError extends Error {
    constructor(message, status = 0, data = null, retryAfter = null) {
        super(message);
        this.name = "ApiError";
        this.status = status;
        this.data = data;
        this.retryAfter = retryAfter;
    }
}

const TravelApi = (() => {
    let refreshPromise = null;
    let blockedUntil = 0;

    function getAccessToken() {
        return localStorage.getItem("accessToken");
    }

    function getRefreshToken() {
        return localStorage.getItem("refreshToken");
    }

    function saveTokens(data) {
        if (!data?.accessToken) {
            throw new ApiError("Сервер не повернув токен авторизації.");
        }

        localStorage.setItem("accessToken", data.accessToken);

        if (data.refreshToken) {
            localStorage.setItem("refreshToken", data.refreshToken);
        }

        if (data.expiresAt) {
            localStorage.setItem("expiresAt", data.expiresAt);
        }

        localStorage.removeItem("token");
    }

    function clearTokens() {
        localStorage.removeItem("accessToken");
        localStorage.removeItem("refreshToken");
        localStorage.removeItem("expiresAt");
        localStorage.removeItem("token");
    }

    function getRole() {
        const token = getAccessToken();
        if (!token) return null;

        try {
            const payload = token.split(".")[1]
                .replace(/-/g, "+")
                .replace(/_/g, "/");

            const normalized = payload.padEnd(
                payload.length + (4 - payload.length % 4) % 4,
                "="
            );

            const decoded = decodeURIComponent(
                atob(normalized)
                    .split("")
                    .map(char =>
                        `%${char.charCodeAt(0).toString(16).padStart(2, "0")}`
                    )
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

    function parseRetryAfter(value) {
        if (!value) return null;

        const seconds = Number(value);
        if (Number.isFinite(seconds)) {
            return Math.max(0, seconds * 1000);
        }

        const date = new Date(value).getTime();
        return Number.isNaN(date) ? null : Math.max(0, date - Date.now());
    }

    function getErrorMessage(data, fallback) {
        if (typeof data === "string" && data.trim()) return data;
        if (data?.message) return data.message;
        if (data?.detail) return data.detail;

        if (data?.errors) {
            return Object.values(data.errors).flat().join("\n");
        }

        if (data?.title && data.title !== "One or more validation errors occurred.") {
            return data.title;
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

    function checkRateLimit() {
        const remaining = blockedUntil - Date.now();

        if (remaining > 0) {
            throw new ApiError(
                `Забагато запитів. Спробуйте ще раз через ${Math.ceil(remaining / 1000)} с.`,
                429,
                null,
                remaining
            );
        }
    }

    function updateRateLimit(response) {
        if (response.status !== 429) return null;

        const retryAfter = parseRetryAfter(
            response.headers.get("Retry-After")
        );

        if (retryAfter !== null) {
            blockedUntil = Math.max(blockedUntil, Date.now() + retryAfter);
        }

        return retryAfter;
    }

    async function send(path, options = {}, token = null) {
        checkRateLimit();

        const controller = new AbortController();
        const timeout = setTimeout(
            () => controller.abort(),
            options.timeout || REQUEST_TIMEOUT
        );

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

        if (
            body !== undefined &&
            !(body instanceof FormData) &&
            typeof body !== "string"
        ) {
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
                    "Сервер не відповів вчасно. Спробуйте ще раз."
                );
            }

            throw new ApiError(
                "Не вдалося з'єднатися із сервером. Перевірте доступність системи."
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
                        body: { refreshToken },
                        auth: false
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

    function redirectToLogin() {
        clearTokens();

        const currentPage = window.location.pathname
            .split("/")
            .pop()
            .toLowerCase();

        if (currentPage !== "index.html" && currentPage !== "") {
            window.location.replace("index.html");
        }
    }

    async function request(path, options = {}) {
        const requiresAuth = options.auth !== false;
        const isAuthRequest = path.startsWith("/api/v1/auth/");

        let token = requiresAuth ? getAccessToken() : null;

        let response = await send(path, options, token);
        let data = await parseResponse(response);

        if (response.status === 401 && requiresAuth && !isAuthRequest) {
            const refreshed = await refreshAccessToken();

            if (!refreshed) {
                redirectToLogin();
                throw new ApiError(
                    "Сесія завершилася. Увійдіть повторно.",
                    401,
                    data
                );
            }

            token = getAccessToken();
            response = await send(path, options, token);
            data = await parseResponse(response);

            if (response.status === 401) {
                redirectToLogin();
                throw new ApiError(
                    "Не вдалося підтвердити сесію. Увійдіть повторно.",
                    401,
                    data
                );
            }
        }

        if (response.status === 429) {
            const retryAfter = updateRateLimit(response);

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
            throw new ApiError(
                getErrorMessage(data, `Не вдалося виконати запит (${response.status}).`),
                response.status,
                data
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

    async function login(email, password) {
        clearTokens();

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
        } catch {
        } finally {
            clearTokens();
            window.location.replace("index.html");
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
