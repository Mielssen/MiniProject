function get(id) {
    return document.getElementById(id);
}

function clearErrors() {
    document.querySelectorAll(".error").forEach(element => {
        element.textContent = "";
    });
}

function showError(id, error) {
    const element = get(id);
    if (!element) return;

    const data = error?.data;

    if (data?.errors) {
        element.textContent = Object.values(data.errors).flat().join("\n");
        return;
    }

    if (error?.status === 401) {
        element.textContent = "Неправильний email або пароль.";
        return;
    }

    if (error?.status === 429) {
        element.textContent = error.message;
        return;
    }

    if (error?.status >= 500) {
        element.textContent = "Сервер тимчасово недоступний. Спробуйте пізніше.";
        return;
    }

    element.textContent = error?.message || "Не вдалося виконати запит.";
}

function redirectByRole() {
    const role = TravelApi.getRole();

    if (role === "manager") {
        window.location.replace("manager.html");
    } else if (role === "admin") {
        window.location.replace("admin.html");
    } else {
        window.location.replace("dashboard.html");
    }
}

async function login() {
    clearErrors();

    const email = get("loginEmail").value.trim();
    const password = get("loginPassword").value;
    const button = document.querySelector("#loginBox button");

    if (!email || !password) {
        showError("loginError", {
            message: "Заповніть email і пароль."
        });
        return;
    }

    button.disabled = true;

    try {
        await TravelApi.login(email, password);
        redirectByRole();
    } catch (error) {
        showError("loginError", error);
    } finally {
        button.disabled = false;
    }
}

async function register() {
    clearErrors();

    const userData = {
        name: get("regName").value.trim(),
        email: get("regEmail").value.trim(),
        password: get("regPassword").value,
        phone: get("regPhone").value.trim(),
        passportData: get("regPassport").value.trim(),
        dateOfBirth: get("regBirth").value
    };

    const button = document.querySelector("#registerBox button");

    if (Object.values(userData).some(value => !value)) {
        showError("registerError", {
            message: "Заповніть усі поля."
        });
        return;
    }

    button.disabled = true;

    try {
        const result = await TravelApi.register(userData);

        if (result?.accessToken) {
            redirectByRole();
        } else {
            showLogin();
            get("loginError").textContent =
                "Реєстрація успішна. Тепер увійдіть у свій акаунт.";
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
        redirectByRole();
    }
});
