
const API_CATALOG = "/api/v1/catalog/items";
const API_BOOKINGS = "/api/bookings";
const API_SAVED_PERSONS = "/api/savedpersons";

const PAGE_SIZE = 10;
const SEARCH_DELAY = 400;

let tourTypes = [];
let tours = [];
let bookings = [];
let selectedTourId = null;
let currentTour = null;
let sliderIndex = 0;
let sliderItems = [];
let savedPersons = [];
let currentPersonIndex = null;

let currentPage = 1;
let totalPages = 1;
let totalCount = 0;
let currentSearch = "";
let searchTimer = null;
let isBookingSubmitting = false;
let pendingBookingAttempt = null;

let catalogAvailable = true;
let bookingsAvailable = true;

if (!TravelApi.isAuthenticated()) {
    window.location.href = "index.html";
}

function showMessage(message, type = "error") {
    let element = document.getElementById("dashboardMessage");

    if (!element) {
        element = document.createElement("div");
        element.id = "dashboardMessage";
        element.style.cssText = `
            padding: 12px 16px;
            margin: 12px 0;
            border-radius: 10px;
            font-size: 14px;
            display: none;
        `;

        document.querySelector(".container").prepend(element);
    }

    element.textContent = message;
    element.style.display = "block";
    element.style.background = type === "error" ? "#ffecec" : "#e8f8f1";
    element.style.color = type === "error" ? "#c0392b" : "#16845b";
}

function hideMessage() {
    const element = document.getElementById("dashboardMessage");

    if (element) {
        element.style.display = "none";
    }
}

function showGridState(gridId, message, retryAction = null) {
    const grid = document.getElementById(gridId);
    grid.innerHTML = "";

    const container = document.createElement("div");
    container.style.cssText = `
        padding: 25px;
        text-align: center;
        color: #636e72;
        grid-column: 1 / -1;
    `;

    const text = document.createElement("p");
    text.textContent = message;
    container.appendChild(text);

    if (retryAction) {
        const button = document.createElement("button");
        button.textContent = "Спробувати ще раз";
        button.style.cssText = `
            margin-top: 10px;
            padding: 9px 18px;
            border: none;
            border-radius: 8px;
            cursor: pointer;
        `;
        button.onclick = retryAction;
        container.appendChild(button);
    }

    grid.appendChild(container);
}

function formatDate(dateString) {
    if (!dateString) return "";

    const date = new Date(dateString);

    if (Number.isNaN(date.getTime())) return "";

    return date.toLocaleDateString("uk-UA", {
        year: "numeric",
        month: "2-digit",
        day: "2-digit"
    });
}

function dateInputValue(dateString) {
    if (!dateString) return "";

    return String(dateString).split("T")[0];
}

function getTourId(tour) {
    return tour?.id ?? tour?.tourId ?? tour?.Id ?? null;
}

function getTourTypeName(tour) {
    return tour?.typeName
        || tourTypes.find(type =>
            (type.typeId ?? type.id) === tour?.typeId
        )?.name
        || "";
}

function getTourAssets(tour) {
    return tour?.assets || tour?.Assets || [];
}

function getTourName(tour) {
    return tour?.name || tour?.Name || "Тур";
}

function getTourPrice(tour) {
    return tour?.price ?? tour?.Price ?? 0;
}

function getTourDuration(tour) {
    return tour?.durationDays ?? tour?.DurationDays ?? 0;
}

function getTourAvailableFrom(tour) {
    return tour?.availableFrom ?? tour?.AvailableFrom ?? "";
}

function getTourAvailableTo(tour) {
    return tour?.availableTo ?? tour?.AvailableTo ?? "";
}

function getTourDescription(tour) {
    return tour?.description ?? tour?.Description ?? "";
}

function getErrorMessage(error) {
    if (error instanceof ApiError) {
        return error.message;
    }

    return error?.message || "Сталася непередбачена помилка.";
}

function createPagination() {
    let pagination = document.getElementById("toursPagination");

    if (!pagination) {
        pagination = document.createElement("div");
        pagination.id = "toursPagination";
        pagination.style.cssText = `
            display: flex;
            justify-content: center;
            align-items: center;
            flex-wrap: wrap;
            gap: 8px;
            margin: 25px 0;
        `;

        document.getElementById("toursGrid").after(pagination);
    }

    return pagination;
}

function renderPagination() {
    const pagination = createPagination();
    pagination.innerHTML = "";

    if (totalPages <= 1) return;

    function addButton(label, page, disabled = false, active = false) {
        const button = document.createElement("button");
        button.textContent = label;
        button.disabled = disabled;

        button.style.cssText = `
            min-width: 36px;
            padding: 8px 12px;
            border: 1px solid #dfe6e9;
            border-radius: 8px;
            background: ${active ? "#00b894" : "#ffffff"};
            color: ${active ? "#ffffff" : "#2d3436"};
            cursor: ${disabled ? "not-allowed" : "pointer"};
            opacity: ${disabled ? "0.5" : "1"};
        `;

        button.onclick = () => loadTours(page);
        pagination.appendChild(button);
    }

    addButton("←", currentPage - 1, currentPage <= 1);

    const start = Math.max(1, currentPage - 2);
    const end = Math.min(totalPages, currentPage + 2);

    if (start > 1) {
        addButton("1", 1, false, currentPage === 1);

        if (start > 2) {
            const dots = document.createElement("span");
            dots.textContent = "…";
            pagination.appendChild(dots);
        }
    }

    for (let page = start; page <= end; page++) {
        addButton(
            String(page),
            page,
            false,
            page === currentPage
        );
    }

    if (end < totalPages) {
        if (end < totalPages - 1) {
            const dots = document.createElement("span");
            dots.textContent = "…";
            pagination.appendChild(dots);
        }

        addButton(
            String(totalPages),
            totalPages,
            false,
            currentPage === totalPages
        );
    }

    addButton("→", currentPage + 1, currentPage >= totalPages);

    const info = document.createElement("span");
    info.textContent = `Сторінка ${currentPage} з ${totalPages} · Усього турів: ${totalCount}`;
    info.style.cssText = `
        width: 100%;
        text-align: center;
        color: #636e72;
        font-size: 13px;
        margin-top: 5px;
    `;

    pagination.appendChild(info);
}

function updateSearchChip() {
    const chip = document.getElementById("searchChip");
    const text = document.getElementById("searchText");

    if (!chip || !text) return;

    if (currentSearch) {
        text.textContent = currentSearch;
        chip.classList.remove("hidden");
    } else {
        chip.classList.add("hidden");
    }
}

async function loadDashboard() {
    hideMessage();

    showGridState("toursGrid", "Завантаження турів...");
    showGridState("bookingsGrid", "Завантаження бронювань...");

    try {
        const data = await TravelApi.get("/api/dashboard");

        const catalog = data?.catalog;
        const userBookings = data?.bookings;

        if (catalog) {
            catalogAvailable = true;
            applyCatalogResult(catalog);
        } else {
            catalogAvailable = false;
            showGridState(
                "toursGrid",
                data?.errors?.catalog || "Каталог тимчасово недоступний.",
                loadDashboard
            );
        }

        if (Array.isArray(userBookings)) {
            bookingsAvailable = true;
            bookings = userBookings;
            renderBookings(bookings);
        } else {
            bookingsAvailable = false;
            showGridState(
                "bookingsGrid",
                data?.errors?.bookings || "Бронювання тимчасово недоступні.",
                loadBookings
            );
        }

        if (data?.partial) {
            const errors = [];

            if (data.errors?.catalog) errors.push("каталог");
            if (data.errors?.bookings) errors.push("бронювання");

            showMessage(
                `Частина даних тимчасово недоступна: ${errors.join(", ")}. Інші дані завантажено.`,
                "error"
            );
        }
    } catch (error) {
        if (error.status === 429) {
            showMessage(error.message);
        } else {
            showMessage(getErrorMessage(error));
        }

        showGridState("toursGrid", "Не вдалося завантажити каталог.", loadDashboard);
        showGridState("bookingsGrid", "Не вдалося завантажити бронювання.", loadDashboard);
    }
}

function applyCatalogResult(result) {
    const items = Array.isArray(result)
        ? result
        : result?.items || result?.Items || [];

    tours = items;

    totalCount = result?.totalCount ?? result?.TotalCount ?? items.length;
    totalPages = result?.totalPages ?? result?.TotalPages ?? 1;
    currentPage = result?.page ?? result?.Page ?? currentPage;

    renderTours(tours);
    renderPagination();
}

async function loadTours(page = 1) {
    currentPage = page;

    const grid = document.getElementById("toursGrid");
    showGridState("toursGrid", "Завантаження турів...");

    const params = new URLSearchParams({
        page: String(page),
        pageSize: String(PAGE_SIZE)
    });

    if (currentSearch) {
        params.set("search", currentSearch);
    }

    try {
        const result = await TravelApi.get(
            `${API_CATALOG}?${params.toString()}`
        );

        catalogAvailable = true;
        applyCatalogResult(result);
        hideMessage();
    } catch (error) {
        catalogAvailable = false;

        showGridState(
            "toursGrid",
            getErrorMessage(error),
            () => loadTours(currentPage)
        );

        if (error.status === 429) {
            showMessage(error.message);
        }
    }
}

function renderTours(list) {
    const grid = document.getElementById("toursGrid");
    grid.innerHTML = "";

    if (!list || list.length === 0) {
        showGridState(
            "toursGrid",
            currentSearch
                ? "За вашим запитом турів не знайдено."
                : "Доступних турів поки немає."
        );

        return;
    }

    list.forEach(tour => {
        const card = document.createElement("div");
        card.className = "tour-card";

        const assets = getTourAssets(tour);
        const firstImage = assets.find(asset =>
            (asset.assetType || asset.AssetType) === "image"
        );

        const image = document.createElement("img");

        if (firstImage) {
            image.className = "tour-card-img";
            image.src = firstImage.url || firstImage.Url;
            image.alt = getTourName(tour);
        } else {
            const placeholder = document.createElement("div");
            placeholder.className = "tour-card-img-placeholder";
            placeholder.textContent = "🌍";
            card.appendChild(placeholder);
        }

        if (firstImage) {
            card.appendChild(image);
        }

        const body = document.createElement("div");
        body.className = "tour-card-body";

        const type = document.createElement("div");
        type.className = "tour-card-type";
        type.textContent = getTourTypeName(tour);

        const title = document.createElement("h4");
        title.textContent = getTourName(tour);

        const description = document.createElement("p");
        description.className = "tour-card-desc";
        description.textContent = getTourDescription(tour);

        const meta = document.createElement("div");
        meta.className = "tour-card-meta";

        const dates = document.createElement("span");
        dates.textContent =
            `${formatDate(getTourAvailableFrom(tour))} → ${formatDate(getTourAvailableTo(tour))}`;

        const duration = document.createElement("span");
        duration.textContent = `${getTourDuration(tour)} дн.`;

        meta.append(dates, duration);

        const price = document.createElement("div");
        price.className = "tour-card-price";
        price.textContent = `${getTourPrice(tour)} $ `;

        const priceNote = document.createElement("span");
        priceNote.style.cssText = "font-size:12px; color:#b2bec3;";
        priceNote.textContent = "/ особу";
        price.appendChild(priceNote);

        const button = document.createElement("button");
        button.className = "btn-view";
        button.textContent = "Детальніше";
        button.onclick = () => openTourModal(tour);

        body.append(type, title, description, meta, price, button);
        card.appendChild(body);
        grid.appendChild(card);
    });
}

async function openTourModal(tour) {
    selectedTourId = getTourId(tour);
    currentTour = tour;

    document.getElementById("tourModal").classList.remove("hidden");
    document.getElementById("modalName").textContent = "Завантаження...";
    document.getElementById("modalDesc").textContent = "";
    document.getElementById("sliderContainer").innerHTML =
        '<div class="slider-placeholder">Завантаження...</div>';

    try {
        const details = await TravelApi.get(`${API_CATALOG}/${selectedTourId}`);

        currentTour = details;
        selectedTourId = getTourId(details) ?? selectedTourId;

        sliderIndex = 0;
        sliderItems = getTourAssets(details);

        document.getElementById("modalType").textContent =
            getTourTypeName(details);

        document.getElementById("modalName").textContent =
            getTourName(details);

        document.getElementById("modalDesc").textContent =
            getTourDescription(details);

        document.getElementById("modalPrice").textContent =
            `${getTourPrice(details)} $ / особу`;

        document.getElementById("modalDays").textContent =
            `${getTourDuration(details)} дн.`;

        document.getElementById("modalFrom").textContent =
            formatDate(getTourAvailableFrom(details));

        document.getElementById("modalTo").textContent =
            formatDate(getTourAvailableTo(details));

        document.getElementById("bookingTourName").textContent =
            getTourName(details);

        renderSlider();
    } catch (error) {
        document.getElementById("modalName").textContent =
            "Не вдалося завантажити деталі туру";

        document.getElementById("modalDesc").textContent =
            getErrorMessage(error);

        document.getElementById("sliderContainer").innerHTML =
            '<div class="slider-placeholder">🌍</div>';
    }
}

function renderSlider() {
    const container = document.getElementById("sliderContainer");

    if (!sliderItems.length) {
        container.innerHTML = '<div class="slider-placeholder">🌍</div>';
        return;
    }

    const slides = sliderItems.map(asset => {
        const url = asset.url || asset.Url;
        const type = asset.assetType || asset.AssetType;

        if (type === "image") {
            return `<img src="${url}" alt="Зображення туру">`;
        }

        return `<video src="${url}" controls></video>`;
    }).join("");

    const dots = sliderItems.map((_, index) =>
        `<button class="slider-dot ${index === 0 ? "active" : ""}" onclick="goToSlide(${index})"></button>`
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

    const track = document.getElementById("sliderTrack");

    if (track) {
        track.style.transform = `translateX(-${index * 100}%)`;
    }

    document.querySelectorAll(".slider-dot").forEach((dot, i) => {
        dot.classList.toggle("active", i === index);
    });
}

function prevSlide() {
    goToSlide(
        sliderIndex > 0
            ? sliderIndex - 1
            : sliderItems.length - 1
    );
}

function nextSlide() {
    goToSlide(
        sliderIndex < sliderItems.length - 1
            ? sliderIndex + 1
            : 0
    );
}

function closeTourModal() {
    document.getElementById("tourModal").classList.add("hidden");
}

function calcEndDate(startDate, durationDays) {
    if (!startDate) return "";

    const date = new Date(`${startDate}T00:00:00`);
    date.setDate(date.getDate() + Number(durationDays));

    return dateInputValue(date.toISOString());
}

function calcMaxStart(availableTo, durationDays) {
    if (!availableTo) return "";

    const date = new Date(`${availableTo}T00:00:00`);
    date.setDate(date.getDate() - Number(durationDays));

    return dateInputValue(date.toISOString());
}

function onStartDateChange() {
    if (!currentTour) return;

    const start = document.getElementById("bookingStartDate").value;

    document.getElementById("bookingEndDate").value =
        calcEndDate(start, getTourDuration(currentTour));
}

function onPeopleChange() {
    const adults = Number(document.getElementById("bookingAdults").value) || 0;
    const children = Number(document.getElementById("bookingChildren").value) || 0;

    const total = adults + children;

    if (total > 20) {
        document.getElementById("bookingTotal").value =
            "Максимум 20 учасників";
        return;
    }

    document.getElementById("bookingTotal").value =
        "Суму розрахує сервер";
}

function openBookingModal() {
    if (!currentTour) return;

    pendingBookingAttempt = null;

    const tour = currentTour;
    const from = dateInputValue(getTourAvailableFrom(tour));
    const to = dateInputValue(getTourAvailableTo(tour));

    document.getElementById("bookingStartDate").value = from;

    document.getElementById("bookingEndDate").value =
        calcEndDate(from, getTourDuration(tour));

    document.getElementById("bookingStartDate").min = from;

    document.getElementById("bookingStartDate").max =
        calcMaxStart(to, getTourDuration(tour));

    document.getElementById("bookingAdults").value = 1;
    document.getElementById("bookingChildren").value = 0;
    document.getElementById("bookingTotal").value =
        "Суму розрахує сервер";

    document.getElementById("bookingComment").value = "";

    document.getElementById("step1").classList.remove("hidden");
    document.getElementById("step2").classList.add("hidden");

    document.getElementById("bookingModal").classList.remove("hidden");

    onPeopleChange();
}

function closeBookingModal() {
    document.querySelector(".booking-modal-content").style.maxWidth = "380px";
    document.getElementById("bookingModal").classList.add("hidden");
    pendingBookingAttempt = null;
}

function goToStep1() {
    document.querySelector(".booking-modal-content").style.maxWidth = "380px";
    document.getElementById("step1").classList.remove("hidden");
    document.getElementById("step2").classList.add("hidden");
}

async function goToStep2() {
    const adults = Number(document.getElementById("bookingAdults").value);
    const children = Number(document.getElementById("bookingChildren").value);
    const startDate = document.getElementById("bookingStartDate").value;

    if (!startDate) {
        alert("Оберіть дату початку туру.");
        return;
    }

    if (adults < 1 || adults + children > 20 || children < 0) {
        alert("Перевірте кількість дорослих і дітей. Допустимо від 1 до 20 учасників.");
        return;
    }

    const maxStart = calcMaxStart(
        getTourAvailableTo(currentTour),
        getTourDuration(currentTour)
    );

    if (
        startDate < dateInputValue(getTourAvailableFrom(currentTour)) ||
        startDate > maxStart
    ) {
        alert("Обрана дата не входить у доступний період туру.");
        return;
    }

    document.getElementById("step2TourName").textContent =
        getTourName(currentTour);

    document.getElementById("step2StartDate").textContent = startDate;

    document.getElementById("step2EndDate").textContent =
        calcEndDate(startDate, getTourDuration(currentTour));

    document.getElementById("step2Total").textContent =
        "Суму розрахує сервер";

    document.getElementById("step2Comment").textContent =
        document.getElementById("bookingComment").value || "—";

    try {
        await loadSavedPersons();
    } catch (error) {
        savedPersons = [];
        showMessage(
            "Не вдалося завантажити збережених учасників. Можна ввести дані вручну.",
            "error"
        );
    }

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

        const label = isChild
            ? `Дитина ${i - adults + 1}`
            : `Дорослий ${i + 1}`;

        const form = document.createElement("div");
        form.className = "person-form";
        form.dataset.index = i;
        form.dataset.isChild = String(isChild);

        form.innerHTML = `
            <div class="person-form-header">
                <span class="person-label">${label}</span>
                ${!isFirst
                    ? `<button class="btn-preset" type="button" onclick="openPresetsModal(${i})">📋 Пресети</button>`
                    : ""}
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
    const forms = document.querySelectorAll(".person-form");

    if (!forms.length) return;

    try {
        const profile = await TravelApi.get("/api/auth/profile");

        forms[0].querySelector(".person-name").value =
            profile.name || "";

        forms[0].querySelector(".person-passport").value =
            profile.passportData || "";

        forms[0].querySelector(".person-birth").value =
            dateInputValue(profile.dateOfBirth);
    } catch (error) {
        showMessage(
            "Не вдалося завантажити ваші дані профілю. Перевірте їх перед бронюванням."
        );

        forms[0].querySelector(".person-name").readOnly = false;
        forms[0].querySelector(".person-passport").readOnly = false;
        forms[0].querySelector(".person-birth").readOnly = false;
    }
}

async function loadSavedPersons() {
    const result = await TravelApi.get(API_SAVED_PERSONS);
    savedPersons = Array.isArray(result) ? result : [];
}

function openPresetsModal(personIndex) {
    currentPersonIndex = personIndex;

    const list = document.getElementById("presetsList");
    list.innerHTML = "";

    if (!savedPersons.length) {
        list.innerHTML = "<p style='color:#636e72;'>Пресетів немає</p>";
    } else {
        savedPersons.forEach(person => {
            const item = document.createElement("div");
            item.className = "preset-item";

            const info = document.createElement("div");
            info.className = "preset-info";

            const name = document.createElement("strong");
            name.textContent = person.name;

            const details = document.createElement("span");
            details.textContent =
                `${person.isChild ? "Дитина" : "Дорослий"} · ${person.passportData || "—"}`;

            info.append(name, details);

            const actions = document.createElement("div");
            actions.style.display = "flex";
            actions.style.gap = "8px";

            const selectButton = document.createElement("button");
            selectButton.className = "btn-confirm";
            selectButton.textContent = "Обрати";
            selectButton.onclick = () => applyPreset(
                person.personId ?? person.id
            );

            const deleteButton = document.createElement("button");
            deleteButton.className = "btn-decline";
            deleteButton.textContent = "✕";
            deleteButton.onclick = () => deletePreset(
                person.personId ?? person.id
            );

            actions.append(selectButton, deleteButton);
            item.append(info, actions);
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
    const person = savedPersons.find(item =>
        (item.personId ?? item.id) === personId
    );

    if (!person || currentPersonIndex === null) return;

    const forms = document.querySelectorAll(".person-form");
    const form = forms[currentPersonIndex];

    form.querySelector(".person-name").value = person.name || "";
    form.querySelector(".person-passport").value = person.passportData || "";
    form.querySelector(".person-birth").value =
        dateInputValue(person.dateOfBirth);

    closePresetsModal();
}

async function deletePreset(personId) {
    try {
        await TravelApi.delete(`${API_SAVED_PERSONS}/${personId}`);
        await loadSavedPersons();
        openPresetsModal(currentPersonIndex);
    } catch (error) {
        showMessage(getErrorMessage(error));
    }
}

function getBookingPayload() {
    const startDate = document.getElementById("bookingStartDate").value;
    const numberOfAdults = Number(document.getElementById("bookingAdults").value);
    const numberOfChildren = Number(document.getElementById("bookingChildren").value);
    const comment = document.getElementById("bookingComment").value.trim();

    const forms = document.querySelectorAll(".person-form");
    const persons = [];

    for (const form of forms) {
        const name = form.querySelector(".person-name").value.trim();
        const passportData = form.querySelector(".person-passport").value.trim();
        const dateOfBirth = form.querySelector(".person-birth").value;
        const isChild = form.dataset.isChild === "true";

        const label = form.querySelector(".person-label").textContent;

        if (!name) {
            throw new Error(`${label}: ім'я обов'язкове.`);
        }

        if (!/^[a-zA-Zа-яА-ЯіІїЇєЄґҐ\s'-]+$/.test(name)) {
            throw new Error(`${label}: ім'я має містити тільки літери.`);
        }

        if (!passportData) {
            throw new Error(`${label}: паспортні дані обов'язкові.`);
        }

        if (!/^\d+$/.test(passportData)) {
            throw new Error(`${label}: паспортні дані мають містити тільки цифри.`);
        }

        if (!dateOfBirth) {
            throw new Error(`${label}: дата народження обов'язкова.`);
        }

        const birthDate = new Date(`${dateOfBirth}T00:00:00`);

        if (Number.isNaN(birthDate.getTime())) {
            throw new Error(`${label}: неправильний формат дати.`);
        }

        if (birthDate > new Date()) {
            throw new Error(`${label}: дата народження не може бути в майбутньому.`);
        }

        persons.push({
            name,
            passportData,
            dateOfBirth,
            isChild
        });
    }

    return {
        tourId: selectedTourId,
        startDate,
        numberOfAdults,
        numberOfChildren,
        comment,
        persons
    };
}

function generateIdempotencyKey() {
    if (crypto.randomUUID) {
        return crypto.randomUUID();
    }

    return `${Date.now()}-${Math.random().toString(16).slice(2)}`;
}

function setBookingButtonState(disabled, text = null) {
    const button = document.querySelector("#step2 .btn-save");

    if (!button) return;

    button.disabled = disabled;

    if (text !== null) {
        button.textContent = text;
    } else {
        button.textContent = "Забронювати";
    }

    button.style.opacity = disabled ? "0.6" : "1";
    button.style.cursor = disabled ? "not-allowed" : "pointer";
}

async function saveSelectedPresets() {
    const forms = document.querySelectorAll(".person-form");

    for (const form of forms) {
        const checkbox = form.querySelector(".person-save");

        if (!checkbox?.checked) continue;

        const person = {
            name: form.querySelector(".person-name").value.trim(),
            passportData: form.querySelector(".person-passport").value.trim(),
            dateOfBirth: form.querySelector(".person-birth").value,
            isChild: form.dataset.isChild === "true"
        };

        try {
            await TravelApi.post(API_SAVED_PERSONS, person);
        } catch {
            showMessage(
                "Бронювання створено, але не вдалося зберегти один із пресетів.",
                "error"
            );
        }
    }
}

async function confirmBooking() {
    if (isBookingSubmitting) return;

    let payload;

    try {
        payload = getBookingPayload();
    } catch (error) {
        showMessage(error.message);
        return;
    }

    const payloadJson = JSON.stringify(payload);

    if (
        !pendingBookingAttempt ||
        pendingBookingAttempt.payloadJson !== payloadJson
    ) {
        pendingBookingAttempt = {
            key: generateIdempotencyKey(),
            payloadJson,
            payload
        };
    }

    isBookingSubmitting = true;
    setBookingButtonState(true, "Відправлення...");

    try {
        await TravelApi.post(
            API_BOOKINGS,
            pendingBookingAttempt.payload,
            {
                idempotencyKey: pendingBookingAttempt.key
            }
        );

        pendingBookingAttempt = null;

        await saveSelectedPresets();

        closeBookingModal();
        closeTourModal();

        showMessage("Бронювання успішно створено.", "success");

        await loadBookings();
    } catch (error) {
        showMessage(getErrorMessage(error));

        if (error.status === 400 || error.status === 422) {
            pendingBookingAttempt = null;
        }
    } finally {
        isBookingSubmitting = false;
        setBookingButtonState(false);
    }
}

async function loadBookings() {
    try {
        const result = await TravelApi.get(`${API_BOOKINGS}/my`);

        bookings = Array.isArray(result) ? result : [];
        bookingsAvailable = true;

        renderBookings(bookings);
    } catch (error) {
        bookingsAvailable = false;

        showGridState(
            "bookingsGrid",
            getErrorMessage(error),
            loadBookings
        );
    }
}

function renderBookings(list) {
    const grid = document.getElementById("bookingsGrid");
    grid.innerHTML = "";

    if (!list || list.length === 0) {
        showGridState("bookingsGrid", "Бронювань поки немає.");
        return;
    }

    list.forEach(booking => {
        const card = document.createElement("div");
        card.className = "card";

        const status = String(booking.status || "").toLowerCase();
        const statusClass = `status-${status}`;

        const header = document.createElement("div");
        header.className = "card-header";

        const title = document.createElement("h4");
        title.textContent = booking.tourName || "Тур";

        const badge = document.createElement("span");
        badge.className = `status-badge ${statusClass}`;
        badge.textContent = booking.status || "Невідомо";

        header.append(title, badge);

        const meta1 = document.createElement("div");
        meta1.className = "meta";

        const start = document.createElement("span");
        start.textContent = `Початок: ${formatDate(booking.startDate)}`;

        const people = document.createElement("span");
        people.textContent = `${booking.numberOfPeople} ос.`;

        meta1.append(start, people);

        const meta2 = document.createElement("div");
        meta2.className = "meta";

        const price = document.createElement("span");
        price.textContent = `Сума: ${booking.totalPrice} $`;

        const date = document.createElement("span");
        date.textContent = `Заброньовано: ${formatDate(booking.bookingDate)}`;

        meta2.append(price, date);

        card.append(header, meta1, meta2);

        if (status !== "cancelled") {
            const actions = document.createElement("div");
            actions.className = "actions";

            const cancelButton = document.createElement("button");
            cancelButton.className = "btn-cancel-booking";
            cancelButton.textContent = "Скасувати";
            cancelButton.onclick = () => cancelBooking(booking.bookingId);

            actions.appendChild(cancelButton);
            card.appendChild(actions);
        }

        grid.appendChild(card);
    });
}

async function cancelBooking(id) {
    if (!confirm("Скасувати бронювання?")) return;

    try {
        await TravelApi.delete(`${API_BOOKINGS}/${id}`);

        showMessage("Бронювання скасовано.", "success");
        await loadBookings();
    } catch (error) {
        showMessage(getErrorMessage(error));
    }
}

function searchTours() {
    const value = document.getElementById("search").value.trim();

    currentSearch = value;
    currentPage = 1;

    updateSearchChip();
    loadTours(1);
}

function clearSearch() {
    document.getElementById("search").value = "";

    currentSearch = "";
    currentPage = 1;

    updateSearchChip();
    loadTours(1);
}

function logout() {
    TravelApi.logout().finally(() => {
        window.location.href = "index.html";
    });
}

function initializeSearch() {
    const input = document.getElementById("search");

    if (!input) return;

    input.addEventListener("input", () => {
        clearTimeout(searchTimer);

        searchTimer = setTimeout(() => {
            currentSearch = input.value.trim();
            currentPage = 1;

            updateSearchChip();
            loadTours(1);
        }, SEARCH_DELAY);
    });

    input.addEventListener("keydown", event => {
        if (event.key === "Enter") {
            clearTimeout(searchTimer);
            searchTours();
        }
    });
}

async function init() {
    initializeSearch();
    await loadDashboard();
}

init();
