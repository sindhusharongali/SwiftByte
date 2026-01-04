FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["SwiftBite.Contracts/SwiftBite.Contracts.csproj", "SwiftBite.Contracts/"]
COPY ["PaymentService/PaymentService.csproj", "PaymentService/"]

# Restore dependencies
RUN dotnet restore "PaymentService/PaymentService.csproj"

# Copy source code
COPY ["SwiftBite.Contracts/", "SwiftBite.Contracts/"]
COPY ["PaymentService/", "PaymentService/"]

# Build
WORKDIR "/src/PaymentService"
RUN dotnet build "PaymentService.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "PaymentService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=publish /app/publish .

EXPOSE 80 443

ENTRYPOINT ["dotnet", "PaymentService.dll"]
