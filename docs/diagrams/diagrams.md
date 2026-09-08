```mermaid
graph TD
    %% 1. Блок клієнта та шлюзу
    Client["Клієнт (Web)"] -->|"POST /api/bookings (JWT)"| Gateway["ApiGateway (Ocelot) :5000"]

    %% 2. Блок розподілу на мікросервіси
    Gateway -->|"Маршрутизація / Перевірка JWT"| AuthS["AuthService :5001"]
    Gateway -->|"Маршрутизація / Перевірка JWT"| TourS["TourService :5002 / :5102"]
    Gateway -->|"Пересилає запит по REST"| BookS["BookingService :5003 / :5103"]

    %% 3. Взаємодія сервісів між собою (як у послідовності)
    BookS -->|"REST CheckAvailability (ціна, дати)"| TourS

    %% 4. Блок баз даних
    AuthS --> AuthDB[("auth (MySQL)")]
    TourS --> TourDB[("tour (MySQL)")]
    BookS --> BookDB[("booking (MySQL)")]
