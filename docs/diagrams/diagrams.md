```mermaid
graph TD
    %% Заголовок діаграми
    %% title Архітектура розгортання сервісів
    
    %% Вузли
    Client("Клієнт<br/>(Web)")
    Gateway("ApiGateway (Ocelot)<br/>:5000")
    
    AuthS("AuthService<br/>:5001")
    TourS("TourService<br/>:5002 / :5102")
    BookS("BookingService<br/>:5003 / :5103")
    
    AuthDB[(auth / MySQL)]
    TourDB[(tour / MySQL)]
    BookDB[(booking / MySQL)]
    
    %% Зв'язки
    Client --> Gateway
    
    Gateway --> AuthS
    Gateway --> TourS
    Gateway --> BookS
    
    AuthS --> AuthDB
    TourS --> TourDB
    BookS --> BookDB

    %% Стилізація блоків (опціонально, для краси)
    classDef service fill:#fff,stroke:#333,stroke-width:1px,rx:5,ry:5;
    classDef db fill:#e1f5fe,stroke:#0277bd,stroke-width:1px,rx:5,ry:5;
    classDef client fill:#f3e5f5,stroke:#7b1fa2,stroke-width:1px,rx:10,ry:10;
    
    class Gateway,AuthS,TourS,BookS service;
    class AuthDB,TourDB,BookDB db;
    class Client client;
---

### 2. Діаграма послідовності: Моноліт (Sequence Diagram)

Це ваша друга схема (вертикальні лінії, час зверху вниз).

```markdown
```mermaid
sequenceDiagram
    participant Client as Клієнт
    participant Gateway as Gateway
    participant Monolith as Моноліт
    participant DB as БД

    Note over Monolith: Спільна БД для всіх
    
    Client->>Gateway: POST /api/bookings (JWT)
    activate Gateway
    Gateway->>Monolith: Маршрутизує запит
    activate Monolith
    
    Monolith->>Monolith: перевірка JWT
    
    Monolith->>DB: SELECT tour
    activate DB
    DB-->>Monolith: Дані туру
    deactivate DB
    
    Monolith->>DB: INSERT booking
    activate DB
    DB-->>Monolith: Підтвердження
    deactivate DB
    
    Monolith-->>Gateway: 200 Створено
    deactivate Monolith
    Gateway-->>Client: 200 Створено
    deactivate Gateway
    ---

### 3. Діаграма послідовності: Мікросервіси (Sequence Diagram)

Це ваша третя схема, де додано Booking-сервіс.

```markdown
```mermaid
sequenceDiagram
    autonumber %% Автоматична нумерація кроків
    
    participant Client as Клієнт
    participant Gateway as Gateway
    participant BookingS as Booking
    participant TourS as Tour

    Client->>Gateway: POST /api/bookings (JWT)
    activate Gateway
    
    Gateway->>BookingS: перевірка JWT / пересилає запит
    activate BookingS
    
    BookingS->>BookingS: перевірка JWT (внутрішня)
    
    BookingS->>TourS: REST CheckAvailability (ціна, дати, ok)
    activate TourS
    TourS-->>BookingS: Відповідь ok
    deactivate TourS
    
    BookingS->>BookingS: зберегти бронювання у БД
    
    BookingS-->>Gateway: 201 Створено
    deactivate BookingS
    Gateway-->>Client: 201 Створено
    deactivate Gateway
