(() => {
    window.TourDetailsModule = {
        open(tour) {
            const state = window.DashboardState;
            state.selectedTourId = window.getTourId(tour);
            state.currentTour = tour;
            state.sliderIndex = 0;
            state.sliderItems = window.getTourAssets(tour);

            document.getElementById("modalType").textContent = window.getTourTypeName(tour);
            document.getElementById("modalName").textContent = window.getTourName(tour);
            document.getElementById("modalDesc").textContent = window.getTourDescription(tour);
            document.getElementById("modalPrice").textContent =
                `${window.getTourPrice(tour)} $ / особу`;
            document.getElementById("modalDays").textContent =
                `${window.getTourDuration(tour)} дн.`;
            document.getElementById("modalFrom").textContent =
                window.formatDate(window.getTourAvailableFrom(tour));
            document.getElementById("modalTo").textContent =
                window.formatDate(window.getTourAvailableTo(tour));
            document.getElementById("bookingTourName").textContent =
                window.getTourName(tour);

            this.renderSlider();
            document.getElementById("tourModal").classList.remove("hidden");
        },

        renderSlider() {
    const state = window.DashboardState;
    const container = document.getElementById("sliderContainer");

    if (!state.sliderItems.length) {
        container.innerHTML = '<div class="slider-placeholder">🌍</div>';
        return;
    }

    const slides = state.sliderItems.map(asset => {
        const url = asset.url || asset.Url;
        const type = asset.assetType || asset.AssetType;

        const imageUrl = url.startsWith("http")
            ? url
            : `http://localhost:5000${url.startsWith("/") ? "" : "/"}${url}`;

        return type === "image"
            ? `<img src="${imageUrl}" alt="Зображення туру">`
            : `<video src="${imageUrl}" controls></video>`;
    }).join("");

    const dots = state.sliderItems.map((_, index) =>
        `<button class="slider-dot ${index === 0 ? "active" : ""}" onclick="goToSlide(${index})"></button>`
    ).join("");

    container.innerHTML = `
        <div class="slider">
            <div class="slider-track" id="sliderTrack">${slides}</div>
            ${state.sliderItems.length > 1 ? `
                <button class="slider-btn prev" onclick="prevSlide()">❮</button>
                <button class="slider-btn next" onclick="nextSlide()">❯</button>
                <div class="slider-dots" id="sliderDots">${dots}</div>
            ` : ""}
        </div>
    `;
},

        goToSlide(index) {
            const state = window.DashboardState;
            if (!state.sliderItems.length) return;

            state.sliderIndex = index;
            const track = document.getElementById("sliderTrack");
            if (track) track.style.transform = `translateX(-${index * 100}%)`;

            document.querySelectorAll(".slider-dot").forEach((dot, i) => {
                dot.classList.toggle("active", i === index);
            });
        },

        close() {
            document.getElementById("tourModal").classList.add("hidden");
        }
    };

    window.goToSlide = index => window.TourDetailsModule.goToSlide(index);
    window.prevSlide = () => {
        const s = window.DashboardState;
        window.TourDetailsModule.goToSlide(
            s.sliderIndex > 0 ? s.sliderIndex - 1 : s.sliderItems.length - 1
        );
    };
    window.nextSlide = () => {
        const s = window.DashboardState;
        window.TourDetailsModule.goToSlide(
            s.sliderIndex < s.sliderItems.length - 1 ? s.sliderIndex + 1 : 0
        );
    };
    window.closeTourModal = () => window.TourDetailsModule.close();
})();
