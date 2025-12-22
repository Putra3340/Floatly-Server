# Floatly-Server

Floatly-Server is the backend server for the Floatly online music library.  
It provides a RESTful API for managing, searching, and streaming music content, including albums, artists, and songs.  
Built with ASP.NET Core, the server is designed to be scalable, secure, and easy to deploy.  

This project is intended for **self-hosting**, giving users full control over their own music library.  
You can run it privately on your own machine or server, or make it publicly accessible for shared access.

---

## Table of Contents

- [Features](#features)
- [Architecture](#architecture)
- [Getting Started](#getting-started)
- [API Overview](#api-overview)
  - [Server Information](#server-information)
  - [Desktop Authentication](#desktop-authentication)
  - [Client Library (V3)](#client-library-v3)
  - [Legacy Library (V1 & V2)](#legacy-library-v1--v2)
  - [User Library Management](#user-library-management)
- [Project Structure](#project-structure)
- [Controllers & Services](#controllers--services)
- [License](#license)

---

## Features

- **Advanced Music Management**:
  - Full CRUD capabilities for Songs, Albums, and Artists via MVC Dashboard.
  - **Upload & Edit**: Support for uploading new tracks and editing metadata.
  - **Cleanup & Refresh**: Tools to clean up database discrepancies and refresh disk content.
  - **Compression**: Utility to compress all songs for optimized streaming.
- **YouTube Music Integration**:
  - Search and play content directly via YouTube integration (Library V3).
  - Fetch lyrics automatically.
- **Powerful Search**:
  - Deep search capabilities across local library and external sources.
- **Robust Authentication**:
  - **Desktop Auth**: Token-based authentication for desktop clients (Auto-login, Registration, Email Verification).
  - **Web Auth**: Standard cookie-based authentication for the admin dashboard.
- **Server Monitoring**:
  - `api/info` endpoint for real-time server statistics (Uptime, Song Count, Server Time).
- **Architecture**:
  - Built on **ASP.NET Core** with **Ocelot**-ready patterns.
  - **MVC** for Administration Views.
  - **REST API** for Client Applications.
  - **Swagger/OpenAPI** documentation support.

---

## Architecture

- **Backend Framework:** ASP.NET Core 8.0/9.0
- **Database:** Entity Framework Core (SQL Server / SQLite support)
- **API:** RESTful, Versioned (V1, V2, V3)
- **Frontend (Admin):** Razor Pages (MVC)
- **Authentication:** Custom Token + Session Cookies
- **External Services:** 
  - YouTube Explode (for V3 integration)
  - MailKit (SMTP for emails)

---

## Getting Started

### Prerequisites

- **.NET SDK** (8.0 or newer)
- **Database**: SQL Server or SQLite
- **Visual Studio 2022** or **VS Code**

### Installation

1. **Clone the repository:**
   ```bash
   git clone https://github.com/Putra3340/Floatly-Server.git
   cd Floatly-Server
   ```

2. **Restore dependencies:**
   ```bash
   dotnet restore
   ```
   *Note: If using Visual Studio, right-click the solution to restore NuGet packages.*

### Configuration

1. Copy `.env.example` to `.env`.
2. Configure your database connection string and SMTP settings in `.env`.

### Running the Server

```bash
dotnet run
```
The server will start at `http://localhost:5178` (default).

---

## API Overview

### Server Information

- **GET** `/api/info`
  - Returns server status, uptime, version, and library statistics (Total Songs, Artists, Albums).

### Desktop Authentication

Managed by `Controllers/ClientController/AuthController.cs`.

- **POST** `/auth/desktop/login`
  - Authenticates user credentials. Returns a session token.
- **POST** `/auth/desktop/autologin`
  - Logs in using a valid saved token.
- **POST** `/auth/desktop/register`
  - Registers a new user account.
- **POST** `/auth/desktop/verify-email`
  - Triggers a verification email to the user.
- **GET** `/auth/desktop/verify-token`
  - Validates the email verification token.

### Client Library (V3)

The latest library API with YouTube integration. Managed by `Controllers/LibraryController/V3.cs`.

- **GET** `/api/library/v3/search`
  - Search for songs (Local + YouTube).
- **GET** `/api/library/v3/play/{id}`
  - Stream a song by ID.
- **GET** `/api/library/v3/lyrics/{urlId}`
  - Fetch lyrics for a specific track.

### Legacy Library (V1 & V2)

Managed by `Controllers/LibraryController/V1.cs` and `V2.cs`.

#### V2
- **GET** `/api/library/v2` - List all songs.
- **GET** `/api/library/v2/search` - Search local library.
- **GET** `/api/library/v2/{id}` - Get song details.
- **GET** `/api/library/v2/artist/{id}` - Get artist details and tracks.
- **GET** `/api/library/v2/album/{id}` - Get album details and tracks.

#### V1
- **GET** `/api/library/v1` - List all songs.
- **GET** `/api/library/v1/{id}` - Get song details.

### User Library Management

Managed by `Controllers/ClientController`.

#### Playlists (`PlaylistController`)
- **POST** `/api/playlist` - List user playlists.
- **POST** `/api/createplaylist` - Create a new playlist.
- **POST** `/api/deleteplaylist` - Delete a playlist.
- **POST** `/api/addplaylistsong` - Add song to playlist.
- **POST** `/api/removeplaylistsong` - Remove song from playlist.
- **POST** `/api/editplaylist` - Update playlist metadata.
- **POST** `/api/getplaylistsongs` - Retrieve playlist tracks.

#### Likes (`LikeController`)
- **POST** `/api/likes` - Get list of liked songs.
- **POST** `/api/likesong` - Like a song.
- **POST** `/api/unlikesong` - Unlike a song.

---

## Controllers & Services

Here is a detailed breakdown of the internal controller structure:

| Controller | Class Name | Responsibility |
| :--- | :--- | :--- |
| **Song** | `SongController` | Main MVC Dashboard. Handles Uploads, Edits, Deletions, and Library cleanup. |
| **Album** | `AlbumController` | MVC management for Albums. |
| **Artist** | `ArtistController` | MVC management for Artists. |
| **Auth** (Web) | `AuthController` | Web-based login/logout for the admin panel. |
| **Library V3** | `LibraryV3Controller` | Latest client API with YouTube and external source support. |
| **Library V2** | `LibraryV2Controller` | Optimized local library API. |
| **Client Auth** | `ClientController.AuthController` | Dedicated stateless authentication for Desktop/Mobile clients. |
| **Client API** | `ClientController.ApiController` | General client info and server stats. |
| **Playlist** | `ClientController.PlaylistController` | User-created playlist management. |
| **Like** | `ClientController.LikeController` | User "Liked Songs" management. |

---

## Project Structure

```text
Floatly-Server/
├── Controllers/
│   ├── ClientController/          # Client-facing API Endpoints
│   │   ├── ApiController.cs       # Server Stats & Info
│   │   ├── AuthController.cs      # Desktop/Mobile Authentication mechanism
│   │   ├── LikeController.cs      # Like/Unlike logic
│   │   └── PlaylistController.cs  # Playlist CRUD
│   ├── LibraryController/         # Library Data Providers
│   │   ├── V1.cs                  # Legacy API
│   │   ├── V2.cs                  # Standard Local API
│   │   └── V3.cs                  # Modern API (YouTube + Local)
│   ├── AlbumController.cs         # Admin: Album Management
│   ├── ArtistController.cs        # Admin: Artist Management
│   ├── AuthController.cs          # Admin: Web Login
│   ├── HomeController.cs          # Dashboard Entry Point
│   └── SongController.cs          # Admin: Main Song Logic (Upload, Edit, etc.)
├── Models/                        # Database Context & Entities
│   ├── ApiClient/                 # Models specific to API responses
│   ├── Album.cs
│   ├── Artist.cs
│   ├── Song.cs
│   ├── User.cs
│   └── FloatlyContext.cs          # EF Core Context
├── Services/                      # Business Logic & External Services
├── Utils/                         # Helpers (Compression, TagLib, etc.)
├── Views/                         # Razor Views (Admin Interface)
├── wwwroot/                       # Static Files
│   └── uploads/                   # Media Storage (Covers, Songs)
├── .env.example                   # Environment Template
├── appsettings.json               # Core Configuration
├── GlobalConfiguration.cs         # Config Loader
├── Program.cs                     # App Entry Point & DI Container
└── README.md
```

---

## License

This project is licensed under the **Apache 2.0 License**. See the `LICENSE` file for details.