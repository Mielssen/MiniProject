const API = "https://localhost:7041/api";

function getToken() { return localStorage.getItem("token"); }
function authHeaders() {
    return { "Content-Type": "application/json", "Authorization": "Bearer " + getToken() };
}

async function safeJson(res) {
    try { return await res.json(); } catch { return null; }
}

function logout() {
    localStorage.removeItem("token");
    window.location.href = "index.html";
}

function switchTab(name, el) {
    document.querySelectorAll(".tab").forEach(t => t.classList.remove("active"));
    el.classList.add("active");

    ["statistics", "clients", "managers"].forEach(t => {
        document.getElementById("tab-" + t).classList.add("hidden");
    });
    document.getElementById("tab-" + name).classList.remove("hidden");

    if (name === "statistics") loadStatistics();
    if (name === "clients")    loadUsers("client", null);
    if (name === "managers")   loadUsers("manager", null);
}

async function loadStatistics() {
    const res = await fetch(API + "/admin/statistics", { headers: authHeaders() });
    if (!res.ok) return;
    const s = await safeJson(res);

    document.getElementById("statsGrid").innerHTML = `
        <div class="stat-card green">
            <div class="stat-icon">👥</div>
            <div class="stat-label">Всього користувачів</div>
            <div class="stat-value">${s.totalUsers}</div>
        </div>
        <div class="stat-card blue">
            <div class="stat-icon">🧑‍💼</div>
            <div class="stat-label">Менеджерів</div>
            <div class="stat-value">${s.totalManagers}</div>
        </div>
        <div class="stat-card orange">
            <div class="stat-icon">📋</div>
            <div class="stat-label">Всього бронювань</div>
            <div class="stat-value">${s.totalBookings}</div>
        </div>
        <div class="stat-card green">
            <div class="stat-icon">✅</div>
            <div class="stat-label">Підтверджених</div>
            <div class="stat-value">${s.confirmedBookings}</div>
        </div>
        <div class="stat-card red">
            <div class="stat-icon">❌</div>
            <div class="stat-label">Скасованих</div>
            <div class="stat-value">${s.cancelledBookings}</div>
        </div>
        <div class="stat-card green">
            <div class="stat-icon">💰</div>
            <div class="stat-label">Загальна виручка</div>
            <div class="stat-value">$${s.totalRevenue.toLocaleString()}</div>
        </div>
        <div class="stat-card blue">
            <div class="stat-icon">🏦</div>
            <div class="stat-label">Підтверджена виручка</div>
            <div class="stat-value">$${s.confirmedRevenue.toLocaleString()}</div>
        </div>
        <div class="stat-card orange">
            <div class="stat-icon">🆕</div>
            <div class="stat-label">Нових цього місяця</div>
            <div class="stat-value">${s.newUsersThisMonth}</div>
        </div>
    `;

    const ranks = ["rank-1", "rank-2", "rank-3"];
    document.getElementById("topToursList").innerHTML = s.topTours.map((t, i) => `
        <div class="top-tour-item">
            <div class="top-tour-rank ${ranks[i]}">${i + 1}</div>
            <div class="top-tour-info">
                <strong>${t.name}</strong>
                <span>${t.bookingsCount} бронювань</span>
            </div>
            <div class="top-tour-revenue">$${t.totalRevenue.toLocaleString()}</div>
        </div>
    `).join("") || "<p class='empty-state'>Немає даних</p>";
}

async function loadUsers(role, status) {
    let url = `${API}/admin/users?role=${role}`;
    if (status) url += `&status=${status}`;

    const res = await fetch(url, { headers: authHeaders() });
    if (!res.ok) return;
    const users = await safeJson(res);

    const containerId = role === "client" ? "clientsList" : "managersList";
    renderUsersTable(users, containerId);
}

function filterUsers(role, status, el) {
    const container = role === "client" ? "#tab-clients" : "#tab-managers";
    document.querySelectorAll(container + " .filter-btn").forEach(b => b.classList.remove("active"));
    el.classList.add("active");
    loadUsers(role, status);
}

function renderUsersTable(users, containerId) {
    if (!users || users.length === 0) {
        document.getElementById(containerId).innerHTML = "<p class='empty-state'>Немає користувачів</p>";
        return;
    }

    document.getElementById(containerId).innerHTML = `
        <div class="users-table">
            <table>
                <thead>
                    <tr>
                        <th>ID</th>
                        <th>Ім'я</th>
                        <th>Email</th>
                        <th>Телефон</th>
                        <th>Роль</th>
                        <th>Статус</th>
                        <th>Дата реєстрації</th>
                    </tr>
                </thead>
                <tbody>
                    ${users.map(u => `
                        <tr class="clickable-row" data-user='${JSON.stringify(u).replace(/'/g, "&#39;")}'>
                            <td>#${u.credentialId}</td>
                            <td>${u.name || "—"}</td>
                            <td>${u.email}</td>
                            <td>${u.phone || "—"}</td>
                            <td><span class="badge badge-${u.role}">${u.role}</span></td>
                            <td><span class="badge badge-${u.status}">${u.status}</span></td>
                            <td>${u.createdAt ? new Date(u.createdAt).toLocaleDateString("uk-UA") : "—"}</td>
                        </tr>
                    `).join("")}
                </tbody>
            </table>
        </div>
    `;

    document.querySelectorAll(`#${containerId} .clickable-row`).forEach(row => {
        row.addEventListener("click", () => {
            const user = JSON.parse(row.getAttribute("data-user"));
            openUserModal(user);
        });
    });
}

let currentUserId = null;

function openUserModal(user) {
    currentUserId = user.credentialId;

    const phone = user.phone?.startsWith("+38") ? user.phone.slice(3) : (user.phone || "");

    document.getElementById("editName").value    = user.name || "";
    document.getElementById("editPhone").value   = phone;
    document.getElementById("editPassport").value= user.passportData || "";
    document.getElementById("editBirth").value   = user.dateOfBirth
        ? new Date(user.dateOfBirth).toISOString().split("T")[0] : "";
    document.getElementById("editRole").value    = user.role;
    document.getElementById("editStatus").value  = user.status;
    document.getElementById("userModalError").innerText = "";

    document.getElementById("userModal").classList.remove("hidden");
}

function closeUserModal() {
    document.getElementById("userModal").classList.add("hidden");
    currentUserId = null;
}

async function saveUser() {
    document.getElementById("userModalError").innerText = "";

    const name     = document.getElementById("editName").value.trim();
    const phone    = document.getElementById("editPhone").value.trim();
    const passport = document.getElementById("editPassport").value.trim();
    const birth    = document.getElementById("editBirth").value;
    const role     = document.getElementById("editRole").value;
    const status   = document.getElementById("editStatus").value;

    if (!name || !/^[a-zA-Zа-яА-ЯіІїЇєЄ]+$/.test(name)) {
        document.getElementById("userModalError").innerText = "Ім'я має містити тільки літери";
        return;
    }

    if (passport && !/^\d+$/.test(passport)) {
        document.getElementById("userModalError").innerText = "Паспортні дані мають містити тільки цифри";
        return;
    }
    
    if (phone && !/^\d{10}$/.test(phone)) {
        document.getElementById("userModalError").innerText = "Телефон має містити рівно 10 цифр";
        return;
    }

    if (birth) {
        const age = new Date().getFullYear() - new Date(birth).getFullYear();
        const monthDiff = new Date().getMonth() - new Date(birth).getMonth();
        const realAge = monthDiff < 0 || (monthDiff === 0 && new Date().getDate() < new Date(birth).getDate())
            ? age - 1 : age;

        if (realAge < 18) {
            document.getElementById("userModalError").innerText = "Користувач має бути старше 18 років";
            return;
        }
    }

    const dataRes = await fetch(`${API}/admin/users/${currentUserId}/data`, {
        method: "PUT",
        headers: authHeaders(),
        body: JSON.stringify({
            name,
            phone: phone || null,
            passportData: passport || null,
            dateOfBirth: birth || null
        })
    });

    if (!dataRes.ok) {
        const err = await safeJson(dataRes);
        if (err?.errors) {
            const msgs = Object.values(err.errors).flat().join("\n");
            document.getElementById("userModalError").innerText = msgs;
        } else {
            document.getElementById("userModalError").innerText = err?.message || "Помилка оновлення даних";
        }
        return;
    }

    const roleRes = await fetch(`${API}/admin/users/${currentUserId}/role`, {
        method: "PUT",
        headers: authHeaders(),
        body: JSON.stringify({ role })
    });

    if (!roleRes.ok) {
        const err = await safeJson(roleRes);
        document.getElementById("userModalError").innerText = err?.message || "Помилка оновлення ролі";
        return;
    }

    const statusRes = await fetch(`${API}/admin/users/${currentUserId}/status`, {
        method: "PUT",
        headers: authHeaders(),
        body: JSON.stringify({ status })
    });

    if (!statusRes.ok) {
        const err = await safeJson(statusRes);
        document.getElementById("userModalError").innerText = err?.message || "Помилка оновлення статусу";
        return;
    }

    closeUserModal();
    document.querySelector(".tab.active").click();
}

window.onload = () => {
    const token = getToken();
    if (!token) { window.location.href = "index.html"; return; }
    loadStatistics();
};