const API_ADMIN = "/api/admin";

let currentUserId = null;

function ensureAdminAccess() {
    if (!TravelApi.isAuthenticated()) {
        window.location.replace("index.html");
        return false;
    }

    if (TravelApi.getRole() !== "admin") {
        window.location.replace("dashboard.html");
        return false;
    }

    return true;
}

function logout() {
    TravelApi.logout();
}

function showAdminError(containerId, error, retry) {
    const container = document.getElementById(containerId);
    container.innerHTML = "";

    const message = document.createElement("p");
    message.className = "empty-state";
    message.textContent = error?.message || "Не вдалося завантажити дані.";

    container.appendChild(message);

    if (retry) {
        const button = document.createElement("button");
        button.className = "btn-save";
        button.textContent = "Повторити";
        button.onclick = retry;
        container.appendChild(button);
    }
}

function switchTab(name, element) {
    document.querySelectorAll(".tab").forEach(tab => {
        tab.classList.remove("active");
    });

    element.classList.add("active");

    ["statistics", "clients", "managers"].forEach(tab => {
        document.getElementById(`tab-${tab}`).classList.add("hidden");
    });

    document.getElementById(`tab-${name}`).classList.remove("hidden");

    if (name === "statistics") loadStatistics();
    if (name === "clients") loadUsers("client", null);
    if (name === "managers") loadUsers("manager", null);
}

async function loadStatistics() {
    const grid = document.getElementById("statsGrid");
    grid.innerHTML = "<p>Завантаження статистики...</p>";

    try {
        const statistics = await TravelApi.get(`${API_ADMIN}/statistics`);

        const cards = [
            ["👥", "Всього користувачів", statistics.totalUsers, "green"],
            ["🧑‍💼", "Менеджерів", statistics.totalManagers, "blue"],
            ["📋", "Всього бронювань", statistics.totalBookings, "orange"],
            ["✅", "Підтверджених", statistics.confirmedBookings, "green"],
            ["❌", "Скасованих", statistics.cancelledBookings, "red"],
            ["💰", "Загальна виручка", `$${Number(statistics.totalRevenue || 0).toLocaleString()}`, "green"],
            ["🏦", "Підтверджена виручка", `$${Number(statistics.confirmedRevenue || 0).toLocaleString()}`, "blue"],
            ["🆕", "Нових цього місяця", statistics.newUsersThisMonth, "orange"]
        ];

        grid.innerHTML = "";

        cards.forEach(([icon, label, value, color]) => {
            const card = document.createElement("div");
            card.className = `stat-card ${color}`;

            const iconElement = document.createElement("div");
            iconElement.className = "stat-icon";
            iconElement.textContent = icon;

            const labelElement = document.createElement("div");
            labelElement.className = "stat-label";
            labelElement.textContent = label;

            const valueElement = document.createElement("div");
            valueElement.className = "stat-value";
            valueElement.textContent = value ?? "0";

            card.append(iconElement, labelElement, valueElement);
            grid.appendChild(card);
        });

        renderTopTours(statistics.topTours || []);
    } catch (error) {
        showAdminError("statsGrid", error, loadStatistics);
        document.getElementById("topToursList").textContent =
            "Не вдалося завантажити популярні тури.";
    }
}

function renderTopTours(tours) {
    const container = document.getElementById("topToursList");
    container.innerHTML = "";

    if (!tours.length) {
        container.innerHTML = "<p class='empty-state'>Немає даних</p>";
        return;
    }

    const ranks = ["rank-1", "rank-2", "rank-3"];

    tours.forEach((tour, index) => {
        const item = document.createElement("div");
        item.className = "top-tour-item";

        const rank = document.createElement("div");
        rank.className = `top-tour-rank ${ranks[index] || ""}`;
        rank.textContent = String(index + 1);

        const info = document.createElement("div");
        info.className = "top-tour-info";

        const name = document.createElement("strong");
        name.textContent = tour.name || "Тур";

        const count = document.createElement("span");
        count.textContent = `${tour.bookingsCount || 0} бронювань`;

        info.append(name, count);

        const revenue = document.createElement("div");
        revenue.className = "top-tour-revenue";
        revenue.textContent =
            `$${Number(tour.totalRevenue || 0).toLocaleString()}`;

        item.append(rank, info, revenue);
        container.appendChild(item);
    });
}

async function loadUsers(role, status) {
    const containerId = role === "client" ? "clientsList" : "managersList";
    const container = document.getElementById(containerId);

    container.innerHTML = "<p>Завантаження користувачів...</p>";

    const params = new URLSearchParams({ role });

    if (status) {
        params.set("status", status);
    }

    try {
        const users = await TravelApi.get(
            `${API_ADMIN}/users?${params.toString()}`
        );

        renderUsersTable(
            Array.isArray(users) ? users : [],
            containerId
        );
    } catch (error) {
        showAdminError(
            containerId,
            error,
            () => loadUsers(role, status)
        );
    }
}

function filterUsers(role, status, element) {
    const selector = role === "client"
        ? "#tab-clients"
        : "#tab-managers";

    document.querySelectorAll(`${selector} .filter-btn`).forEach(button => {
        button.classList.remove("active");
    });

    element.classList.add("active");
    loadUsers(role, status);
}

function renderUsersTable(users, containerId) {
    const container = document.getElementById(containerId);
    container.innerHTML = "";

    if (!users.length) {
        container.innerHTML =
            "<p class='empty-state'>Немає користувачів</p>";
        return;
    }

    const wrapper = document.createElement("div");
    wrapper.className = "users-table";

    const table = document.createElement("table");
    const thead = document.createElement("thead");
    const headerRow = document.createElement("tr");

    ["ID", "Ім'я", "Email", "Телефон", "Роль", "Статус", "Дата реєстрації"]
        .forEach(label => {
            const th = document.createElement("th");
            th.textContent = label;
            headerRow.appendChild(th);
        });

    thead.appendChild(headerRow);

    const tbody = document.createElement("tbody");

    users.forEach(user => {
        const row = document.createElement("tr");
        row.className = "clickable-row";

        const values = [
            `#${user.credentialId}`,
            user.name || "—",
            user.email || "—",
            user.phone || "—",
            user.role || "—",
            user.status || "—",
            user.createdAt
                ? new Date(user.createdAt).toLocaleDateString("uk-UA")
                : "—"
        ];

        values.forEach((value, index) => {
            const cell = document.createElement("td");
            cell.textContent = value;

            if (index === 4 || index === 5) {
                const badge = document.createElement("span");
                badge.className = `badge badge-${value}`;
                badge.textContent = value;
                cell.textContent = "";
                cell.appendChild(badge);
            }

            row.appendChild(cell);
        });

        row.addEventListener("click", () => openUserModal(user));
        tbody.appendChild(row);
    });

    table.append(thead, tbody);
    wrapper.appendChild(table);
    container.appendChild(wrapper);
}

function openUserModal(user) {
    currentUserId = user.credentialId;

    const phone = user.phone?.startsWith("+38")
        ? user.phone.slice(3)
        : (user.phone || "");

    document.getElementById("editName").value = user.name || "";
    document.getElementById("editPhone").value = phone;
    document.getElementById("editPassport").value =
        user.passportData || "";
    document.getElementById("editBirth").value = user.dateOfBirth
        ? String(user.dateOfBirth).split("T")[0]
        : "";
    document.getElementById("editRole").value = user.role || "client";
    document.getElementById("editStatus").value = user.status || "active";
    document.getElementById("userModalError").textContent = "";

    document.getElementById("userModal").classList.remove("hidden");
}

function closeUserModal() {
    document.getElementById("userModal").classList.add("hidden");
    currentUserId = null;
}

function validateUserForm() {
    const name = document.getElementById("editName").value.trim();
    const phone = document.getElementById("editPhone").value.trim();
    const passport = document.getElementById("editPassport").value.trim();
    const birth = document.getElementById("editBirth").value;

    if (!name || !/^[a-zA-Zа-яА-ЯіІїЇєЄґҐ\s'-]+$/.test(name)) {
        return "Ім'я має містити тільки літери.";
    }

    if (passport && !/^\d+$/.test(passport)) {
        return "Паспортні дані мають містити тільки цифри.";
    }

    if (phone && !/^\d{10}$/.test(phone)) {
        return "Телефон має містити рівно 10 цифр.";
    }

    if (birth && new Date(birth) > new Date()) {
        return "Дата народження не може бути в майбутньому.";
    }

    return null;
}

async function saveUser() {
    const errorElement = document.getElementById("userModalError");
    const button = document.querySelector("#userModal .btn-save");

    errorElement.textContent = "";

    if (!currentUserId) {
        errorElement.textContent = "Не вибрано користувача.";
        return;
    }

    const validationError = validateUserForm();

    if (validationError) {
        errorElement.textContent = validationError;
        return;
    }

    const name = document.getElementById("editName").value.trim();
    const phone = document.getElementById("editPhone").value.trim();
    const passport = document.getElementById("editPassport").value.trim();
    const birth = document.getElementById("editBirth").value;
    const role = document.getElementById("editRole").value;
    const status = document.getElementById("editStatus").value;

    button.disabled = true;

    try {
        await TravelApi.put(
            `${API_ADMIN}/users/${currentUserId}/data`,
            {
                name,
                phone: phone || null,
                passportData: passport || null,
                dateOfBirth: birth || null
            }
        );

        await TravelApi.put(
            `${API_ADMIN}/users/${currentUserId}/role`,
            { role }
        );

        await TravelApi.put(
            `${API_ADMIN}/users/${currentUserId}/status`,
            { status }
        );

        closeUserModal();

        const activeTab = document.querySelector(".tab.active");

        if (activeTab) {
            activeTab.click();
        }
    } catch (error) {
        errorElement.textContent = error.message ||
            "Не вдалося зберегти зміни.";
    } finally {
        button.disabled = false;
    }
}

document.addEventListener("DOMContentLoaded", () => {
    if (!ensureAdminAccess()) return;
    loadStatistics();
});
