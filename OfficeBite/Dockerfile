# Use official .NET 9 SDK image to build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files first for efficient caching
COPY ["OfficeBite.API/OfficeBite.API.csproj", "OfficeBite.API/"]
COPY ["OfficeBite.BLL/OfficeBite.BLL.csproj", "OfficeBite.BLL/"]
COPY ["OfficeBite.DAL/OfficeBite.DAL.csproj", "OfficeBite.DAL/"]
COPY ["OfficeBite.Shared/OfficeBite.Shared.csproj", "OfficeBite.Shared/"]

# Restore dependencies
RUN dotnet restore "OfficeBite.API/OfficeBite.API.csproj"

# Copy the rest of the source code
COPY . .

# Build and publish release output
WORKDIR "/src/OfficeBite.API"
RUN dotnet publish "OfficeBite.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime image (.NET 9 ASP.NET Core)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Render exposes PORT env var (defaults to 8080 or $PORT)
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "OfficeBite.API.dll"]
