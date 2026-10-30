FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY src/BookingService/BookingService.csproj src/BookingService/
RUN dotnet restore src/BookingService/BookingService.csproj

COPY . .
RUN dotnet publish src/BookingService/BookingService.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 5100

ENV ASPNETCORE_URLS=http://+:5100

ENTRYPOINT ["dotnet", "BookingService.dll"]