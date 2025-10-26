# Сборка
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY *.sln ./
COPY Jango_Travel/*.csproj Jango_Travel/
RUN dotnet restore

COPY . .
WORKDIR /src/Jango_Travel
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Рантайм
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
COPY --from=build /app/publish .
EXPOSE 8080

# ⬇️ укажи свой DLL
ENTRYPOINT ["dotnet", "Jango_Travel.dll"]
