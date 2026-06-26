# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY invoice-service.sln ./
COPY Directory.Build.props ./
COPY Application/Application.csproj Application/
COPY Contracts/Contracts.csproj Contracts/
COPY DataAccess/DataAccess.csproj DataAccess/
COPY Domain/Domain.csproj Domain/
COPY DTO/DTO.csproj DTO/
COPY Infrastructure/Infrastructure.csproj Infrastructure/
COPY Web/Web.csproj Web/
RUN dotnet restore Web/Web.csproj

COPY . .
RUN dotnet publish Web/Web.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV APP_PORT=8080 \
    ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Web.dll"]
