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
  - [Windows](#windows)
    - [Prerequisites](#windows-prerequisites)
    - [Installation](#windows-installation)
    - [Configuration](#windows-configuration)
    - [Running the Server](#windows-running-the-server)
  - [Linux & Other Platforms](#linux--other-platforms)
    - [Prerequisites](#linux-prerequisites)
    - [Installation](#linux-installation)
    - [Configuration](#linux-configuration)
    - [Running the Server](#linux-running-the-server)
- [API Overview](#api-overview)
  - [Desktop Authentication](#desktop-authentication)
  - [Endpoints](#endpoints)
- [Project Structure](#project-structure)
- [Testing](#testing)
- [Deployment](#deployment)
- [Contributing](#contributing)
- [License](#license)

---

## Features

- Full CRUD support for albums, artists, and songs through a web interface  
- Powerful search across the music library  
- Music streaming endpoints for seamless playback  
- Custom user authentication and authorization system  
- Automatic login support for the desktop client  
- RESTful API with integrated Swagger/OpenAPI documentation  
- MVC architecture with Razor views for administration and management  
- Configurable settings via `.env` file  
- SMTP integration for email notifications and account registration  
- Built-in logging and structured error handling  


---

## Architecture

- **Backend Framework:** ASP.NET Core
- **API:** RESTful, documented with Swagger, and OpenAPI
- **Views:** Razor Pages (for admin/management)
- **Data Models:** Album, Artist, Song, User, etc.
- **Controllers:** Handle API and view requests
- **Authentication:** Custom desktop client authentication
- **Configuration:** `.env` and environment-specific overrides

---

## Getting Started

### Windows
#### Windows Prerequisites

- Microsoft Visual Studio 2022 or later (with .NET workload).
- SQL Server or SQLITE.
#### Windows Installation
1. **Clone the repository on Visual Studio:**
   `https://github.com/Putra3340/Floatly-Server.git`.
2. **Restore dependencies:**
   - Open the solution in Visual Studio.
   - Right-click the solution in Solution Explorer and select `Restore NuGet Packages`.
   - Right-click the solution in Solution Explorer and select `Restore Client-Side Libraries`.
#### Windows Configuration
- Copy `.env.example` to `.env` and adjust settings as needed.
- Set up your database connection string in the configuration file.
- Configure authentication secrets and other environment variables.
#### Windows Running the Server
- Press `F5` in Visual Studio to build and run the server.
- The server will start on the port specified in `Properties/launchSettings.json` (default: `http://localhost:5178`).

### Linux & Other Platforms
#### Linux Prerequisites

- [.NET 9+ SDK](https://dotnet.microsoft.com/download)
- [Git](https://git-scm.com/)
- [LibMan CLI](https://learn.microsoft.com/aspnet/core/client-side/libman/libman-cli)  
  Install via:
  ```bash
  dotnet tool install -g Microsoft.Web.LibraryManager.Cli
- SQL Server or SQLITE

Ensure the .NET global tools path `~/.dotnet/tools` is included in your PATH environment variable.
For example, in Fish shell (persistent):
```bash
set -U fish_user_paths $fish_user_paths $HOME/.dotnet/tools
```

#### Linux Installation

1. **Clone the repository:**
   ```sh
   git clone https://github.com/Putra3340/Floatly-Server.git
   cd Floatly-Server
   ```

2. **Restore dependencies:**
   ```sh
   dotnet restore
   libman restore
   ```

#### Linux Configuration

- Copy `.env.example` to `.env` and adjust settings as needed.
- Set up your database connection string in the configuration file.
- Configure authentication secrets and other environment variables.

#### Linux Running the Server

```sh
dotnet build
dotnet run
```

The server will start on the port specified in `Properties/launchSettings.json` (default: `http://localhost:5178`).

---

## API Overview

### Desktop Authentication

- **Login:** `POST /auth/desktop/login`  
  Authenticate a user and return a custom token for authorized requests.  

- **AutoLogin:** `POST /auth/desktop/autologin`  
  Authenticate a user automatically using a stored token.   

- **Register:** `POST /auth/desktop/register`  
  Create a new user account.  

- **Request Email Verification:** `POST /auth/desktop/verify-email`  
  Send a verification email using SMTP.  

- **Confirm Email Verification:** `GET /auth/desktop/verify-token`  
  Validate the verification token and confirm the user’s email address.  


### Endpoints

#### Library V1

- `GET /api/library/v1/{id}` — Get library item by ID  
- `GET /api/library/v1` — Get all library items  

#### Library V2

- `GET /api/library/v2` — Get all library items
- `GET /api/library/v3/{id}` — Get library item by ID  
- `GET /api/library/v2/artist/{id}` — Get artist details  
- `GET /api/library/v2/album/{id}` — Get album details  

#### Likes

- `POST /api/likes` — Get user liked song list
- `POST /api/likesong` — Like a song  
- `POST /api/unlikesong` — Unlike a song  

#### Playlists

- `POST /api/playlist` — Get user playlists  
- `POST /api/createplaylist` — Create a new playlist  
- `POST /api/deleteplaylist` — Delete an existing playlist  
- `POST /api/addplaylistsong` — Add a song to a playlist  
- `POST /api/removeplaylistsong` — Remove a song from a playlist  
- `POST /api/editplaylist` — Edit playlist details  
- `POST /api/getplaylistsongs` — Get all songs in a playlist  


> **Note:** All admin endpoints require web authentication.

### API Documentation

- Swagger UI is available at `/swagger` when running in development mode.

---

## Project Structure
```
Floatly-Server/
├── Controllers/             # API and MVC controllers
│   └── LibraryController/   # API for searching the song library
│   └── ClientController/    # API for client-side song interactions
├── Models/                  # Database context & data models (Album, Artist, Song, User, etc.)
├── Services/                # Third-party services (e.g., email)
├── Utils/                   # Utility/helper classes
├── Views/                   # Razor views for admin/management
├── Properties/              # Project metadata & launch settings
├── Program.cs               # Main entry point
├── GlobalConfiguration.cs   # Reads configuration from `.env`
├── wwwroot/                 # Public web root
│   └── uploads/             # Uploaded music, lyrics, covers, banners
├── .env                     # Main configuration file
├── README.md
├── LICENSE
└── ...
```

---

## Testing

Soon

---

## Deployment

- **Docker:** Add a `Dockerfile` for containerized deployment.
- **Cloud:** Deploy to Azure, AWS, or any cloud provider supporting .NET.
- **Reverse Proxy:** Use Nginx or Apache for HTTPS and load balancing.

---

## Contributing

Contributions are welcome! Please open issues or submit pull requests for new features, bug fixes, or documentation improvements.

---

## License

This project is licensed under the [Apache 2.0 License](LICENSE).

---

## Contact

For questions or support, open an issue on the repository