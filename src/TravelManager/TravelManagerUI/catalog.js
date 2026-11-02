(() => {
    const API_CATALOG = "/api/v1/catalog/items";
    const PAGE_SIZE = 10;
    const SEARCH_DELAY = 400;

    window.CatalogModule = {
        applyResult(result) {
            const state = window.DashboardState;
            const items = Array.isArray(result)
                ? result
                : result?.items || result?.Items || [];

            state.tours = items;
            state.totalCount = result?.totalCount ?? result?.TotalCount ?? items.length;
            state.totalPages = result?.totalPages ?? result?.TotalPages ?? 1;
            state.currentPage = result?.page ?? result?.Page ?? state.currentPage;

            this.render(items);
            this.renderPagination();
        },

        async load(page = 1) {
            const state = window.DashboardState;
            state.currentPage = page;
            window.showGridState("toursGrid", "Завантаження турів...");

            const params = new URLSearchParams({
                page: String(page),
                pageSize: String(PAGE_SIZE)
            });

            if (state.currentSearch) params.set("search", state.currentSearch);

            try {
                const result = await TravelApi.get(`${API_CATALOG}?${params}`);
                state.catalogAvailable = true;
                this.applyResult(result);
                window.hideMessage();
            } catch (error) {
                state.catalogAvailable = false;
                window.showGridState(
                    "toursGrid",
                    window.getErrorMessage(error),
                    () => this.load(state.currentPage)
                );
                if (error.status === 429) window.showMessage(error.message);
            }
        },

        render(list) {
            const grid = document.getElementById("toursGrid");
            grid.innerHTML = "";

            if (!list?.length) {
                window.showGridState(
                    "toursGrid",
                    window.DashboardState.currentSearch
                        ? "За вашим запитом турів не знайдено."
                        : "Доступних турів поки немає."
                );
                return;
            }

            list.forEach(tour => {
                const card = document.createElement("div");
                card.className = "tour-card";

                const assets = window.getTourAssets(tour);
                const firstImage = assets.find(asset =>
                    (asset.assetType || asset.AssetType) === "image"
                );

                if (firstImage) {
    const image = document.createElement("img");
    image.className = "tour-card-img";

    const imageUrl = firstImage.url || firstImage.Url;

    image.src = imageUrl.startsWith("http")
        ? imageUrl
        : `http://localhost:5000${imageUrl.startsWith("/") ? "" : "/"}${imageUrl}`;

    image.alt = window.getTourName(tour);
    card.appendChild(image);
} else {
                    const placeholder = document.createElement("div");
                    placeholder.className = "tour-card-img-placeholder";
                    placeholder.textContent = "🌍";
                    card.appendChild(placeholder);
                }

                const body = document.createElement("div");
                body.className = "tour-card-body";

                const type = document.createElement("div");
                type.className = "tour-card-type";
                type.textContent = window.getTourTypeName(tour);

                const title = document.createElement("h4");
                title.textContent = window.getTourName(tour);

                const description = document.createElement("p");
                description.className = "tour-card-desc";
                description.textContent = window.getTourDescription(tour);

                const meta = document.createElement("div");
                meta.className = "tour-card-meta";

                const dates = document.createElement("span");
                dates.textContent =
                    `${window.formatDate(window.getTourAvailableFrom(tour))} → ${window.formatDate(window.getTourAvailableTo(tour))}`;

                const duration = document.createElement("span");
                duration.textContent = `${window.getTourDuration(tour)} дн.`;
                meta.append(dates, duration);

                const price = document.createElement("div");
                price.className = "tour-card-price";
                price.textContent = `${window.getTourPrice(tour)} $ `;

                const note = document.createElement("span");
                note.style.cssText = "font-size:12px;color:#b2bec3;";
                note.textContent = "/ особу";
                price.appendChild(note);

                const button = document.createElement("button");
                button.className = "btn-view";
                button.textContent = "Детальніше";
                button.onclick = () => window.TourDetailsModule.open(tour);

                body.append(type, title, description, meta, price, button);
                card.appendChild(body);
                grid.appendChild(card);
            });
        },

        renderPagination() {
            const state = window.DashboardState;
            let pagination = document.getElementById("toursPagination");

            if (!pagination) {
                pagination = document.createElement("div");
                pagination.id = "toursPagination";
                pagination.style.cssText =
                    "display:flex;justify-content:center;align-items:center;flex-wrap:wrap;gap:8px;margin:25px 0;";
                document.getElementById("toursGrid").after(pagination);
            }

            pagination.innerHTML = "";
            if (state.totalPages <= 1) return;

            const addButton = (label, page, disabled = false, active = false) => {
                const button = document.createElement("button");
                button.textContent = label;
                button.disabled = disabled;
                button.style.cssText = `
                    min-width:36px;padding:8px 12px;border:1px solid #dfe6e9;
                    border-radius:8px;background:${active ? "#00b894" : "#fff"};
                    color:${active ? "#fff" : "#2d3436"};
                    cursor:${disabled ? "not-allowed" : "pointer"};
                    opacity:${disabled ? "0.5" : "1"};
                `;
                button.onclick = () => this.load(page);
                pagination.appendChild(button);
            };

            addButton("←", state.currentPage - 1, state.currentPage <= 1);

            const start = Math.max(1, state.currentPage - 2);
            const end = Math.min(state.totalPages, state.currentPage + 2);

            if (start > 1) {
                addButton("1", 1, false, state.currentPage === 1);
                if (start > 2) pagination.append("…");
            }

            for (let page = start; page <= end; page++) {
                addButton(String(page), page, false, page === state.currentPage);
            }

            if (end < state.totalPages) {
                if (end < state.totalPages - 1) pagination.append("…");
                addButton(String(state.totalPages), state.totalPages, false,
                    state.currentPage === state.totalPages);
            }

            addButton("→", state.currentPage + 1, state.currentPage >= state.totalPages);

            const info = document.createElement("span");
            info.textContent =
                `Сторінка ${state.currentPage} з ${state.totalPages} · Усього турів: ${state.totalCount}`;
            info.style.cssText =
                "width:100%;text-align:center;color:#636e72;font-size:13px;margin-top:5px;";
            pagination.appendChild(info);
        },

        updateSearchChip() {
            const state = window.DashboardState;
            const chip = document.getElementById("searchChip");
            const text = document.getElementById("searchText");
            if (!chip || !text) return;

            if (state.currentSearch) {
                text.textContent = state.currentSearch;
                chip.classList.remove("hidden");
            } else {
                chip.classList.add("hidden");
            }
        },

        search() {
            const state = window.DashboardState;
            clearTimeout(state.searchTimer);
            state.currentSearch = document.getElementById("search").value.trim();
            state.currentPage = 1;
            this.updateSearchChip();
            this.load(1);
        },

        clearSearch() {
            const state = window.DashboardState;
            clearTimeout(state.searchTimer);
            document.getElementById("search").value = "";
            state.currentSearch = "";
            state.currentPage = 1;
            this.updateSearchChip();
            this.load(1);
        },

        initializeSearch() {
            const input = document.getElementById("search");
            if (!input) return;

            input.addEventListener("input", () => {
                const state = window.DashboardState;
                clearTimeout(state.searchTimer);
                state.searchTimer = setTimeout(() => {
                    state.currentSearch = input.value.trim();
                    state.currentPage = 1;
                    this.updateSearchChip();
                    this.load(1);
                }, SEARCH_DELAY);
            });

            input.addEventListener("keydown", event => {
                if (event.key === "Enter") {
                    event.preventDefault();
                    this.search();
                }
            });
        }
    };

    window.searchTours = () => window.CatalogModule.search();
    window.clearSearch = () => window.CatalogModule.clearSearch();
})();
