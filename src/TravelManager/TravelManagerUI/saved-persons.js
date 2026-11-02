(() => {
    const API_SAVED_PERSONS = "/api/savedpersons";

    window.SavedPersonsModule = {
        async load() {
            const result = await TravelApi.get(API_SAVED_PERSONS);
            window.DashboardState.savedPersons = Array.isArray(result) ? result : [];
            return window.DashboardState.savedPersons;
        },

        renderModal(personIndex) {
            const state = window.DashboardState;
            state.currentPersonIndex = personIndex;

            const list = document.getElementById("presetsList");
            list.innerHTML = "";

            if (!state.savedPersons.length) {
                list.innerHTML = "<p style='color:#636e72;'>Пресетів немає</p>";
            } else {
                state.savedPersons.forEach(person => {
                    const id = person.personId ?? person.PersonId ?? person.id ?? person.Id;
                    const item = document.createElement("div");
                    item.className = "preset-item";

                    const info = document.createElement("div");
                    info.className = "preset-info";

                    const name = document.createElement("strong");
                    name.textContent = person.name ?? person.Name ?? "";

                    const details = document.createElement("span");
                    details.textContent =
                        `${(person.isChild ?? person.IsChild) ? "Дитина" : "Дорослий"} · ${person.passportData ?? person.PassportData ?? "—"}`;

                    info.append(name, details);

                    const actions = document.createElement("div");
                    actions.style.cssText = "display:flex;gap:8px;";

                    const select = document.createElement("button");
                    select.className = "btn-confirm";
                    select.textContent = "Обрати";
                    select.onclick = () => this.apply(id);

                    const remove = document.createElement("button");
                    remove.className = "btn-decline";
                    remove.textContent = "✕";
                    remove.onclick = () => this.remove(id);

                    actions.append(select, remove);
                    item.append(info, actions);
                    list.appendChild(item);
                });
            }

            document.getElementById("presetsModal").classList.remove("hidden");
        },

        apply(personId) {
            const state = window.DashboardState;
            const person = state.savedPersons.find(item =>
                (item.personId ?? item.PersonId ?? item.id ?? item.Id) === personId
            );
            if (!person || state.currentPersonIndex === null) return;

            const form = document.querySelectorAll(".person-form")[state.currentPersonIndex];
            if (!form) return;

            form.querySelector(".person-name").value = person.name ?? person.Name ?? "";
            form.querySelector(".person-passport").value =
                person.passportData ?? person.PassportData ?? "";
            form.querySelector(".person-birth").value =
                window.dateInputValue(person.dateOfBirth ?? person.DateOfBirth);

            this.closeModal();
        },

        async remove(personId) {
            try {
                await TravelApi.delete(`${API_SAVED_PERSONS}/${personId}`);
                await this.load();
                this.renderModal(window.DashboardState.currentPersonIndex);
            } catch (error) {
                window.showMessage(window.getErrorMessage(error));
            }
        },

        closeModal() {
            document.getElementById("presetsModal").classList.add("hidden");
            window.DashboardState.currentPersonIndex = null;
        },

        async prefillFirstPerson() {
    const forms = document.querySelectorAll(".person-form");
    if (!forms.length) return;

    try {
        const profile = await TravelApi.get("/api/v1/users/me");

        forms[0].querySelector(".person-name").value =
            profile.displayName ?? "";

        forms[0].querySelector(".person-passport").value =
            profile.passportData ?? "";

        forms[0].querySelector(".person-birth").value =
            window.dateInputValue(profile.dateOfBirth);
    } catch {
        window.showMessage(
            "Не вдалося завантажити ваші дані профілю. Перевірте їх перед бронюванням."
        );

        forms[0].querySelector(".person-name").readOnly = false;
        forms[0].querySelector(".person-passport").readOnly = false;
        forms[0].querySelector(".person-birth").readOnly = false;
    }
},

        async saveSelected() {
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
                    window.showMessage(
                        "Бронювання створено, але не вдалося зберегти один із пресетів."
                    );
                }
            }
        }
    };

    window.openPresetsModal = index => window.SavedPersonsModule.renderModal(index);
    window.closePresetsModal = () => window.SavedPersonsModule.closeModal();
})();
