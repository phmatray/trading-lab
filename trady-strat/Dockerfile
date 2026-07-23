# syntax=docker/dockerfile:1.24
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src
COPY *.sln *.slnx Directory.Build.props Directory.Packages.props global.json ./
COPY TradyStrat/*.csproj TradyStrat/
COPY TradyStrat.Tests/*.csproj TradyStrat.Tests/
RUN dotnet restore
COPY . .
RUN dotnet publish TradyStrat -c Release -o /app /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:5180
ENV ASPNETCORE_ENVIRONMENT=Production
ENV Database__Path=/data/tradystrat.db
ENV LOG_DIR=/data/logs
EXPOSE 5180
VOLUME /data
ENTRYPOINT ["dotnet", "TradyStrat.dll"]
