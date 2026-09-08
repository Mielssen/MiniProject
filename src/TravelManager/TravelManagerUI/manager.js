const API = "https://localhost:7041/api/tours";
const token = localStorage.getItem("token");

let editId = null;
let assets = [];
let tourTypes = [];

if (!token) {
    window.location.href = "index.html";
}

async function loadTourTypes() {
    const res = await fetch("https://localhost:7041/api/tours/types");
    tourTypes = await res.json();

    const select = document.getElementById("typeId");
    select.innerHTML = tourTypes.map(t =>
        `<option value="${t.typeId}">${t.name}</option>`
    ).join("");
}

function renderPreview() {
    const preview = document.getElementById("preview");
    preview.innerHTML = "";

    assets.forEach((asset, index) => {
        const wrapper = document.createElement("div");
        wrapper.style.cssText = "position:relative; display:inline-block; margin:4px;";

        if (asset.assetType === "image") {
            const img = document.createElement("img");
            img.src = asset.url;
            img.className = "preview-item";
            wrapper.appendChild(img);
        } else if (asset.assetType === "video") {
            const video = document.createElement("video");
            video.src = asset.url;
            video.style.cssText = "width:100px; height:60px; object-fit:cover; border-radius:10px;";
            video.controls = true;
            wrapper.appendChild(video);
        }

        const btn = document.createElement("button");
        btn.innerText = "✕";
        btn.style.cssText = `
            position:absolute; top:2px; right:2px;
            background:red; color:white; border:none;
            border-radius:50%; width:20px; height:20px;
            cursor:pointer; font-size:12px; line-height:1;
        `;
        btn.onclick = () => {
            assets.splice(index, 1);
            renderPreview();
        };

        wrapper.appendChild(btn);
        preview.appendChild(wrapper);
    });
}

document.getElementById("files")?.addEventListener("change", (e) => {
    const files = Array.from(e.target.files);

    for (const file of files) {
        assets.push({
            assetType: file.type.startsWith("video") ? "video" : "image",
            url: URL.createObjectURL(file),
            _file: file
        });
    }

    renderPreview();
    e.target.value = "";
});

async function loadTours() {
    const res = await fetch(API);
    const data = await res.json();
    renderTours(data);
}

function formatDate(dateString) {
    if (!dateString) return "";
    return new Date(dateString).toLocaleDateString("uk-UA", {
        year: "numeric",
        month: "2-digit",
        day: "2-digit"
    });
}

function renderTours(list) {
    const grid = document.getElementById("toursGrid");
    grid.innerHTML = "";

    list.forEach(t => {
        const card = document.createElement("div");
        card.className = "card";

        const mediaAssets = t.assets || [];

        let mediaHtml = "";

        if (mediaAssets.length > 0) {
            mediaHtml = `
                <div style="display:flex; gap:6px; margin:10px 0; flex-wrap:wrap;">
                    ${mediaAssets.map(a => {
                        if (a.assetType === "image") {
                            return `<img src="${a.url}" style="
                                width:60px; height:60px;
                                object-fit:cover;
                                border-radius:10px;
                                border:1px solid #eee;">`;
                        } else if (a.assetType === "video") {
                            return `<video src="${a.url}" style="
                                width:100px; height:60px;
                                object-fit:cover;
                                border-radius:10px;"
                                controls></video>`;
                        }
                        return "";
                    }).join("")}
                </div>
            `;
        }

        card.innerHTML = `
        <div class="card-header">
            <h4>${t.name}</h4>
             <span>${tourTypes.find(type => type.typeId === t.typeId)?.name || t.typeId}</span>
        </div>

            ${mediaHtml}

            <p class="desc">${t.description || ""}</p>

            <div class="meta">
                <span>${t.price} $</span>
                <span>${t.durationDays} дн.</span>
            </div>

            <div class="meta">
                <span>${formatDate(t.availableFrom)} → ${formatDate(t.availableTo)}</span>
            </div>

            <div class="actions">
                <button class="btn-edit">Редагувати</button>
                <button class="btn-delete">Видалити</button>
            </div>
        `;

        card.querySelector(".btn-edit").onclick = () => editTour(t);
        card.querySelector(".btn-delete").onclick = () => deleteTour(t.tourId);

        grid.appendChild(card);
    });
}

function searchTours() {
    const value = document.getElementById("search").value.trim();
    if (!value) return;

    fetch(`${API}?search=${encodeURIComponent(value)}`)
        .then(r => r.json())
        .then(data => {
            renderTours(data);
            document.getElementById("searchText").innerText = value;
            document.getElementById("searchChip").classList.remove("hidden");
        });
}

function clearSearch() {
    document.getElementById("search").value = "";
    document.getElementById("searchChip").classList.add("hidden");
    loadTours();
}

function openModal(isEdit = false) {
    assets = [];
    document.getElementById("preview").innerHTML = "";

    document.getElementById("modalTitle").innerText =
        isEdit ? "Редагувати тур" : "Створити тур";

    document.getElementById("modal").classList.remove("hidden");

    if (!isEdit) {
        editId = null;

        ["name", "description", "price", "days", "from", "to", "typeId"]
            .forEach(id => document.getElementById(id).value = "");
    }
}

function closeModal() {
    document.getElementById("modal").classList.add("hidden");
}

function editTour(tour) {
    editId = tour.tourId;

    openModal(true);

    assets = tour.assets ? tour.assets.map(a => ({
        assetType: a.assetType.toLowerCase(),
        url: a.url
    })) : [];

    document.getElementById("name").value = tour.name;
    document.getElementById("description").value = tour.description || "";
    document.getElementById("price").value = tour.price;
    document.getElementById("days").value = tour.durationDays;
    document.getElementById("from").value = tour.availableFrom?.split("T")[0];
    document.getElementById("to").value = tour.availableTo?.split("T")[0];
    document.getElementById("typeId").value = tour.typeId;

    renderPreview();
}

async function saveTour() {
    const name = document.getElementById("name").value;
    const description = document.getElementById("description").value;
    const price = document.getElementById("price").value;
    const days = document.getElementById("days").value;
    const from = document.getElementById("from").value;
    const to = document.getElementById("to").value;
    const typeId = document.getElementById("typeId").value;

    if (!name || !price || !days || !from || !to || !typeId) {
        alert("Заповніть всі поля");
        return;
    }

    const tourData = {
        name,
        description,
        price: +price,
        durationDays: +days,
        availableFrom: from,
        availableTo: to,
        typeId: +typeId,
        assets: assets.filter(a => !a._file)
    };

    const tourRes = await fetch(API + (editId ? "/" + editId : ""), {
        method: editId ? "PUT" : "POST",
        headers: {
            "Content-Type": "application/json",
            "Authorization": "Bearer " + token
        },
        body: JSON.stringify(tourData)
    });

    if (!tourRes.ok) {
        const err = await tourRes.json();
        alert(err?.message || "Помилка");
        return;
    }

    let currentId = editId;
    if (!editId) {
        const created = await tourRes.json();
        currentId = created.tourId;
    }

    for (const asset of assets) {
        if (asset._file) {
            const formData = new FormData();
            formData.append("file", asset._file);

            await fetch(`${API}/${currentId}/images`, {
                method: "POST",
                headers: { "Authorization": "Bearer " + token },
                body: formData
            });
        }
    }

    assets = [];
    document.getElementById("preview").innerHTML = "";

    closeModal();
    loadTours();
}

async function deleteTour(id) {
    if (!confirm("Видалити тур?")) return;

    await fetch(API + "/" + id, {
        method: "DELETE",
        headers: { "Authorization": "Bearer " + token }
    });

    loadTours();
}

function logout() {
    localStorage.removeItem("token");
    window.location.href = "index.html";
}

async function init() {
    await loadTourTypes();
    await loadTours();
}
let currentStatusFilter = null;

function switchTab(name, btn) {
    document.getElementById("tab-tours").classList.add("hidden");
    document.getElementById("tab-bookings").classList.add("hidden");
    document.querySelectorAll(".tab").forEach(t => t.classList.remove("active"));

    document.getElementById("tab-" + name).classList.remove("hidden");
    btn.classList.add("active");

    if (name === "bookings") loadBookings();
}

async function loadBookings(status = null) {
    currentStatusFilter = status;
    const url = status
        ? `https://localhost:7041/api/bookings?status=${status}`
        : `https://localhost:7041/api/bookings`;

    const res = await fetch(url, {
        headers: { "Authorization": "Bearer " + token }
    });

    const data = await res.json();
    renderBookings(data);
}

function filterBookings(status, btn) {
    document.querySelectorAll(".filter-btn").forEach(b => b.classList.remove("active"));
    btn.classList.add("active");
    loadBookings(status);
}

function renderBookings(list) {
    const container = document.getElementById("bookingsList");
    container.innerHTML = "";

    if (list.length === 0) {
        container.innerHTML = "<p style='color:#636e72;'>Бронювань немає</p>";
        return;
    }

    list.forEach(b => {
        const row = document.createElement("div");
        row.className = "booking-row";
        row.style.cursor = "pointer";

        const statusClass = `status-${b.status.toLowerCase()}`;

        const actions = b.status === "pending" ? `
            <button class="btn-confirm" onclick="event.stopPropagation(); updateBookingStatus(${b.bookingId}, 'confirmed')">✓</button>
            <button class="btn-decline" onclick="event.stopPropagation(); updateBookingStatus(${b.bookingId}, 'cancelled')">✕</button>
        ` : "";

        row.innerHTML = `
            <div class="booking-row-left">
                <h4>${b.tourName}</h4>
                <span>${b.userName || "—"} · ${b.userEmail || "—"}</span>
                <span style="display:block; margin-top:3px;">
                    Початок: ${b.startDate} · ${b.numberOfPeople} ос. · ${b.totalPrice} $
                </span>
            </div>
            <div class="booking-row-right">
                <span class="status-badge ${statusClass}">${b.status}</span>
                ${actions}
            </div>
        `;

        row.onclick = () => openBookingDetailModal(b);
        container.appendChild(row);
    });
}

function openBookingDetailModal(b) {
    document.getElementById("detailTourName").innerText = b.tourName;
    document.getElementById("detailTourPrice").innerText = b.tourPrice + " $";
    document.getElementById("detailTourDays").innerText = b.tourDurationDays + " дн.";

    document.getElementById("detailUserName").innerText = b.userName || "—";
    document.getElementById("detailUserEmail").innerText = b.userEmail || "—";
    document.getElementById("detailUserPhone").innerText = b.userPhone || "—";
    document.getElementById("detailUserPassport").innerText = b.userPassportData || "—";
    document.getElementById("detailUserBirth").innerText = b.userDateOfBirth
        ? formatDate(b.userDateOfBirth) : "—";

    document.getElementById("detailStartDate").innerText = b.startDate;
    document.getElementById("detailBookingDate").innerText = formatDate(b.bookingDate);
    document.getElementById("detailPeople").innerText = b.numberOfPeople;
    document.getElementById("detailTotal").innerText = b.totalPrice + " $";
    document.getElementById("detailComment").innerText = b.comment || "—";

    const statusClass = `status-${b.status.toLowerCase()}`;
    document.getElementById("detailStatus").innerHTML =
        `<span class="status-badge ${statusClass}">${b.status}</span>`;

    // Учасники
    const personsContainer = document.getElementById("detailPersons");
    personsContainer.innerHTML = "";

    if (b.persons && b.persons.length > 0) {
        b.persons.forEach((p, i) => {
            personsContainer.innerHTML += `
                <div class="detail-person">
                    <div class="detail-person-header">
                        <span>${p.isChild ? "👦 Дитина" : "👤 Дорослий"} ${i + 1}</span>
                    </div>
                    <div class="detail-grid">
                        <div class="detail-item"><span>Ім'я</span><strong>${p.name}</strong></div>
                        <div class="detail-item"><span>Паспорт</span><strong>${p.passportData || "—"}</strong></div>
                        <div class="detail-item"><span>Дата народження</span><strong>${p.dateOfBirth || "—"}</strong></div>
                    </div>
                </div>
            `;
        });
    } else {
        personsContainer.innerHTML = "<p style='color:#636e72; font-size:13px;'>Немає даних</p>";
    }

    const actions = document.getElementById("detailActions");
    actions.innerHTML = b.status === "pending" ? `
        <button class="btn-confirm" style="flex:1; padding:12px;"
            onclick="updateBookingStatus(${b.bookingId}, 'confirmed'); closeBookingDetailModal()">
            ✓ Підтвердити
        </button>
        <button class="btn-decline" style="flex:1; padding:12px;"
            onclick="updateBookingStatus(${b.bookingId}, 'cancelled'); closeBookingDetailModal()">
            ✕ Скасувати
        </button>
    ` : "";

    document.getElementById("bookingDetailModal").classList.remove("hidden");
}

function closeBookingDetailModal() {
    document.getElementById("bookingDetailModal").classList.add("hidden");
}

async function updateBookingStatus(id, status) {
    const res = await fetch(`https://localhost:7041/api/bookings/${id}/status`, {
        method: "PUT",
        headers: {
            "Content-Type": "application/json",
            "Authorization": "Bearer " + token
        },
        body: JSON.stringify({ status })
    });

    if (!res.ok) {
        const err = await res.json();
        alert(err?.message || "Помилка");
        return;
    }

    loadBookings(currentStatusFilter);
}
init();