FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["SwiftBite.Contracts/SwiftBite.Contracts.csproj", "SwiftBite.Contracts/"]
COPY ["OrderService/OrderService.csproj", "OrderService/"]

# Restore dependencies
RUN dotnet restore "OrderService/OrderService.csproj"

# Copy source code
COPY ["SwiftBite.Contracts/", "SwiftBite.Contracts/"]
COPY ["OrderService/", "OrderService/"]

# Build
WORKDIR "/src/OrderService"
RUN dotnet build "OrderService.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "OrderService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=publish /app/publish .

EXPOSE 80 443

ENTRYPOINT ["dotnet", "OrderService.dll"]
