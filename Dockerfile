FROM mcr.microsoft.com/dotnet/sdk:10.0.401 AS build
WORKDIR /source
COPY global.json Directory.Build.props ./
COPY src/backend ./src/backend
RUN dotnet publish src/backend/Game.Api/Game.Api.csproj -c Release -o /app --nologo
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app ./
ENV ASPNETCORE_HTTP_PORTS=8080 ASPNETCORE_ENVIRONMENT=Production
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Game.Api.dll"]
