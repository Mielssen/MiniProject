(() => {
    const API_BOOKINGS = "/api/bookings";

    function calcEndDate(startDate, durationDays) {
    if (!startDate) return "";

    const date = new Date(`${startDate}T00:00:00`);
    date.setDate(date.getDate() + Number(durationDays) - 1);

    return window.dateInputValue(date.toISOString());
}

    function clearFieldErrors() {
        document.querySelectorAll(".booking-field-error").forEach(el => el.remove());
        document.querySelectorAll(".booking-field-invalid").forEach(el =>
            el.classList.remove("booking-field-invalid")
        );
    }

    function showFieldError(input, message) {
        if (!input) return;
        input.classList.add("booking-field-invalid");

        const error = document.createElement("div");
        error.className = "booking-field-error";
        error.textContent = message;
        error.style.cssText = "color:#c0392b;font-size:12px;margin-top:4px;";
        input.insertAdjacentElement("afterend", error);
    }

    function showApiErrors(error) {
        clearFieldErrors();
        const errors = error?.data?.errors;

        if (!errors || typeof errors !== "object") {
            window.showMessage(window.getErrorMessage(error));
            return;
        }

        const map = {
            startdate: "bookingStartDate",
            numberofadults: "bookingAdults",
            numberofchildren: "bookingChildren",
            comment: "bookingComment"
        };

        let displayed = false;

        for (const [key, value] of Object.entries(errors)) {
            const message = Array.isArray(value) ? value.join(" ") : String(value);
            const normalized = key.split(".").pop().toLowerCase();

            if (map[normalized]) {
                showFieldError(document.getElementById(map[normalized]), message);
                displayed = true;
                continue;
            }

            const match = key.match(/persons\[(\d+)\]\.(\w+)/i);
            if (match) {
                const form = document.querySelectorAll(".person-form")[Number(match[1])];
                const selector = {
                    name: ".person-name",
                    passportdata: ".person-passport",
                    dateofbirth: ".person-birth"
                }[match[2].toLowerCase()];
                if (form && selector) {
                    showFieldError(form.querySelector(selector), message);
                    displayed = true;
                }
            }
        }

        window.showMessage(displayed
            ? "Перевірте поля, позначені помилками."
            : window.getErrorMessage(error));
    }

    function getPayload() {
        const state = window.DashboardState;
        const forms = document.querySelectorAll(".person-form");
        const persons = [];

        for (const form of forms) {
            const name = form.querySelector(".person-name").value.trim();
            const passportData = form.querySelector(".person-passport").value.trim();
            const dateOfBirth = form.querySelector(".person-birth").value;
            const isChild = form.dataset.isChild === "true";
            const label = form.querySelector(".person-label").textContent;

            if (!name) throw new Error(`${label}: ім'я обов'язкове.`);
            if (!/^[a-zA-Zа-яА-ЯіІїЇєЄґҐ\s'-]+$/.test(name)) {
                throw new Error(`${label}: ім'я має містити тільки літери.`);
            }
            if (!passportData) throw new Error(`${label}: паспортні дані обов'язкові.`);
            if (!/^\d+$/.test(passportData)) {
                throw new Error(`${label}: паспортні дані мають містити тільки цифри.`);
            }
            if (!dateOfBirth) throw new Error(`${label}: дата народження обов'язкова.`);

            const birthDate = new Date(`${dateOfBirth}T00:00:00`);
            if (Number.isNaN(birthDate.getTime())) {
                throw new Error(`${label}: неправильний формат дати.`);
            }
            if (birthDate > new Date()) {
                throw new Error(`${label}: дата народження не може бути в майбутньому.`);
            }

            persons.push({ name, passportData, dateOfBirth, isChild });
        }

        return {
            tourId: state.selectedTourId,
            startDate: document.getElementById("bookingStartDate").value,
            numberOfAdults: Number(document.getElementById("bookingAdults").value),
            numberOfChildren: Number(document.getElementById("bookingChildren").value),
            comment: document.getElementById("bookingComment").value.trim(),
            persons
        };
    }

    function idempotencyKey() {
        return crypto.randomUUID
            ? crypto.randomUUID()
            : `${Date.now()}-${Math.random().toString(16).slice(2)}`;
    }

    function setSubmitState(disabled, text = "Забронювати") {
        const button = document.querySelector("#step2 .btn-save");
        if (!button) return;
        button.disabled = disabled;
        button.textContent = disabled ? text : "Забронювати";
        button.style.opacity = disabled ? "0.6" : "1";
        button.style.cursor = disabled ? "not-allowed" : "pointer";
    }

    window.BookingModule = {
        open() {
            const state = window.DashboardState;
            if (!state.currentTour) return;

            state.pendingBookingAttempt = null;
            const from = window.dateInputValue(window.getTourAvailableFrom(state.currentTour));
const to = window.dateInputValue(window.getTourAvailableTo(state.currentTour));
const duration = window.getTourDuration(state.currentTour);
const start = document.getElementById("bookingStartDate");

const lastStartDate = new Date(`${to}T00:00:00`);
lastStartDate.setDate(lastStartDate.getDate() - duration + 1);

start.value = from;
start.min = from;
start.max = window.dateInputValue(lastStartDate.toISOString());

            document.getElementById("bookingEndDate").value =
                calcEndDate(from, window.getTourDuration(state.currentTour));
            document.getElementById("bookingAdults").value = 1;
            document.getElementById("bookingChildren").value = 0;
            document.getElementById("bookingTotal").value = "Суму розрахує сервер";
            document.getElementById("bookingComment").value = "";

            clearFieldErrors();
            document.getElementById("step1").classList.remove("hidden");
            document.getElementById("step2").classList.add("hidden");
            document.getElementById("bookingModal").classList.remove("hidden");
            document.querySelector(".booking-modal-content").style.maxWidth = "380px";
            this.updatePeople();
        },

        close() {
            document.querySelector(".booking-modal-content").style.maxWidth = "380px";
            document.getElementById("bookingModal").classList.add("hidden");
            window.DashboardState.pendingBookingAttempt = null;
        },

        back() {
            document.querySelector(".booking-modal-content").style.maxWidth = "380px";
            document.getElementById("step1").classList.remove("hidden");
            document.getElementById("step2").classList.add("hidden");
        },

        updateStartDate() {
            const state = window.DashboardState;
            if (!state.currentTour) return;
            document.getElementById("bookingEndDate").value =
                calcEndDate(
                    document.getElementById("bookingStartDate").value,
                    window.getTourDuration(state.currentTour)
                );
        },

        updatePeople() {
            const adults = Number(document.getElementById("bookingAdults").value) || 0;
            const children = Number(document.getElementById("bookingChildren").value) || 0;
            document.getElementById("bookingTotal").value =
                adults + children > 20 ? "Максимум 20 учасників" : "Суму розрахує сервер";
        },

        async nextStep() {
            const state = window.DashboardState;
            const adults = Number(document.getElementById("bookingAdults").value);
            const children = Number(document.getElementById("bookingChildren").value);
            const startDate = document.getElementById("bookingStartDate").value;

            if (!state.currentTour) {
                window.showMessage("Спочатку виберіть тур.");
                return;
            }
            if (!startDate) {
                alert("Оберіть дату початку туру.");
                return;
            }

            const startInput = document.getElementById("bookingStartDate");

            if (startDate < startInput.min || startDate > startInput.max) {
                alert("Обрана дата не підходить для повної тривалості подорожі.");
                return;
            }

            if (!Number.isInteger(adults) || !Number.isInteger(children) ||
                adults < 1 || children < 0 || adults + children > 20) {
                alert("Перевірте кількість дорослих і дітей. Допустимо від 1 до 20 учасників.");
                return;
            }

            clearFieldErrors();
            document.getElementById("step2TourName").textContent =
                window.getTourName(state.currentTour);
            document.getElementById("step2StartDate").textContent =
                window.formatDate(startDate);
            document.getElementById("step2EndDate").textContent =
                window.formatDate(calcEndDate(startDate, window.getTourDuration(state.currentTour)));
            document.getElementById("step2Total").textContent = "Суму розрахує сервер";
            document.getElementById("step2Comment").textContent =
                document.getElementById("bookingComment").value || "—";

            try {
                await window.SavedPersonsModule.load();
            } catch {
                state.savedPersons = [];
                window.showMessage(
                    "Не вдалося завантажити збережених учасників. Дані можна ввести вручну."
                );
            }

            this.renderPersons(adults, children);
            document.getElementById("step1").classList.add("hidden");
            document.getElementById("step2").classList.remove("hidden");
            document.querySelector(".booking-modal-content").style.maxWidth = "700px";
        },

        renderPersons(adults, children) {
            const container = document.getElementById("personsForms");
            container.innerHTML = "";

            for (let i = 0; i < adults + children; i++) {
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
                        <label style="display:flex;align-items:center;gap:8px;font-size:13px;margin-top:5px;">
                            <input type="checkbox" class="person-save"> Зберегти в пресети
                        </label>
                    ` : ""}
                `;
                container.appendChild(form);
            }

            window.SavedPersonsModule.prefillFirstPerson();
        },

        async confirm() {
            const state = window.DashboardState;
            if (state.isBookingSubmitting) return;

            let payload;
            try {
                payload = getPayload();
            } catch (error) {
                window.showMessage(error.message);
                return;
            }

            const payloadJson = JSON.stringify(payload);
            if (!state.pendingBookingAttempt ||
                state.pendingBookingAttempt.payloadJson !== payloadJson) {
                state.pendingBookingAttempt = {
                    key: idempotencyKey(),
                    payloadJson,
                    payload
                };
            }

            state.isBookingSubmitting = true;
            setSubmitState(true, "Відправлення...");

            try {
                await TravelApi.post(API_BOOKINGS, state.pendingBookingAttempt.payload, {
                    idempotencyKey: state.pendingBookingAttempt.key
                });

                state.pendingBookingAttempt = null;
                await window.SavedPersonsModule.saveSelected();
                this.close();
                window.TourDetailsModule.close();
                window.showMessage("Бронювання успішно створено.", "success");
                await window.MyBookingsModule.load();
            } catch (error) {
                if (error.status === 400 || error.status === 422) {
                    state.pendingBookingAttempt = null;
                    showApiErrors(error);
                } else {
                    window.showMessage(window.getErrorMessage(error));
                }
            } finally {
                state.isBookingSubmitting = false;
                setSubmitState(false);
            }
        }
    };

    document.addEventListener("input", event => {
        if (event.target.matches("#bookingModal input, #bookingModal textarea")) {
            event.target.classList.remove("booking-field-invalid");
            const next = event.target.nextElementSibling;
            if (next?.classList.contains("booking-field-error")) next.remove();
        }
    });

    window.openBookingModal = () => window.BookingModule.open();
    window.closeBookingModal = () => window.BookingModule.close();
    window.goToStep1 = () => window.BookingModule.back();
    window.goToStep2 = () => window.BookingModule.nextStep();
    window.onStartDateChange = () => window.BookingModule.updateStartDate();
    window.onPeopleChange = () => window.BookingModule.updatePeople();
    window.confirmBooking = () => window.BookingModule.confirm();
})();
