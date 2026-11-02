(() => {
    const API_BOOKINGS = "/api/bookings";

    window.MyBookingsModule = {
        async load() {
            try {
                const result = await TravelApi.get(`${API_BOOKINGS}/my`);
                const state = window.DashboardState;
                state.bookings = Array.isArray(result) ? result : [];
                state.bookingsAvailable = true;
                this.render(state.bookings);
            } catch (error) {
                window.DashboardState.bookingsAvailable = false;
                window.showGridState(
                    "bookingsGrid",
                    window.getErrorMessage(error),
                    () => this.load()
                );
            }
        },

        render(list) {
            const grid = document.getElementById("bookingsGrid");
            grid.innerHTML = "";

            if (!list?.length) {
                window.showGridState("bookingsGrid", "Бронювань поки немає.");
                return;
            }

            list.forEach(booking => {
                const statusValue = booking.status ?? booking.Status ?? "";
                const status = String(statusValue).toLowerCase();
                const bookingId = booking.bookingId ?? booking.BookingId;
                const card = document.createElement("div");
                card.className = "card";

                const header = document.createElement("div");
                header.className = "card-header";

                const title = document.createElement("h4");
                title.textContent = booking.tourName ?? booking.TourName ?? "Тур";

                const badge = document.createElement("span");
                badge.className = `status-badge status-${status}`;
                badge.textContent = statusValue || "Невідомо";
                header.append(title, badge);

                const meta1 = document.createElement("div");
                meta1.className = "meta";

                const start = document.createElement("span");
                start.textContent =
                    `Початок: ${window.formatDate(booking.startDate ?? booking.StartDate)}`;

                const people = document.createElement("span");
                people.textContent =
                    `${booking.numberOfPeople ?? booking.NumberOfPeople ?? 0} ос.`;
                meta1.append(start, people);

                const meta2 = document.createElement("div");
                meta2.className = "meta";

                const price = document.createElement("span");
                price.textContent =
                    `Сума: ${booking.totalPrice ?? booking.TotalPrice ?? 0} $`;

                const date = document.createElement("span");
                date.textContent =
                    `Заброньовано: ${window.formatDate(booking.bookingDate ?? booking.BookingDate)}`;
                meta2.append(price, date);

                card.append(header, meta1, meta2);

                if (status !== "cancelled") {
                    const actions = document.createElement("div");
                    actions.className = "actions";

                    const cancel = document.createElement("button");
                    cancel.className = "btn-cancel-booking";
                    cancel.textContent = "Скасувати";
                    cancel.onclick = () => this.cancel(bookingId);

                    actions.appendChild(cancel);
                    card.appendChild(actions);
                }

                grid.appendChild(card);
            });
        },

        async cancel(id) {
            if (!confirm("Скасувати бронювання?")) return;

            try {
                await TravelApi.delete(`${API_BOOKINGS}/${id}`);
                window.showMessage("Бронювання скасовано.", "success");
                await this.load();
            } catch (error) {
                window.showMessage(window.getErrorMessage(error));
            }
        }
    };
})();
