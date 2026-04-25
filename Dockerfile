FROM node:20-slim AS frontend-build
WORKDIR /app/frontend
COPY frontend/package*.json ./
RUN npm ci --legacy-peer-deps
COPY frontend/ .
RUN npx vite build

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS backend-build
WORKDIR /src
# Restore tylko łańcuch Api (bez Workers i testów z .sln — ich plików jeszcze nie ma w obrazie).
COPY src/Greenhouse.Domain/Greenhouse.Domain.csproj src/Greenhouse.Domain/
COPY src/Greenhouse.Application/Greenhouse.Application.csproj src/Greenhouse.Application/
COPY src/Greenhouse.Infrastructure/Greenhouse.Infrastructure.csproj src/Greenhouse.Infrastructure/
COPY src/Greenhouse.Api/Greenhouse.Api.csproj src/Greenhouse.Api/
RUN dotnet restore src/Greenhouse.Api/Greenhouse.Api.csproj

COPY src/ src/
RUN dotnet publish src/Greenhouse.Api/Greenhouse.Api.csproj -c Release -o /publish/api

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

COPY --from=backend-build /publish/api ./api/
COPY --from=frontend-build /app/frontend/dist ./api/wwwroot/
COPY data/ ./data/
# Unikalny identyfikator przy każdym zbudowaniu obrazu — frontend porównuje z API i przeładowuje stronę po wdrożeniu nowej wersji.
RUN sh -c 'printf "%s-%s" "$(date -u +%Y-%m-%dT%H:%M:%SZ)" "$(tr -dc a-f0-9 </dev/urandom | head -c 8)" > /app/api/deploy-id'

EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000
