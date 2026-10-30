FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/TravelManager/TravelManager.csproj src/TravelManager/
RUN dotnet restore src/TravelManager/TravelManager.csproj

COPY . .
RUN dotnet publish src/TravelManager/TravelManager.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 5272

ENV ASPNETCORE_URLS=http://+:5272

ENTRYPOINT ["dotnet", "TravelManager.dll"]