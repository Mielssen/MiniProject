FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY src/AuthService/AuthService.csproj src/AuthService/
RUN dotnet restore src/AuthService/AuthService.csproj

COPY . .
RUN dotnet publish src/AuthService/AuthService.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

COPY --from=build /app/publish .
EXPOSE 5001

ENV ASPNETCORE_URLS=http://+:5001

ENTRYPOINT ["dotnet", "AuthService.dll"]