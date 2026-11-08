# Збірка з КОРЕНЯ репозиторію:
#   docker build -f Platform.ApiGateway/Dockerfile -t wearcast-gateway .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Platform.ServiceDefaults/ Platform.ServiceDefaults/
COPY Platform.ApiGateway/ Platform.ApiGateway/
RUN dotnet restore Platform.ApiGateway/Platform.ApiGateway.csproj
RUN dotnet publish Platform.ApiGateway/Platform.ApiGateway.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Platform.ApiGateway.dll"]
