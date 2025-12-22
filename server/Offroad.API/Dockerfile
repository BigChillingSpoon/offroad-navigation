# ===== BUILD STAGE =====
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Offroad.Api/Offroad.Api.csproj Offroad.Api/
COPY Offroad.Application/Offroad.Application.csproj Offroad.Application/
COPY Offroad.Domain/Offroad.Domain.csproj Offroad.Domain/
COPY Offroad.Infrastructure/Offroad.Infrastructure.csproj Offroad.Infrastructure/

RUN dotnet restore Offroad.Api/Offroad.Api.csproj

COPY . .
WORKDIR /src/Offroad.Api
RUN dotnet publish -c Release -o /app/publish

# ===== RUNTIME STAGE =====
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Offroad.Api.dll"]
