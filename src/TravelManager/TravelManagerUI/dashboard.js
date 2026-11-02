(() => {
    window.DashboardState = {
        tourTypes: [],
        tours: [],
        bookings: [],
        selectedTourId: null,
        currentTour: null,
        sliderIndex: 0,
        sliderItems: [],
        savedPersons: [],
        currentPersonIndex: null,
        currentPage: 1,
        totalPages: 1,
        totalCount: 0,
        currentSearch: "",
        searchTimer: null,
        isBookingSubmitting: false,
        pendingBookingAttempt: null,
        catalogAvailable: true,
        bookingsAvailable: true
    };

    window.showMessage = function (message, type = "error") {
        let element = document.getElementById("dashboardMessage");

        if (!element) {
            element = document.createElement("div");
            element.id = "dashboardMessage";
            element.style.cssText =
                "padding:12px 16px;margin:12px 0;border-radius:10px;font-size:14px;display:none;";
            document.querySelector(".container").prepend(element);
        }

        element.textContent = message;
        element.style.display = "block";
        element.style.background = type === "error" ? "#ffecec" : "#e8f8f1";
        element.style.color = type === "error" ? "#c0392b" : "#16845b";
    };

    window.hideMessage = function () {
        const element = document.getElementById("dashboardMessage");
        if (element) element.style.display = "none";
    };

    window.showGridState = function (gridId, message, retryAction = null) {
        const grid = document.getElementById(gridId);
        if (!grid) return;
        grid.innerHTML = "";

        const container = document.createElement("div");
        container.style.cssText =
            "padding:25px;text-align:center;color:#636e72;grid-column:1/-1;";

        const text = document.createElement("p");
        text.textContent = message;
        container.appendChild(text);

        if (retryAction) {
            const button = document.createElement("button");
            button.textContent = "Спробувати ще раз";
            button.style.cssText =
                "margin-top:10px;padding:9px 18px;border:none;border-radius:8px;cursor:pointer;";
            button.onclick = retryAction;
            container.appendChild(button);
        }

        grid.appendChild(container);
    };

    window.formatDate = function (value) {
        if (!value) return "";
        const date = new Date(value);
        if (Number.isNaN(date.getTime())) return "";
        return date.toLocaleDateString("uk-UA", {
            year: "numeric", month: "2-digit", day: "2-digit"
        });
    };

    window.dateInputValue = value => value ? String(value).split("T")[0] : "";
    window.getTourId = tour => tour?.id ?? tour?.tourId ?? tour?.Id ?? tour?.TourId ?? null;
    window.getTourAssets = tour => tour?.assets ?? tour?.Assets ?? [];
    window.getTourName = tour => tour?.name ?? tour?.Name ?? "Тур";
    window.getTourPrice = tour => tour?.price ?? tour?.Price ?? 0;
    window.getTourDuration = tour => tour?.durationDays ?? tour?.DurationDays ?? 0;
    window.getTourAvailableFrom = tour => tour?.availableFrom ?? tour?.AvailableFrom ?? "";
    window.getTourAvailableTo = tour => tour?.availableTo ?? tour?.AvailableTo ?? "";
    window.getTourDescription = tour => tour?.description ?? tour?.Description ?? "";

    window.getTourTypeName = function (tour) {
        const state = window.DashboardState;
        const direct = tour?.typeName ?? tour?.TypeName;
        if (direct) return direct;

        const typeId = tour?.typeId ?? tour?.TypeId;
        const type = state.tourTypes.find(item =>
            (item.typeId ?? item.TypeId ?? item.id ?? item.Id) === typeId
        );
        return type?.name ?? type?.Name ?? "";
    };

    window.getErrorMessage = function (error) {
        if (error instanceof ApiError) return error.message;
        return error?.message || "Сталася непередбачена помилка.";
    };

    async function loadDashboard() {
        window.hideMessage();
        window.showGridState("toursGrid", "Завантаження турів...");
        window.showGridState("bookingsGrid", "Завантаження бронювань...");

        try {
            const data = await TravelApi.get("/api/dashboard");
            const catalog = data?.catalog;
            const userBookings = data?.bookings;

            if (catalog) {
                window.DashboardState.catalogAvailable = true;
                window.CatalogModule.applyResult(catalog);
            } else {
                window.DashboardState.catalogAvailable = false;
                window.showGridState(
                    "toursGrid",
                    data?.errors?.catalog || "Каталог тимчасово недоступний.",
                    loadDashboard
                );
            }

            if (Array.isArray(userBookings)) {
                window.DashboardState.bookingsAvailable = true;
                window.DashboardState.bookings = userBookings;
                window.MyBookingsModule.render(userBookings);
            } else {
                window.DashboardState.bookingsAvailable = false;
                window.showGridState(
                    "bookingsGrid",
                    data?.errors?.bookings || "Бронювання тимчасово недоступні.",
                    () => window.MyBookingsModule.load()
                );
            }

            if (data?.partial) {
                const errors = [];
                if (data.errors?.catalog) errors.push("каталог");
                if (data.errors?.bookings) errors.push("бронювання");
                window.showMessage(
                    `Частина даних тимчасово недоступна: ${errors.join(", ")}. Інші дані завантажено.`
                );
            }
        } catch (error) {
            window.showMessage(window.getErrorMessage(error));
            window.showGridState("toursGrid", "Не вдалося завантажити каталог.", loadDashboard);
            window.showGridState("bookingsGrid", "Не вдалося завантажити бронювання.", loadDashboard);
        }
    }

    window.logout = function () {
        TravelApi.logout();
    };

    async function init() {
        if (!TravelApi.isAuthenticated()) {
            window.location.href = "index.html";
            return;
        }

        window.CatalogModule.initializeSearch();
        await loadDashboard();
    }

    init();
})();
