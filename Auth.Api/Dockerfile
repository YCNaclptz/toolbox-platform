FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Platform.Shared/Platform.Shared.csproj Platform.Shared/
COPY Auth.Api/Auth.Api.csproj Auth.Api/
RUN dotnet restore Auth.Api/Auth.Api.csproj
COPY Platform.Shared/ Platform.Shared/
COPY Auth.Api/ Auth.Api/
RUN dotnet publish Auth.Api/Auth.Api.csproj -c Release -o /app/publish --no-restore

FROM base AS final
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Auth.Api.dll"]
