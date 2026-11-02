const LOGIN_MESSAGES = {
    INVALID_CREDENTIALS: "Неправильний email або пароль.",
    USER_DELETED: "Цей обліковий запис видалено.",
    EMAIL_EXISTS: "Користувач із таким email вже існує."
};

function get(id) {
    return document.getElementById(id);
}

function clearErrors() {
    document.querySelectorAll(".error").forEach(element => {
        element.innerText = "";
    });
}

function showError(id, error) {
    const element = get(id);
    if (!element) return;

    const data = error?.data || error;

    if (data?.errors) {
        element.innerText = Object.values(data.errors).flat().join("\n");
        return;
    }

    if (data?.title && LOGIN_MESSAGES[data.title]) {
        element.innerText = LOGIN_MESSAGES[data.title];
        return;
    }

    if (data?.detail) {
        element.innerText = data.detail;
        return;
    }

    if (data?.message) {
        element.innerText = data.message;
        return;
    }

    if (error?.status === 401) {
        element.innerText = "Неправильний email або пароль.";
        return;
    }

    if (error?.status >= 500) {
        element.innerText = "Помилка сервера. Спробуйте пізніше.";
        return;
    }

    element.innerText = error?.message || "Не вдалося виконати запит.";
}

async function login() {
    clearErrors();

    const email = get("loginEmail").value.trim();
    const password = get("loginPassword").value;

    if (!email || !password) {
        showError("loginError", {
            message: "Заповніть email і пароль."
        });
        return;
    }

    const button = document.querySelector("#loginBox button");
    button.disabled = true;

    try {
        await TravelApi.login(email, password);

        const role = TravelApi.getRole();

        if (role === "manager") {
            window.location.href = "manager.html";
        } else if (role === "admin") {
            window.location.href = "admin.html";
        } else {
            window.location.href = "dashboard.html";
        }
    } catch (error) {
        showError("loginError", error);
    } finally {
        button.disabled = false;
    }
}

async function register() {
    clearErrors();

    const name = get("regName").value.trim();
    const email = get("regEmail").value.trim();
    const password = get("regPassword").value;
    const phone = get("regPhone").value.trim();
    const passportData = get("regPassport").value.trim();
    const dateOfBirth = get("regBirth").value;

    if (!name || !email || !password || !phone || !passportData || !dateOfBirth) {
        showError("registerError", {
            message: "Заповніть усі поля."
        });
        return;
    }

    const button = document.querySelector("#registerBox button");
    button.disabled = true;

    try {
        await TravelApi.register({
            email,
            password,
            name,
            phone,
            passportData,
            dateOfBirth
        });

        if (TravelApi.isAuthenticated()) {
            const role = TravelApi.getRole();

            if (role === "manager") {
                window.location.href = "manager.html";
            } else if (role === "admin") {
                window.location.href = "admin.html";
            } else {
                window.location.href = "dashboard.html";
            }
        } else {
            showLogin();
            showError("loginError", {
                message: "Реєстрація успішна. Тепер увійдіть у свій акаунт."
            });
        }
    } catch (error) {
        showError("registerError", error);
    } finally {
        button.disabled = false;
    }
}

function showRegister() {
    clearErrors();
    get("loginBox").classList.add("hidden");
    get("registerBox").classList.remove("hidden");
}

function showLogin() {
    clearErrors();
    get("registerBox").classList.add("hidden");
    get("loginBox").classList.remove("hidden");
}

document.addEventListener("DOMContentLoaded", () => {
    if (TravelApi.isAuthenticated()) {
        const role = TravelApi.getRole();

        if (role === "manager") {
            window.location.href = "manager.html";
        } else if (role === "admin") {
            window.location.href = "admin.html";
        } else {
            window.location.href = "dashboard.html";
        }
    }
});