# ================================
# Stage 1: Build
# ================================
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

# Copy solution và csproj (case-sensitive!)
COPY ECommerceSystem.sln .
COPY ECommerceSystem.API/EcommerceSystem.API.csproj ./ECommerceSystem.Api/
COPY ECommerceSystem.Shared/ECommerceSystem.Shared.csproj ./ECommerceSystem.Shared/
COPY ECommerceSystem.GUI/ECommerceSystem.GUI.csproj ./ECommerceSystem.GUI/

# Restore packages
RUN dotnet restore ECommerceSystem.sln --disable-parallel

# Copy toàn bộ source code
COPY . .

# Build & publish từng project để tránh conflict
RUN dotnet publish ECommerceSystem.API/EcommerceSystem.API.csproj -c Release -o /app/API
RUN dotnet publish ECommerceSystem.Shared/ECommerceSystem.Shared.csproj -c Release -o /app/Shared
RUN dotnet publish ECommerceSystem.GUI/ECommerceSystem.GUI.csproj -c Release -o /app/GUI

# ================================
# Stage 2: Runtime
# ================================
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Copy publish output từ stage build
COPY --from=build /app/API ./API
COPY --from=build /app/Shared ./Shared
COPY --from=build /app/GUI ./GUI

# Expose cổng để chạy ứng dụng
EXPOSE 8080

# Copy file cấu hình
COPY ECommerceSystem.API/appsettings.json ./API/

# Chạy ứng dụng API
ENTRYPOINT ["dotnet", "API/EcommerceSystem.API.dll"]