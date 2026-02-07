# ===== BUILD STAGE =====
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY server/Offroad.Api/Offroad.Api.csproj Offroad.Api/
COPY server/Routing.Application/Routing.Application.csproj Routing.Application/
COPY server/Offroad.Domain/Routing.Domain.csproj Offroad.Domain/
COPY server/Offroad.Infrastructure/Routing.Infrastructure.csproj Offroad.Infrastructure/
RUN dotnet restore Offroad.Api/Offroad.Api.csproj

COPY server/. .

RUN dotnet publish Offroad.Api/Offroad.Api.csproj -c Release -o /app/publish


# ===== RUNTIME STAGE =====
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Offroad.Api.dll"]
