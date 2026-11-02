const API_PROFILE = "/api/v1/users/me";
const API_PROFILE_UPDATE = "/api/v1/users/me/data";
const API_BOOKINGS = "/api/bookings";

function switchTab(name) {
    document.querySelectorAll(".tab-content").forEach(tab => tab.classList.add("hidden"));
    document.querySelectorAll(".tab").forEach(tab => tab.classList.remove("active"));

    document.getElementById("tab-" + name).classList.remove("hidden");

    const activeTab = document.querySelector(`.tab[onclick="switchTab('${name}')"]`);
    if (activeTab) activeTab.classList.add("active");

    if (name === "bookings") loadBookings();
}

function showProfileError(message) {
    document.getElementById("profileError").innerText = message || "Сталася помилка.";
}

function getApiErrorMessage(error) {
    if (error?.data?.errors) {
        return Object.values(error.data.errors).flat().join("\n");
    }

    return error?.message || "Не вдалося виконати запит.";
}

async function loadProfile() {
    try {
        const data = await TravelApi.get(API_PROFILE);

        document.getElementById("profileEmail").innerText = data.email || "";
        document.getElementById("profileName").value = data.displayName || "";
        document.getElementById("profilePhone").value = data.phone?.replace(/^\+38/, "") || "";
        document.getElementById("profilePassport").value = data.passportData || "";

        if (data.dateOfBirth) {
            const date = new Date(data.dateOfBirth);
            document.getElementById("profileBirth").value =
                new Date(date.getTime() - date.getTimezoneOffset() * 60000)
                    .toISOString()
                    .split("T")[0];
        }
    } catch (error) {
        showProfileError(getApiErrorMessage(error));
    }
}

async function saveProfile() {
    const errorElement = document.getElementById("profileError");
    const successElement = document.getElementById("profileSuccess");
    const saveButton = document.querySelector(".btn-save");

    errorElement.innerText = "";
    successElement.innerText = "";

    const name = document.getElementById("profileName").value.trim();
    const phoneValue = document.getElementById("profilePhone").value.trim();
    const phone = phoneValue ? phoneValue.replace(/\D/g, "") : null;
    const passportData = document.getElementById("profilePassport").value.trim();

    saveButton.disabled = true;

    try {
        const data = await TravelApi.put(API_PROFILE_UPDATE, {
            name,
            phone,
            passportData,
            dateOfBirth: document.getElementById("profileBirth").value || null
        });

        document.getElementById("profileName").value = data.displayName || name;
        document.getElementById("profilePhone").value =
            data.phone?.replace(/^\+38/, "") || phone || "";
        document.getElementById("profilePassport").value =
            data.passportData || passportData;

        successElement.innerText = "Профіль успішно оновлено";
    } catch (error) {
        showProfileError(getApiErrorMessage(error));
    } finally {
        saveButton.disabled = false;
    }
}

async function loadBookings() {
    const grid = document.getElementById("bookingsGrid");
    grid.innerHTML = "<div class=\"empty-state\"><p>Завантаження...</p></div>";

    try {
        const data = await TravelApi.get(API_BOOKINGS + "/my");
        renderBookings(Array.isArray(data) ? data : data?.items || []);
    } catch (error) {
        grid.innerHTML = `<div class="empty-state"><p>${getApiErrorMessage(error)}</p></div>`;
    }
}

function renderBookings(list) {
    const grid = document.getElementById("bookingsGrid");
    grid.innerHTML = "";

    if (!list.length) {
        grid.innerHTML = `
            <div class="empty-state">
                <div class="empty-icon">🧳</div>
                <p>Бронювань немає</p>
            </div>
        `;
        return;
    }

    list.forEach(booking => {
        const card = document.createElement("div");
        card.className = "booking-card";

        const status = String(booking.status || "").toLowerCase();
        card.innerHTML = `
            <div class="booking-info">
                <h4>${booking.tourName || "Тур"}</h4>
                <span>Початок: ${booking.startDate || ""} · Заброньовано: ${formatDate(booking.bookingDate)}</span>
            </div>
            <div class="booking-right">
                <span class="status-badge status-${status}">${booking.status || ""}</span>
                <button class="btn-cancel-booking">Скасувати</button>
            </div>
        `;

        card.querySelector(".btn-cancel-booking").onclick =
            () => cancelBooking(booking.bookingId);
        grid.appendChild(card);
    });
}

async function cancelBooking(id) {
    if (!confirm("Скасувати бронювання?")) return;

    try {
        await TravelApi.delete(API_BOOKINGS + "/" + id);
        await loadBookings();
    } catch (error) {
        alert(getApiErrorMessage(error));
    }
}

function formatDate(dateString) {
    if (!dateString) return "";
    return new Date(dateString).toLocaleDateString("uk-UA", {
        year: "numeric",
        month: "2-digit",
        day: "2-digit"
    });
}

async function logout() {
    try {
        await TravelApi.logout();
    } finally {
        window.location.href = "index.html";
    }
}

if (!TravelApi.isAuthenticated()) {
    window.location.href = "index.html";
} else {
    loadProfile();
}
