FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY FlightApp/FlightKS.csproj FlightApp/
RUN dotnet restore FlightApp/FlightKS.csproj
COPY . .
WORKDIR /src/FlightApp
RUN dotnet publish -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:5194 \
    ASPNETCORE_HTTP_PORTS=5194

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && groupadd --system flightks \
    && useradd --system --gid flightks --home-dir /app --shell /usr/sbin/nologin flightks \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build --chown=flightks:flightks /app/publish .
RUN mkdir -p /app/dp-keys /app/wwwroot/uploads \
    && chown -R flightks:flightks /app

USER flightks
EXPOSE 5194
ENTRYPOINT ["dotnet", "FlightKS.dll"]
