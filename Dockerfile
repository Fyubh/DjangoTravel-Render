# Сборка
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# у нас .csproj в корне репозитория
COPY *.csproj ./
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Рантайм
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
COPY --from=build /app/publish .
EXPOSE 8080
# имя DLL = имя твоего .csproj
ENTRYPOINT ["dotnet", "Jango_Travel.dll"]