FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Platform.Shared/Platform.Shared.csproj Platform.Shared/
COPY SwearJar.Api/SwearJar.Api.csproj SwearJar.Api/
RUN dotnet restore SwearJar.Api/SwearJar.Api.csproj
COPY Platform.Shared/ Platform.Shared/
COPY SwearJar.Api/ SwearJar.Api/
RUN dotnet publish SwearJar.Api/SwearJar.Api.csproj -c Release -o /app/publish --no-restore

FROM base AS final
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SwearJar.Api.dll"]
