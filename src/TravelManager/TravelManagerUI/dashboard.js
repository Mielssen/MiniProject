const API_TOURS = "https://localhost:7041/api/tours";
const API_BOOKINGS = "https://localhost:7041/api/bookings";
const API_SAVED_PERSONS = "https://localhost:7041/api/savedpersons";
const token = localStorage.getItem("token");

let tourTypes = [];
let selectedTourId = null;
let currentTour = null;
let sliderIndex = 0;
let sliderItems = [];
let savedPersons = [];
let currentPersonIndex = null;

if (!token) {
    window.location.href = "index.html";
}

async function loadTourTypes() {
    const res = await fetch(API_TOURS + "/types");
    tourTypes = await res.json();
}

async function loadTours() {
    const res = await fetch(API_TOURS);
    const data = await res.json();
    renderTours(data);
}

async function loadSavedPersons() {
    const res = await fetch(API_SAVED_PERSONS, {
        headers: { "Authorization": "Bearer " + token }
    });
    savedPersons = await res.json();
}

function formatDate(dateString) {
    if (!dateString) return "";
    return new Date(dateString).toLocaleDateString("uk-UA", {
        year: "numeric", month: "2-digit", day: "2-digit"
    });
}

function renderTours(list) {
    const grid = document.getElementById("toursGrid");
    grid.innerHTML = "";

    list.forEach(t => {
        const card = document.createElement("div");
        card.className = "tour-card";

        const typeName = tourTypes.find(type => type.typeId === t.typeId)?.name || "";
        const images = (t.assets || []).filter(a => a.assetType === "image");
        const firstImage = images[0];

        const imgHtml = firstImage
            ? `<img class="tour-card-img" src="${firstImage.url}">`
            : `<div class="tour-card-img-placeholder">🌍</div>`;

        card.innerHTML = `
            ${imgHtml}
            <div class="tour-card-body">
                <div class="tour-card-type">${typeName}</div>
                <h4>${t.name}</h4>
                <p class="tour-card-desc">${t.description || ""}</p>
                <div class="tour-card-meta">
                    <span>${formatDate(t.availableFrom)} → ${formatDate(t.availableTo)}</span>
                    <span>${t.durationDays} дн.</span>
                </div>
                <div class="tour-card-price">${t.price} $ <span style="font-size:12px; color:#b2bec3;">/ особу</span></div>
                <button class="btn-view">Детальніше</button>
            </div>
        `;

        card.querySelector(".btn-view").onclick = () => openTourModal(t);
        grid.appendChild(card);
    });
}

function openTourModal(tour) {
    currentTour = tour;
    selectedTourId = tour.tourId;
    sliderIndex = 0;
    sliderItems = tour.assets || [];

    const typeName = tourTypes.find(type => type.typeId === tour.typeId)?.name || "";

    document.getElementById("modalType").innerText = typeName;
    document.getElementById("modalName").innerText = tour.name;
    document.getElementById("modalDesc").innerText = tour.description || "";
    document.getElementById("modalPrice").innerText = tour.price + " $ / особу";
    document.getElementById("modalDays").innerText = tour.durationDays + " дн.";
    document.getElementById("modalFrom").innerText = formatDate(tour.availableFrom);
    document.getElementById("modalTo").innerText = formatDate(tour.availableTo);
    document.getElementById("bookingTourName").innerText = tour.name;

    renderSlider();
    document.getElementById("tourModal").classList.remove("hidden");
}

function renderSlider() {
    const container = document.getElementById("sliderContainer");

    if (sliderItems.length === 0) {
        container.innerHTML = `<div class="slider-placeholder">🌍</div>`;
        return;
    }

    const slides = sliderItems.map(a => {
        if (a.assetType === "image") return `<img src="${a.url}">`;
        else return `<video src="${a.url}" controls></video>`;
    }).join("");

    const dots = sliderItems.map((_, i) =>
        `<button class="slider-dot ${i === 0 ? 'active' : ''}" onclick="goToSlide(${i})"></button>`
    ).join("");

    container.innerHTML = `
        <div class="slider">
            <div class="slider-track" id="sliderTrack">${slides}</div>
            ${sliderItems.length > 1 ? `
                <button class="slider-btn prev" onclick="prevSlide()">❮</button>
                <button class="slider-btn next" onclick="nextSlide()">❯</button>
                <div class="slider-dots" id="sliderDots">${dots}</div>
            ` : ""}
        </div>
    `;
}

function goToSlide(index) {
    sliderIndex = index;
    document.getElementById("sliderTrack").style.transform = `translateX(-${index * 100}%)`;
    document.querySelectorAll(".slider-dot").forEach((d, i) => {
        d.classList.toggle("active", i === index);
    });
}

function prevSlide() {
    goToSlide(sliderIndex > 0 ? sliderIndex - 1 : sliderItems.length - 1);
}

function nextSlide() {
    goToSlide(sliderIndex < sliderItems.length - 1 ? sliderIndex + 1 : 0);
}

function closeTourModal() {
    document.getElementById("tourModal").classList.add("hidden");
}

function calcEndDate(startDate, durationDays) {
    if (!startDate) return "";
    const end = new Date(startDate);
    end.setDate(end.getDate() + durationDays);
    return end.toISOString().split("T")[0];
}

function calcMaxStart(availableTo, durationDays) {
    if (!availableTo) return "";
    const max = new Date(availableTo);
    max.setDate(max.getDate() - durationDays);
    return max.toISOString().split("T")[0];
}

function onStartDateChange() {
    const start = document.getElementById("bookingStartDate").value;
    document.getElementById("bookingEndDate").value = calcEndDate(start, currentTour.durationDays);
}

function onPeopleChange() {
    const adults = +document.getElementById("bookingAdults").value || 0;
    const children = +document.getElementById("bookingChildren").value || 0;
    const total = (adults * currentTour.price) + (children * currentTour.price * 0.5);
    document.getElementById("bookingTotal").value = total.toFixed(2) + " $";
}

function openBookingModal() {
    const tour = currentTour;

    document.getElementById("bookingStartDate").value = tour.availableFrom?.split("T")[0];
    document.getElementById("bookingEndDate").value = calcEndDate(tour.availableFrom?.split("T")[0], tour.durationDays);
    document.getElementById("bookingStartDate").min = tour.availableFrom?.split("T")[0];
    document.getElementById("bookingStartDate").max = calcMaxStart(tour.availableTo?.split("T")[0], tour.durationDays);
    document.getElementById("bookingAdults").value = 1;
    document.getElementById("bookingChildren").value = 0;
    document.getElementById("bookingTotal").value = tour.price + " $";
    document.getElementById("bookingComment").value = "";

    document.getElementById("step1").classList.remove("hidden");
    document.getElementById("step2").classList.add("hidden");
    document.getElementById("bookingModal").classList.remove("hidden");
}

function closeBookingModal() {
    document.querySelector(".booking-modal-content").style.maxWidth = "380px";
    document.getElementById("bookingModal").classList.add("hidden");
}

function goToStep1() {
    document.querySelector(".booking-modal-content").style.maxWidth = "380px";
    document.getElementById("step1").classList.remove("hidden");
    document.getElementById("step2").classList.add("hidden");
}

async function goToStep2() {
    const adults = +document.getElementById("bookingAdults").value || 0;
    const children = +document.getElementById("bookingChildren").value || 0;

    if (adults < 1) {
        alert("Мінімум 1 дорослий");
        return;
    }

    document.getElementById("step2TourName").innerText = currentTour.name;
    document.getElementById("step2StartDate").innerText = document.getElementById("bookingStartDate").value;
    document.getElementById("step2EndDate").innerText = document.getElementById("bookingEndDate").value;
    document.getElementById("step2Total").innerText = document.getElementById("bookingTotal").value;
    document.getElementById("step2Comment").innerText = document.getElementById("bookingComment").value || "—";

    await loadSavedPersons();
    renderPersonsForms(adults, children);

    document.getElementById("step1").classList.add("hidden");
    document.getElementById("step2").classList.remove("hidden");
    document.querySelector(".booking-modal-content").style.maxWidth = "700px";
}

function renderPersonsForms(adults, children) {
    const container = document.getElementById("personsForms");
    container.innerHTML = "";

    const total = adults + children;

    for (let i = 0; i < total; i++) {
        const isChild = i >= adults;
        const isFirst = i === 0;
        const label = isChild ? `Дитина ${i - adults + 1}` : `Дорослий ${i + 1}`;

        const form = document.createElement("div");
        form.className = "person-form";
        form.dataset.index = i;
        form.dataset.isChild = isChild;

        form.innerHTML = `
            <div class="person-form-header">
                <span class="person-label">${label}</span>
                ${!isFirst ? `<button class="btn-preset" onclick="openPresetsModal(${i})">📋 Пресети</button>` : ""}
            </div>
            <input placeholder="Ім'я" class="person-name" ${isFirst ? "readonly" : ""}>
            <input placeholder="Паспортні дані" class="person-passport" ${isFirst ? "readonly" : ""}>
            <input type="date" class="person-birth" ${isFirst ? "readonly" : ""}>
            ${!isFirst ? `
                <label style="display:flex; align-items:center; gap:8px; font-size:13px; margin-top:5px;">
                    <input type="checkbox" class="person-save"> Зберегти в пресети
                </label>
            ` : ""}
        `;

        container.appendChild(form);
    }

    prefillFirstPerson();
}

async function prefillFirstPerson() {
    const res = await fetch("https://localhost:7041/api/auth/profile", {
        headers: { "Authorization": "Bearer " + token }
    });
    const profile = await res.json();

    const forms = document.querySelectorAll(".person-form");
    if (forms.length > 0) {
        forms[0].querySelector(".person-name").value = profile.name || "";
        forms[0].querySelector(".person-passport").value = profile.passportData || "";
        forms[0].querySelector(".person-birth").value = profile.dateOfBirth?.split("T")[0] || "";
    }
}

function openPresetsModal(personIndex) {
    currentPersonIndex = personIndex;

    const list = document.getElementById("presetsList");
    list.innerHTML = "";

    if (savedPersons.length === 0) {
        list.innerHTML = "<p style='color:#636e72;'>Пресетів немає</p>";
    } else {
        savedPersons.forEach(p => {
            const item = document.createElement("div");
            item.className = "preset-item";
            item.innerHTML = `
                <div class="preset-info">
                    <strong>${p.name}</strong>
                    <span>${p.isChild ? "Дитина" : "Дорослий"} · ${p.passportData || "—"}</span>
                </div>
                <div style="display:flex; gap:8px;">
                    <button class="btn-confirm" onclick="applyPreset(${p.personId})">Обрати</button>
                    <button class="btn-decline" onclick="deletePreset(${p.personId})">✕</button>
                </div>
            `;
            list.appendChild(item);
        });
    }

    document.getElementById("presetsModal").classList.remove("hidden");
}

function closePresetsModal() {
    document.getElementById("presetsModal").classList.add("hidden");
    currentPersonIndex = null;
}

function applyPreset(personId) {
    const person = savedPersons.find(p => p.personId === personId);
    if (!person || currentPersonIndex === null) return;

    const forms = document.querySelectorAll(".person-form");
    const form = forms[currentPersonIndex];

    form.querySelector(".person-name").value = person.name;
    form.querySelector(".person-passport").value = person.passportData || "";
    form.querySelector(".person-birth").value = person.dateOfBirth || "";

    closePresetsModal();
}

async function deletePreset(personId) {
    await fetch(`${API_SAVED_PERSONS}/${personId}`, {
        method: "DELETE",
        headers: { "Authorization": "Bearer " + token }
    });

    await loadSavedPersons();
    openPresetsModal(currentPersonIndex);
}

async function confirmBooking() {
    const startDate = document.getElementById("bookingStartDate").value;
    const numberOfAdults = +document.getElementById("bookingAdults").value;
    const numberOfChildren = +document.getElementById("bookingChildren").value;
    const comment = document.getElementById("bookingComment").value;

    const forms = document.querySelectorAll(".person-form");
    const persons = [];

    for (const form of forms) {
        const name = form.querySelector(".person-name").value.trim();
        const passport = form.querySelector(".person-passport").value.trim();
        const birth = form.querySelector(".person-birth").value;
        const isChild = form.dataset.isChild === "true";
        const saveCheckbox = form.querySelector(".person-save");
        const label = form.querySelector(".person-label").innerText;

        if (!name) {
            alert(`${label}: ім'я обов'язкове`);
            return;
        }

        if (!/^[a-zA-Zа-яА-ЯіІїЇєЄ\s]+$/.test(name)) {
            alert(`${label}: ім'я має містити тільки літери`);
            return;
        }

        if (!passport) {
            alert(`${label}: паспортні дані обов'язкові`);
            return;
        }

        if (!/^\d+$/.test(passport)) {
            alert(`${label}: паспортні дані мають містити тільки цифри`);
            return;
        }

        if (!birth) {
            alert(`${label}: дата народження обов'язкова`);
            return;
        }

        const birthDate = new Date(birth);
        if (isNaN(birthDate.getTime())) {
            alert(`${label}: невірний формат дати`);
            return;
        }

        if (birthDate > new Date()) {
            alert(`${label}: дата народження не може бути в майбутньому`);
            return;
        }

        persons.push({ name, passportData: passport, dateOfBirth: birth, isChild });

        if (saveCheckbox?.checked) {
            await fetch(API_SAVED_PERSONS, {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    "Authorization": "Bearer " + token
                },
                body: JSON.stringify({ name, passportData: passport, dateOfBirth: birth, isChild })
            });
        }
    }

    const res = await fetch(API_BOOKINGS, {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
            "Authorization": "Bearer " + token
        },
        body: JSON.stringify({
            tourId: selectedTourId,
            startDate,
            numberOfAdults,
            numberOfChildren,
            comment,
            persons
        })
    });

    if (!res.ok) {
        const err = await res.json();
        alert(err?.message || "Помилка");
        return;
    }

    closeBookingModal();
    closeTourModal();
    loadBookings();
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
        grid.innerHTML = "<p style='color:#636e72;'>Бронювань немає</p>";
        return;
    }

    list.forEach(b => {
        const card = document.createElement("div");
        card.className = "card";

        const statusClass = `status-${b.status.toLowerCase()}`;

        card.innerHTML = `
            <div class="card-header">
                <h4>${b.tourName}</h4>
                <span class="status-badge ${statusClass}">${b.status}</span>
            </div>
            <div class="meta">
                <span>Початок: ${b.startDate}</span>
                <span>${b.numberOfPeople} ос.</span>
            </div>
            <div class="meta">
                <span>Сума: ${b.totalPrice} $</span>
                <span>Заброньовано: ${formatDate(b.bookingDate)}</span>
            </div>
            <div class="actions">
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

function searchTours() {
    const value = document.getElementById("search").value.trim();
    if (!value) return;

    fetch(`${API_TOURS}?search=${encodeURIComponent(value)}`)
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

function logout() {
    localStorage.removeItem("token");
    window.location.href = "index.html";
}

async function init() {
    await loadTourTypes();
    await loadTours();
    await loadBookings();
}

init();