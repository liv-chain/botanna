
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["AveManiaBot.csproj", "./"]
RUN dotnet restore "AveManiaBot.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet build "AveManiaBot.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "AveManiaBot.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY ave_mania.db /app/ave_mania.db
# Nota: Nessuna modifica ai permessi necessaria sui container Windows.
ENTRYPOINT ["dotnet", "AveManiaBot.dll"]