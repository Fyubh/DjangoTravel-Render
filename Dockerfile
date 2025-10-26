# ---------- Runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# ---------- Build ----------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# 1) Копируем .csproj (ускоряет кэш)
COPY Jango_Travel.csproj ./
RUN dotnet restore Jango_Travel.csproj

# 2) Копируем остальное
COPY . .

# 3) Публикуем
RUN dotnet publish Jango_Travel.csproj -c Release -o /app/publish /p:UseAppHost=false

# ---------- Final image ----------
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Jango_Travel.dll"]
