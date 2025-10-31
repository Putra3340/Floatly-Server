##################################################################################################
# Floatly-Server Dockerfile
#
# 🩵 Linux Prerequisites:
#   - .NET 9+ SDK
#   - Git
#   - LibMan CLI
#       Install via:
#           dotnet tool install -g Microsoft.Web.LibraryManager.Cli
#
#   - SQL Server or SQLite (depending on your configuration)
#
# 💡 Ensure the .NET global tools path (~/.dotnet/tools) is included in your PATH environment variable.
#   For example, in Fish shell (persistent):
#       set -U fish_user_paths $fish_user_paths $HOME/.dotnet/tools
#
# 🧩 Linux Installation (Manual steps if building locally):
#   git clone https://github.com/Putra3340/Floatly-Server.git
#   cd Floatly-Server
#   dotnet restore
#   libman restore
#
# See https://aka.ms/customizecontainer to learn how to customize your debug container
# and how Visual Studio uses this Dockerfile to build your images for faster debugging.
##################################################################################################

# ===== Stage 1: Base (runtime) =====
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# ===== Stage 2: Build =====
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Floatly-Server.csproj", "."]
RUN dotnet restore "./Floatly-Server.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "./Floatly-Server.csproj" -c $BUILD_CONFIGURATION -o /app/build

# ===== Stage 3: Publish =====
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Floatly-Server.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# ===== Stage 4: Final =====
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Floatly-Server.dll"]
