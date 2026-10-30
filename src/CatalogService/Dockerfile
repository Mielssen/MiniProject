FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY src/CatalogService/CatalogService.csproj src/CatalogService/
RUN dotnet restore src/CatalogService/CatalogService.csproj

COPY . .
RUN dotnet publish src/CatalogService/CatalogService.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 5002
EXPOSE 7002

ENTRYPOINT ["dotnet", "CatalogService.dll"]