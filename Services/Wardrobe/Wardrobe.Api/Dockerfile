# Збірка з КОРЕНЯ репозиторію:
#   docker build -f Services/Wardrobe/Wardrobe.Api/Dockerfile -t wardrobe-api .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Services/Wardrobe/ Services/Wardrobe/
RUN dotnet restore Services/Wardrobe/Wardrobe.Api/Wardrobe.Api.csproj
RUN dotnet publish Services/Wardrobe/Wardrobe.Api/Wardrobe.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Wardrobe.Api.dll"]
