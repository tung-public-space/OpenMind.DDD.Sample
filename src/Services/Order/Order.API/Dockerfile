FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files
COPY ["src/Services/Order/Order.API/Order.API.csproj", "src/Services/Order/Order.API/"]
COPY ["src/Services/Order/Order.Application/Order.Application.csproj", "src/Services/Order/Order.Application/"]
COPY ["src/Services/Order/Order.Domain/Order.Domain.csproj", "src/Services/Order/Order.Domain/"]
COPY ["src/Services/Order/Order.Infrastructure/Order.Infrastructure.csproj", "src/Services/Order/Order.Infrastructure/"]
COPY ["src/Services/Order/Order.IntegrationEvents/Order.IntegrationEvents.csproj", "src/Services/Order/Order.IntegrationEvents/"]
COPY ["src/Services/Payment/Payment.IntegrationEvents/Payment.IntegrationEvents.csproj", "src/Services/Payment/Payment.IntegrationEvents/"]
COPY ["src/BuildingBlocks/DDD.BuildingBlocks.Domain/DDD.BuildingBlocks.Domain.csproj", "src/BuildingBlocks/DDD.BuildingBlocks.Domain/"]
COPY ["src/BuildingBlocks/DDD.BuildingBlocks.Integration/DDD.BuildingBlocks.Integration.csproj", "src/BuildingBlocks/DDD.BuildingBlocks.Integration/"]

# Restore dependencies
RUN dotnet restore "src/Services/Order/Order.API/Order.API.csproj"

# Copy everything else
COPY . .

# Build
WORKDIR "/src/src/Services/Order/Order.API"
RUN dotnet build "Order.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Order.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Order.API.dll"]
