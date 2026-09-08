const API = "https://localhost:7041/api/auth";
const API_BOOKINGS = "https://localhost:7041/api/bookings";
const token = localStorage.getItem("token");

if (!token) {
    window.location.href = "index.html";
}

function switchTab(name) {
    document.querySelectorAll(".tab-content").forEach(t => t.classList.add("hidden"));
    document.querySelectorAll(".tab").forEach(t => t.classList.remove("active"));

    document.getElementById("tab-" + name).classList.remove("hidden");
    event.target.classList.add("active");

    if (name === "bookings") loadBookings();
}

async function loadProfile() {
    const res = await fetch(API + "/profile", {
        headers: { "Authorization": "Bearer " + token }
    });

    if (!res.ok) return;

    const data = await res.json();

    document.getElementById("profileEmail").innerText = data.email;
    document.getElementById("profileName").value = data.name || "";
    document.getElementById("profilePhone").value = data.phone?.replace("+38", "") || "";
    document.getElementById("profilePassport").value = data.passportData || "";

    if (data.dateOfBirth) {
        document.getElementById("profileBirth").value =
            new Date(data.dateOfBirth).toISOString().split("T")[0];
    }
}

async function saveProfile() {
    document.getElementById("profileError").innerText = "";
    document.getElementById("profileSuccess").innerText = "";

    const name = document.getElementById("profileName").value;
    const phone = document.getElementById("profilePhone").value;
    const passportData = document.getElementById("profilePassport").value;

    const res = await fetch(API + "/profile", {
        method: "PUT",
        headers: {
            "Content-Type": "application/json",
            "Authorization": "Bearer " + token
        },
        body: JSON.stringify({ name, phone, passportData })
    });

    if (!res.ok) {
        const err = await res.json();
        let msg = "";
        if (err?.errors) {
            for (let key in err.errors) {
                msg += err.errors[key].join("\n") + "\n";
            }
        } else {
            msg = err?.message || "Помилка";
        }
        document.getElementById("profileError").innerText = msg;
        return;
    }

    document.getElementById("profileSuccess").innerText = "Профіль успішно оновлено";
}

async function loadBookings() {
    const res = await fetch(API_BOOKINGS + "/my", {
        headers: { "Authorization": "Bearer " + token }
    });

    const data = await res.json();
    renderBookings(data);
}

function renderBookings(list) {
    const grid = document.getElementById("bookingsGrid");
    grid.innerHTML = "";

    if (list.length === 0) {
        grid.innerHTML = `
            <div class="empty-state">
                <div class="empty-icon">🧳</div>
                <p>Бронювань немає</p>
            </div>
        `;
        return;
    }

    list.forEach(b => {
        const card = document.createElement("div");
        card.className = "booking-card";

        const statusClass = `status-${b.status.toLowerCase()}`;

        card.innerHTML = `
            <div class="booking-info">
                <h4>${b.tourName}</h4>
                <span>Початок: ${b.startDate} · Заброньовано: ${formatDate(b.bookingDate)}</span>
            </div>
            <div class="booking-right">
                <span class="status-badge ${statusClass}">${b.status}</span>
                <button class="btn-cancel-booking">Скасувати</button>
            </div>
        `;

        card.querySelector(".btn-cancel-booking").onclick = () => cancelBooking(b.bookingId);
        grid.appendChild(card);
    });
}

async function cancelBooking(id) {
    if (!confirm("Скасувати бронювання?")) return;

    const res = await fetch(API_BOOKINGS + "/" + id, {
        method: "DELETE",
        headers: { "Authorization": "Bearer " + token }
    });

    if (!res.ok) {
        const err = await res.json();
        alert(err?.message || "Помилка");
        return;
    }

    loadBookings();
}

function formatDate(dateString) {
    if (!dateString) return "";
    return new Date(dateString).toLocaleDateString("uk-UA", {
        year: "numeric", month: "2-digit", day: "2-digit"
    });
}

function logout() {
    localStorage.removeItem("token");
    window.location.href = "index.html";
}

loadProfile();