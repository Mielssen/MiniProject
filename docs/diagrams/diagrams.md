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
