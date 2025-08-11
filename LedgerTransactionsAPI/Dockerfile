# Dockerfile dentro de la carpeta del proyecto: LedgerTransactionsAPI/
# Contexto de build en docker-compose: ./LedgerTransactionsAPI

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia solo el csproj primero para aprovechar cache
COPY *.csproj ./
RUN dotnet restore

# Ahora copia el resto del proyecto
COPY . .
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "LedgerTransactionsAPI.dll"]
