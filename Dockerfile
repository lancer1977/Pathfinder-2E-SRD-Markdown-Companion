FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY PathfinderRagChatUi.sln ./
COPY src/PathfinderRagChatUi/PathfinderRagChatUi.csproj src/PathfinderRagChatUi/
COPY tests/PathfinderRagChatUi.Tests/PathfinderRagChatUi.Tests.csproj tests/PathfinderRagChatUi.Tests/
RUN dotnet restore PathfinderRagChatUi.sln

COPY . .
RUN dotnet publish src/PathfinderRagChatUi/PathfinderRagChatUi.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends git ca-certificates \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    Database__Path=/app/data/app.db \
    Corpus__WorkingDirectory=/app/data/source

EXPOSE 8080

COPY --from=build /app/publish ./

RUN mkdir -p /app/data

ENTRYPOINT ["dotnet", "PathfinderRagChatUi.dll"]
