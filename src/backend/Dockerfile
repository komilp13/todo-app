FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

# Copy solution and project files
COPY ["TodoApp.sln", "."]
COPY ["TodoApp.Api/TodoApp.Api.csproj", "TodoApp.Api/"]
COPY ["TodoApp.Application/TodoApp.Application.csproj", "TodoApp.Application/"]
COPY ["TodoApp.Domain/TodoApp.Domain.csproj", "TodoApp.Domain/"]
COPY ["TodoApp.Infrastructure/TodoApp.Infrastructure.csproj", "TodoApp.Infrastructure/"]
COPY ["TodoApp.UnitTests/TodoApp.UnitTests.csproj", "TodoApp.UnitTests/"]
COPY ["TodoApp.IntegrationTests/TodoApp.IntegrationTests.csproj", "TodoApp.IntegrationTests/"]

# Restore dependencies
RUN dotnet restore "TodoApp.sln"

# Copy all source files
COPY . .

# Build
RUN dotnet build -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "TodoApp.Api/TodoApp.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=publish /app/publish .

EXPOSE 5000

ENTRYPOINT ["dotnet", "TodoApp.Api.dll"]
