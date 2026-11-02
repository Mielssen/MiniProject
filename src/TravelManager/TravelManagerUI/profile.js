
const API_PROFILE = "/api/v1/users/me";
const API_PROFILE_UPDATE = "/api/v1/users/me/data";
const API_BOOKINGS = "/api/bookings";

function showProfileError(message) {
    document.getElementById("profileError").textContent =
        message || "Сталася помилка.";
    document.getElementById("profileSuccess").textContent = "";
}

function getApiErrorMessage(error) {
    if (error?.data?.errors) {
        return Object.values(error.data.errors).flat().join("\n");
    }

    return error?.message || "Не вдалося виконати запит.";
}

function switchTab(name) {
    document.querySelectorAll(".tab-content").forEach(tab => {
        tab.classList.add("hidden");
    });

    document.querySelectorAll(".tab").forEach(tab => {
        tab.classList.remove("active");
    });

    document.getElementById("tab-" + name).classList.remove("hidden");

    const activeTab = document.querySelector(
        `.tab[onclick="switchTab('${name}')"]`
    );

    if (activeTab) activeTab.classList.add("active");

    if (name === "bookings") loadBookings();
}

function formatDate(value) {
    if (!value) return "—";

    const date = new Date(value);

    if (Number.isNaN(date.getTime())) return "—";

    return date.toLocaleDateString("uk-UA");
}

function dateInputValue(value) {
    return value ? String(value).split("T")[0] : "";
}

async function loadProfile() {
    try {
        const data = await TravelApi.get(API_PROFILE);

        document.getElementById("profileEmail").textContent =
            data.email || "";

        document.getElementById("profileName").value =
            data.displayName || "";

        document.getElementById("profilePhone").value =
            data.phone?.replace(/^\+38/, "") || "";

        document.getElementById("profilePassport").value =
            data.passportData || "";

        document.getElementById("profileBirth").value =
            dateInputValue(data.dateOfBirth);
    } catch (error) {
        showProfileError(getApiErrorMessage(error));
    }
}

async function saveProfile() {
    const errorElement = document.getElementById("profileError");
    const successElement = document.getElementById("profileSuccess");
    const saveButton = document.querySelector(".btn-save");

    errorElement.textContent = "";
    successElement.textContent = "";

    const name = document.getElementById("profileName").value.trim();
    const phoneValue = document.getElementById("profilePhone").value.trim();
    const phone = phoneValue ? phoneValue.replace(/\D/g, "") : null;
    const passportData =
        document.getElementById("profilePassport").value.trim();

    saveButton.disabled = true;

    try {
        const data = await TravelApi.put(API_PROFILE_UPDATE, {
            name,
            phone,
            passportData
        });

        document.getElementById("profileName").value =
            data.displayName || name;

        document.getElementById("profilePhone").value =
            data.phone?.replace(/^\+38/, "") || phone || "";

        document.getElementById("profilePassport").value =
            data.passportData || passportData;

        successElement.textContent = "Профіль успішно оновлено.";
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
        grid.innerHTML = "";

        const message = document.createElement("p");
        message.textContent = getApiErrorMessage(error);
        grid.appendChild(message);

        const retry = document.createElement("button");
        retry.className = "btn-save";
        retry.textContent = "Повторити";
        retry.onclick = loadBookings;
        grid.appendChild(retry);
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

        const info = document.createElement("div");
        info.className = "booking-info";

        const title = document.createElement("h4");
        title.textContent = booking.tourName || "Тур";

        const dates = document.createElement("span");
        dates.textContent =
            `Початок: ${formatDate(booking.startDate)} · ` +
            `Заброньовано: ${formatDate(booking.bookingDate)}`;

        info.append(title, dates);

        const right = document.createElement("div");
        right.className = "booking-right";

        const statusValue = String(booking.status || "").toLowerCase();

        const status = document.createElement("span");
        status.className = `status-badge status-${statusValue}`;
        status.textContent = booking.status || "—";

        right.appendChild(status);

        if (statusValue !== "cancelled") {
            const cancel = document.createElement("button");
            cancel.className = "btn-cancel-booking";
            cancel.textContent = "Скасувати";
            cancel.onclick = () => cancelBooking(booking.bookingId);
            right.appendChild(cancel);
        }

        card.append(info, right);
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
