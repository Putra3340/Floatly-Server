# Floatly-Server

Floatly-Server is the backend server for the Floatly online music library. It provides a RESTful API for managing, searching, and streaming music content, including albums, artists, and songs. This project is built with ASP.NET Core and is designed to be scalable, secure, and easy to deploy.

---

## Table of Contents

- [Features](#features)
- [Architecture](#architecture)
- [Getting Started](#getting-started)
  - [Prerequisites](#prerequisites)
  - [Installation](#installation)
  - [Configuration](#configuration)
  - [Running the Server](#running-the-server)
- [API Overview](#api-overview)
  - [Authentication](#authentication)
  - [Endpoints](#endpoints)
- [Project Structure](#project-structure)
- [Testing](#testing)
- [Deployment](#deployment)
- [Contributing](#contributing)
- [License](#license)

---

## Features

- User authentication and authorization (JWT-based)
- CRUD operations for albums, artists, and songs
- Music streaming endpoints
- Search functionality for music library
- RESTful API with Swagger/OpenAPI documentation
- Configurable settings via `appsettings.json`
- MVC architecture with Razor views for admin/management
- Logging and error handling
- Caching support for improved performance

---

## Architecture

- **Backend Framework:** ASP.NET Core
- **API:** RESTful, documented with Swagger
- **Views:** Razor Pages (for admin/management)
- **Data Models:** Album, Artist, Song, User, etc.
- **Controllers:** Handle API and view requests
- **Authentication:** JWT tokens
- **Configuration:** `appsettings.json` and environment-specific overrides

---

## Getting Started

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- [Git](https://git-scm.com/)
- (Optional) SQL Server or another supported database if you want persistent storage

### Installation

1. **Clone the repository:**
   ```sh
   git clone https://github.com/Putra3340/Floatly-Server.git
   cd Floatly-Server
   ```

2. **Restore dependencies:**
   ```sh
   dotnet restore
   ```

### Configuration

- Copy `appsettings.json` to `appsettings.Development.json` and adjust settings as needed.
- Set up your database connection string in the configuration file.
- (Optional) Configure authentication secrets and other environment variables.

### Running the Server

```sh
dotnet build
dotnet run
```

The server will start on the port specified in `appsettings.json` (default: `http://localhost:5000`).

---

## API Overview

### Authentication

- **Login:** `POST /auth/login`  
  Returns a JWT token for authenticated requests.

- **Register:** `POST /auth/register`  
  Create a new user account.

### Endpoints

#### Albums

- `GET /albums` — List all albums
- `GET /albums/{id}` — Get album details
- `POST /albums` — Create a new album (admin)
- `PUT /albums/{id}` — Update album (admin)
- `DELETE /albums/{id}` — Delete album (admin)

#### Artists

- `GET /artists` — List all artists
- `GET /artists/{id}` — Get artist details
- `POST /artists` — Create a new artist (admin)
- `PUT /artists/{id}` — Update artist (admin)
- `DELETE /artists/{id}` — Delete artist (admin)

#### Songs

- `GET /music` — List all songs
- `GET /music/{id}` — Get song details
- `POST /music` — Add a new song (admin)
- `PUT /music/{id}` — Update song (admin)
- `DELETE /music/{id}` — Delete song (admin)
- `GET /music/stream/{id}` — Stream a song

#### Search

- `GET /search?query=...` — Search albums, artists, and songs

#### Home

- `GET /` — Home page or API status

> **Note:** All admin endpoints require authentication.

### API Documentation

- Swagger UI is available at `/swagger` when running in development mode.

---

## Project Structure

```
Floatly-Server/
├── Controllers/         # API and MVC controllers
├── Models/              # Data models (Album, Artist, Song, User, etc.)
├── Views/               # Razor views for admin/management
├── Properties/
├── Program.cs           # Main entry point
├── GlobalConfiguration.cs
├── appsettings.json     # Main configuration file
├── README.md
├── LICENSE
└── ...
```

---

## Testing

- Unit and integration tests can be added using xUnit or NUnit.
- To run tests (if present):

  ```sh
  dotnet test
  ```

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