const API_TOURS = "/api/tours";
const API_BOOKINGS = "/api/bookings";

let editId = null;
let assets = [];
let tourTypes = [];
let currentStatusFilter = null;
let searchTimer = null;

function ensureManagerAccess() {
    if (!TravelApi.isAuthenticated()) {
        window.location.replace("index.html");
        return false;
    }

    const role = TravelApi.getRole();

    if (role !== "manager" && role !== "admin") {
        window.location.replace("dashboard.html");
        return false;
    }

    return true;
}

function getErrorMessage(error) {
    return error?.message || "Не вдалося виконати операцію.";
}

function formatDate(value) {
    if (!value) return "—";

    const date = new Date(value);
    if (Number.isNaN(date.getTime())) return "—";

    return date.toLocaleDateString("uk-UA");
}

async function loadTourTypes() {
    const result = await TravelApi.get(`${API_TOURS}/types`);
    tourTypes = Array.isArray(result) ? result : [];

    const select = document.getElementById("typeId");

    select.innerHTML = "";

    tourTypes.forEach(type => {
        const option = document.createElement("option");
        option.value = type.typeId ?? type.TypeId;
        option.textContent = type.name ?? type.Name;
        select.appendChild(option);
    });
}

function renderPreview() {
    const preview = document.getElementById("preview");
    preview.innerHTML = "";

    assets.forEach((asset, index) => {
        const wrapper = document.createElement("div");
        wrapper.style.cssText =
            "position:relative;display:inline-block;margin:4px;";

        if (asset.assetType === "image") {
            const image = document.createElement("img");
            image.src = asset.url;
            image.className = "preview-item";
            image.alt = "Попередній перегляд";
            wrapper.appendChild(image);
        } else if (asset.assetType === "video") {
            const video = document.createElement("video");
            video.src = asset.url;
            video.controls = true;
            video.style.cssText =
                "width:100px;height:60px;object-fit:cover;border-radius:10px;";
            wrapper.appendChild(video);
        }

        const removeButton = document.createElement("button");
        removeButton.type = "button";
        removeButton.textContent = "✕";
        removeButton.style.cssText =
            "position:absolute;top:2px;right:2px;background:red;color:white;border:0;border-radius:50%;width:20px;height:20px;cursor:pointer;";
        removeButton.onclick = () => {
            const removed = assets.splice(index, 1)[0];

            if (removed?._file && removed.url) {
                URL.revokeObjectURL(removed.url);
            }

            renderPreview();
        };

        wrapper.appendChild(removeButton);
        preview.appendChild(wrapper);
    });
}

document.getElementById("files")?.addEventListener("change", event => {
    const files = Array.from(event.target.files || []);

    files.forEach(file => {
        if (
            !file.type.startsWith("image/") &&
            !file.type.startsWith("video/")
        ) {
            return;
        }

        assets.push({
            assetType: file.type.startsWith("video") ? "video" : "image",
            url: URL.createObjectURL(file),
            _file: file
        });
    });

    renderPreview();
    event.target.value = "";
});

async function loadTours(search = "") {
    const grid = document.getElementById("toursGrid");
    grid.innerHTML = "<p>Завантаження турів...</p>";

    try {
        const params = new URLSearchParams();

        if (search) {
            params.set("search", search);
        }

        const path = params.size
            ? `${API_TOURS}?${params.toString()}`
            : API_TOURS;

        const result = await TravelApi.get(path);
        renderTours(Array.isArray(result) ? result : []);
    } catch (error) {
        grid.innerHTML = "";

        const message = document.createElement("p");
        message.textContent = getErrorMessage(error);

        const retry = document.createElement("button");
        retry.className = "btn-save";
        retry.textContent = "Повторити";
        retry.onclick = () => loadTours(search);

        grid.append(message, retry);
    }
}

function renderTours(list) {
    const grid = document.getElementById("toursGrid");
    grid.innerHTML = "";

    if (!list.length) {
        grid.innerHTML = "<p>Турів не знайдено.</p>";
        return;
    }

    list.forEach(tour => {
        const card = document.createElement("div");
        card.className = "card";

        const media = document.createElement("div");
        media.style.cssText =
            "display:flex;gap:6px;margin:10px 0;flex-wrap:wrap;";

        (tour.assets || []).forEach(asset => {
            if (asset.assetType === "image") {
                const image = document.createElement("img");
                image.src = asset.url;
                image.alt = tour.name || "Фото туру";
                image.style.cssText =
                    "width:60px;height:60px;object-fit:cover;border-radius:10px;border:1px solid #eee;";
                media.appendChild(image);
            } else if (asset.assetType === "video") {
                const video = document.createElement("video");
                video.src = asset.url;
                video.controls = true;
                video.style.cssText =
                    "width:100px;height:60px;object-fit:cover;border-radius:10px;";
                media.appendChild(video);
            }
        });

        const header = document.createElement("div");
        header.className = "card-header";

        const title = document.createElement("h4");
        title.textContent = tour.name || "Тур";

        const type = document.createElement("span");
        const tourType = tourTypes.find(item =>
            (item.typeId ?? item.TypeId) === tour.typeId
        );
        type.textContent =
            tourType?.name ?? tourType?.Name ?? tour.typeId ?? "—";

        header.append(title, type);

        const description = document.createElement("p");
        description.className = "desc";
        description.textContent = tour.description || "";

        const priceDuration = document.createElement("div");
        priceDuration.className = "meta";

        const price = document.createElement("span");
        price.textContent = `${tour.price} $`;

        const duration = document.createElement("span");
        duration.textContent = `${tour.durationDays} дн.`;

        priceDuration.append(price, duration);

        const dates = document.createElement("div");
        dates.className = "meta";
        dates.textContent =
            `${formatDate(tour.availableFrom)} → ${formatDate(tour.availableTo)}`;

        const actions = document.createElement("div");
        actions.className = "actions";

        const editButton = document.createElement("button");
        editButton.className = "btn-edit";
        editButton.textContent = "Редагувати";
        editButton.onclick = () => editTour(tour);

        const deleteButton = document.createElement("button");
        deleteButton.className = "btn-delete";
        deleteButton.textContent = "Видалити";
        deleteButton.onclick = () => deleteTour(tour.tourId);

        actions.append(editButton, deleteButton);
        card.append(header, media, description, priceDuration, dates, actions);
        grid.appendChild(card);
    });
}

function searchTours() {
    clearTimeout(searchTimer);

    const value = document.getElementById("search").value.trim();

    if (!value) {
        clearSearch();
        return;
    }

    document.getElementById("searchText").textContent = value;
    document.getElementById("searchChip").classList.remove("hidden");

    loadTours(value);
}

function clearSearch() {
    clearTimeout(searchTimer);

    document.getElementById("search").value = "";
    document.getElementById("searchChip").classList.add("hidden");

    loadTours();
}

document.getElementById("search")?.addEventListener("input", event => {
    clearTimeout(searchTimer);

    const value = event.target.value.trim();

    searchTimer = setTimeout(() => {
        if (!value) {
            document.getElementById("searchChip").classList.add("hidden");
            loadTours();
            return;
        }

        document.getElementById("searchText").textContent = value;
        document.getElementById("searchChip").classList.remove("hidden");
        loadTours(value);
    }, 400);
});

function openModal(isEdit = false) {
    assets.forEach(asset => {
        if (asset._file && asset.url) URL.revokeObjectURL(asset.url);
    });

    assets = [];
    document.getElementById("preview").innerHTML = "";

    document.getElementById("modalTitle").textContent =
        isEdit ? "Редагувати тур" : "Створити тур";

    document.getElementById("modal").classList.remove("hidden");

    if (!isEdit) {
        editId = null;

        ["name", "description", "price", "days", "from", "to"]
            .forEach(id => {
                document.getElementById(id).value = "";
            });

        if (tourTypes.length) {
            document.getElementById("typeId").value =
                tourTypes[0].typeId ?? tourTypes[0].TypeId;
        }
    }
}

function closeModal() {
    document.getElementById("modal").classList.add("hidden");
}

function editTour(tour) {
    editId = tour.tourId;

    assets = (tour.assets || []).map(asset => ({
        assetType: String(asset.assetType || "").toLowerCase(),
        url: asset.url
    }));

    document.getElementById("modalTitle").textContent = "Редагувати тур";
    document.getElementById("name").value = tour.name || "";
    document.getElementById("description").value = tour.description || "";
    document.getElementById("price").value = tour.price ?? "";
    document.getElementById("days").value = tour.durationDays ?? "";
    document.getElementById("from").value =
        tour.availableFrom?.split("T")[0] || "";
    document.getElementById("to").value =
        tour.availableTo?.split("T")[0] || "";
    document.getElementById("typeId").value = tour.typeId ?? "";

    document.getElementById("preview").innerHTML = "";
    document.getElementById("modal").classList.remove("hidden");

    renderPreview();
}

async function saveTour() {
    const button = document.querySelector("#modal .btn-save");

    const name = document.getElementById("name").value.trim();
    const description = document.getElementById("description").value.trim();
    const price = Number(document.getElementById("price").value);
    const days = Number(document.getElementById("days").value);
    const from = document.getElementById("from").value;
    const to = document.getElementById("to").value;
    const typeId = Number(document.getElementById("typeId").value);

    if (!name || !price || !days || !from || !to || !typeId) {
        alert("Заповніть усі обов'язкові поля.");
        return;
    }

    if (price <= 0 || days <= 0 || from > to) {
        alert("Перевірте ціну, тривалість і дати туру.");
        return;
    }

    const tourData = {
        name,
        description,
        price,
        durationDays: days,
        availableFrom: from,
        availableTo: to,
        typeId,
        assets: assets.filter(asset => !asset._file)
    };

    button.disabled = true;

    try {
        let currentId = editId;

        if (editId) {
            await TravelApi.put(`${API_TOURS}/${editId}`, tourData);
        } else {
            const created = await TravelApi.post(API_TOURS, tourData);
            currentId = created?.tourId ?? created?.TourId;
        }

        if (!currentId && assets.some(asset => asset._file)) {
            throw new Error("Не вдалося отримати ID створеного туру для завантаження файлів.");
        }

        for (const asset of assets) {
            if (!asset._file) continue;

            const formData = new FormData();
            formData.append("file", asset._file);

            await TravelApi.post(
                `${API_TOURS}/${currentId}/images`,
                formData
            );
        }

        closeModal();
        await loadTours();
    } catch (error) {
        alert(getErrorMessage(error));
    } finally {
        button.disabled = false;
    }
}

async function deleteTour(id) {
    if (!confirm("Видалити тур?")) return;

    try {
        await TravelApi.delete(`${API_TOURS}/${id}`);
        await loadTours();
    } catch (error) {
        alert(getErrorMessage(error));
    }
}

function logout() {
    TravelApi.logout();
}

function switchTab(name, button) {
    document.getElementById("tab-tours").classList.add("hidden");
    document.getElementById("tab-bookings").classList.add("hidden");

    document.querySelectorAll(".tab").forEach(tab => {
        tab.classList.remove("active");
    });

    document.getElementById(`tab-${name}`).classList.remove("hidden");
    button.classList.add("active");

    if (name === "bookings") {
        loadBookings();
    }
}

async function loadBookings(status = null) {
    currentStatusFilter = status;

    const container = document.getElementById("bookingsList");
    container.innerHTML = "<p>Завантаження бронювань...</p>";

    try {
        const path = status
            ? `${API_BOOKINGS}?status=${encodeURIComponent(status)}`
            : API_BOOKINGS;

        const result = await TravelApi.get(path);
        renderBookings(Array.isArray(result) ? result : []);
    } catch (error) {
        container.innerHTML = "";

        const message = document.createElement("p");
        message.textContent = getErrorMessage(error);

        const retry = document.createElement("button");
        retry.className = "btn-save";
        retry.textContent = "Повторити";
        retry.onclick = () => loadBookings(currentStatusFilter);

        container.append(message, retry);
    }
}

function filterBookings(status, button) {
    document.querySelectorAll(".filter-btn").forEach(item => {
        item.classList.remove("active");
    });

    button.classList.add("active");
    loadBookings(status);
}

function renderBookings(list) {
    const container = document.getElementById("bookingsList");
    container.innerHTML = "";

    if (!list.length) {
        container.innerHTML =
            "<p style='color:#636e72;'>Бронювань немає</p>";
        return;
    }

    list.forEach(booking => {
        const row = document.createElement("div");
        row.className = "booking-row";
        row.style.cursor = "pointer";

        const left = document.createElement("div");
        left.className = "booking-row-left";

        const title = document.createElement("h4");
        title.textContent = booking.tourName || "Тур";

        const user = document.createElement("span");
        user.textContent =
            `${booking.userName || "—"} · ${booking.userEmail || "—"}`;

        const details = document.createElement("span");
        details.style.cssText = "display:block;margin-top:3px;";
        details.textContent =
            `Початок: ${formatDate(booking.startDate)} · ${booking.numberOfPeople} ос. · ${booking.totalPrice} $`;

        left.append(title, user, details);

        const right = document.createElement("div");
        right.className = "booking-row-right";

        const status = document.createElement("span");
        status.className =
            `status-badge status-${String(booking.status || "").toLowerCase()}`;
        status.textContent = booking.status || "—";

        right.appendChild(status);

        if (booking.status === "pending") {
            const confirmButton = document.createElement("button");
            confirmButton.className = "btn-confirm";
            confirmButton.textContent = "✓";
            confirmButton.onclick = event => {
                event.stopPropagation();
                updateBookingStatus(booking.bookingId, "confirmed");
            };

            const cancelButton = document.createElement("button");
            cancelButton.className = "btn-decline";
            cancelButton.textContent = "✕";
            cancelButton.onclick = event => {
                event.stopPropagation();
                updateBookingStatus(booking.bookingId, "cancelled");
            };

            right.append(confirmButton, cancelButton);
        }

        row.append(left, right);
        row.onclick = () => openBookingDetailModal(booking);

        container.appendChild(row);
    });
}

function setDetailText(id, value) {
    document.getElementById(id).textContent = value ?? "—";
}

function openBookingDetailModal(booking) {
    setDetailText("detailTourName", booking.tourName);
    setDetailText("detailTourPrice", `${booking.tourPrice ?? "—"} $`);
    setDetailText("detailTourDays", `${booking.tourDurationDays ?? "—"} дн.`);
    setDetailText("detailUserName", booking.userName);
    setDetailText("detailUserEmail", booking.userEmail);
    setDetailText("detailUserPhone", booking.userPhone);
    setDetailText("detailUserPassport", booking.userPassportData);
    setDetailText("detailUserBirth", formatDate(booking.userDateOfBirth));
    setDetailText("detailStartDate", formatDate(booking.startDate));
    setDetailText("detailBookingDate", formatDate(booking.bookingDate));
    setDetailText("detailPeople", booking.numberOfPeople);
    setDetailText("detailTotal", `${booking.totalPrice ?? "—"} $`);
    setDetailText("detailComment", booking.comment || "—");

    const status = document.getElementById("detailStatus");
    status.textContent = booking.status || "—";
    status.className =
        `status-badge status-${String(booking.status || "").toLowerCase()}`;

    const personsContainer = document.getElementById("detailPersons");
    personsContainer.innerHTML = "";

    (booking.persons || []).forEach((person, index) => {
        const item = document.createElement("div");
        item.className = "detail-person";

        const header = document.createElement("div");
        header.className = "detail-person-header";
        header.textContent =
            `${person.isChild ? "👦 Дитина" : "👤 Дорослий"} ${index + 1}`;

        const details = document.createElement("div");
        details.className = "detail-grid";

        [
            ["Ім'я", person.name],
            ["Паспорт", person.passportData],
            ["Дата народження", formatDate(person.dateOfBirth)]
        ].forEach(([label, value]) => {
            const field = document.createElement("div");
            field.className = "detail-item";

            const caption = document.createElement("span");
            caption.textContent = label;

            const strong = document.createElement("strong");
            strong.textContent = value || "—";

            field.append(caption, strong);
            details.appendChild(field);
        });

        item.append(header, details);
        personsContainer.appendChild(item);
    });

    if (!booking.persons?.length) {
        personsContainer.textContent = "Немає даних про учасників.";
    }

    const actions = document.getElementById("detailActions");
    actions.innerHTML = "";

    if (booking.status === "pending") {
        const confirmButton = document.createElement("button");
        confirmButton.className = "btn-confirm";
        confirmButton.textContent = "✓ Підтвердити";
        confirmButton.onclick = async () => {
            await updateBookingStatus(booking.bookingId, "confirmed");
            closeBookingDetailModal();
        };

        const cancelButton = document.createElement("button");
        cancelButton.className = "btn-decline";
        cancelButton.textContent = "✕ Скасувати";
        cancelButton.onclick = async () => {
            await updateBookingStatus(booking.bookingId, "cancelled");
            closeBookingDetailModal();
        };

        actions.append(confirmButton, cancelButton);
    }

    document.getElementById("bookingDetailModal").classList.remove("hidden");
}

function closeBookingDetailModal() {
    document.getElementById("bookingDetailModal").classList.add("hidden");
}

async function updateBookingStatus(id, status) {
    try {
        await TravelApi.put(`${API_BOOKINGS}/${id}/status`, { status });
        await loadBookings(currentStatusFilter);
    } catch (error) {
        alert(getErrorMessage(error));
    }
}

async function init() {
    if (!ensureManagerAccess()) return;

    try {
        await loadTourTypes();
        await loadTours();
    } catch (error) {
        document.getElementById("toursGrid").textContent =
            getErrorMessage(error);
    }
}

document.addEventListener("DOMContentLoaded", init);
