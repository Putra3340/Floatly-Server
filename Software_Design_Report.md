# Software Design Report: Floatly Server

## 1. Executive Summary
**Floatly Server** is a high-performance media streaming and management server built on **ASP.NET Core (.NET 10)**. It is designed to serve as a centralized backend for music clients, offering features such as local library management, YouTube audio/video integration, real-time lyrics synchronization, and a premium subscription model handled via **Midtrans**.

## 2. Technology Stack

### Backend Core
*   **Framework**: ASP.NET Core (.NET 10 Preview)
*   **Language**: C#
*   **Database ORM**: Entity Framework Core 9.0.9
*   **Databases**:
    *   **Development**: Microsoft SQL Server
    *   **Production**: SQLite (`database.db`)

### Key Libraries & Services
*   **Media Processing**: `Xabe.FFmpeg` (Video/Audio muxing), `TagLibSharp` (Metadata extraction).
*   **External Integration**: `YoutubeExplode` (YouTube streaming/downloading), `Sindika.AspNet.Midtrans` (Payment Gateway).
*   **Real-time Communication**: SignalR (`StatusHub`).
*   **API Documentation**: Swagger / OpenAPI.

## 3. System Architecture

The system follows a typical **Model-View-Controller (MVC)** pattern, capable of serving both RESTful APIs (for clients) and Server-Side Rendered Views (for the Admin Dashboard).

```mermaid
graph TD
    Client[Mobile/Web Client] -->|REST API / HTTPS| LoadBalancer[Reverse Proxy/IIS]
    LoadBalancer --> WebApp[Floatly Server (ASP.NET Core)]
    
    subgraph "Floatly Server Internal"
        WebApp --> Controllers[Controllers Layer]
        Controllers --> Services[Services Layer]
        Services --> FFmpeg[FFmpeg Wrapper]
        Services --> YT[YoutubeExplode]
        Services --> DB[EF Core Context]
    end
    
    DB --> SQL[SQL Server / SQLite]
    YT --> YouTube[YouTube API]
    Controllers --> Midtrans[Midtrans Payment Gateway]

    note right of Controllers
        LikeController is deprecated.
        Likes are handled via
        Special Playlists in PlaylistController.
    end
```

## 4. Database Schema Design

The database is normalized to handle standard music metadata (Artists, Albums, Songs) and specific YouTube integrations. Use of a `SongCounter` table allows for tracking analytics (Plays/Likes) separately from the immutable song data.

```mermaid
erDiagram
    Users ||--o{ Playlists : "Creates"
    Users ||--o{ Transaction : "Perform"
    
    Artists ||--o{ Albums : "Has"
    Albums ||--o{ Songs : "Contains"
    
    Songs ||--o{ SongCounter : "Tracked By"
    Songs ||--o{ PlaylistSongs : "Included In"
    
    YoutubeSongs ||--o{ SongCounter : "Tracked By"
    YoutubeSongs ||--o{ YoutubeLyrics : "Has"
    YoutubeSongs ||--o{ PlaylistSongs : "Included In"

    Playlists ||--o{ PlaylistSongs : "Contains"

    Users {
        int Id PK
        string Username
        string Email
        datetime PremiumExpired
        datetime CreatedAt
    }

    Songs {
        int Id PK
        string Title
        string MusicFilePath
        string LyricsFilePath
        int AlbumId FK
    }

    YoutubeSongs {
        int Id PK
        string UrlId
        string Title
        string Music
        string Video
        string Lyrics
    }

    Transaction {
        int Id PK
        string OrderId
        decimal Amount
        int PaymentStatus
    }
```

## 5. Core Workflows & Logic

### 5.1. Song Upload Process
This flow describes how an Admin uploads a new song file, and how metadata/analytics are initialized.

```mermaid
sequenceDiagram
    participant Admin as Admin User
    participant SC as SongController
    participant FH as FileHelper
    participant DB as Database
    participant S3 as Local Storage

    Admin->>SC: POST /Song/Upload (Multipart Form)
    activate SC
    
    SC->>FH: Save Music File
    FH->>S3: Write .mp3
    S3-->>FH: Path
    FH-->>SC: Returns MusicFilePath

    SC->>FH: Save Cover/Lyrics/Banner (Parallel)
    FH-->>SC: Return File Paths

    SC->>DB: Create Song Record
    SC->>DB: Initialize SongCounter (0 plays)
    SC->>DB: SaveChangesAsync()
    
    SC-->>Admin: 200 OK
    deactivate SC
```

### 5.2. YouTube Download & Integration
This complex flow involves fetching metadata, downloading streams, and parsing closed captions into SRT format.

```mermaid
sequenceDiagram
    participant Client
    participant YS as YoutubeService
    participant YT as YouTube
    participant FF as FFmpeg
    participant DB as Database

    Client->>YS: DownloadAndSaveAsync(url)
    activate YS
    
    YS->>YT: Get Video Manifest
    YT-->>YS: Stream Info (Audio/Video)
    
    YS->>YT: Download Audio Stream
    YS->>YT: Download Video Stream
    
    opt Video Quality Upgrade
        YS->>FF: Mux Audio + Video (HD)
    end
    
    YS->>YT: Get Closed Captions (Manifest)
    loop Each Track
        YS->>YT: Get Caption Track
        YS->>YS: Parse XML/JSON to .SRT Format
        YS->>DB: Save YoutubeLyrics
    end

    YS->>DB: Save YoutubeSongs (Metadata)
    YS->>DB: Save SongCounter
    
    deactivate YS
```

### 5.3. Subscription Payment Flow (Midtrans)
Handles the transaction lifecycle from checking out to webhook notification.

```mermaid
sequenceDiagram
    participant User
    participant SubC as SubsController
    participant MT as Midtrans Gateway
    participant DB as Database

    User->>SubC: POST /subs/pay (username)
    activate SubC
    SubC->>DB: Find User
    SubC->>DB: Create Transaction (Pending)
    
    SubC->>MT: Create Snap Token
    MT-->>SubC: Token
    SubC-->>User: Token (Launch Payment UI)
    deactivate SubC

    User->>MT: Complete Payment
    
    MT->>SubC: POST /midtrans/notification (Webhook)
    activate SubC
    SubC->>DB: Find Transaction by OrderId
    
    alt Status == Settlement
        SubC->>DB: Update Transaction Status
        SubC->>DB: Update User.PremiumExpired (+30 Days)
    else Status == Pending/Cancel
        SubC->>DB: Update Transaction Status
    end
    
    SubC->>MT: 200 OK
    deactivate SubC
```

### 5.4. Client "Like Song" Flow
"Liking" a song is implemented as adding it to a user's "Special Playlist" (e.g. "Liked Songs").

```mermaid
sequenceDiagram
    participant Client
    participant PC as PlaylistController
    participant DB as Database

    Client->>PC: POST /api/playlist/addlikesong (token, songId)
    activate PC
    PC->>DB: Validate User Token
    PC->>DB: Find Playlist (SpecialPlaylist == true)
    
    alt Song is Valid
        PC->>DB: Add to PlaylistSongs
        PC->>DB: SaveChangesAsync()
        PC-->>Client: 200 OK
    else Song Exists in Playlist
        PC-->>Client: 409 Conflict
    end
    deactivate PC
```

## 6. API Specification Highlights

### Authentication (`/auth`)
*   `POST /login`: Admin login using hardcoded credentials in `GlobalConfiguration`.
*   `GET /logout`: Clears authentication cookie.

### Songs (`/Song`)
*   `POST /Upload`: Admin upload.
*   `GET /DashboardV2`: Main admin stats view.
*   `GET /GetArtist`: Public API for artist list (pagination supported).
*   `GET /GetAlbumSong`: Fetch songs for specific album.

### YouTube (`/Song/GetYtSong`)
*   `GET /GetYtSong`: List downloaded YouTube songs.
*   `GET /GetYtLibrarySearch`: Search within downloaded YouTube songs.

### Subscriptions (`/subs`)
*   `POST /pay`: Initiate payment.
*   `POST /midtrans/notification`: Webhook handler.

### Client API (`/api/playlist`)
*   `GET /`: Get all playlists (supports "token" query param).
*   `GET /getsongs`: Get songs in a playlist.
*   `POST /create`: Create new playlist.
*   `POST /addlikesong`: Add to "Liked Songs" (Special Playlist).

## 7. Folder Structure Implementation
*   `Controllers/`: API Endpoints.
*   `Models/`: Database Entities.
*   `Service/`: Business Logic (`YoutubeService`, `AudioService`).
*   `Utils/`: Helpers (`FileHelper`, `DirectoryScanner`).
*   `wwwroot/`: Static assets (uploaded files are stored here under `uploads/`).
