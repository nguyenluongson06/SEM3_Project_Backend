FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
# USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["SEM3_Project_Backend/SEM3_Project_Backend.csproj", "SEM3_Project_Backend/"]
RUN dotnet restore "SEM3_Project_Backend/SEM3_Project_Backend.csproj"
COPY . .
WORKDIR "/src/SEM3_Project_Backend"
RUN dotnet build "SEM3_Project_Backend.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "SEM3_Project_Backend.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY SEM3_Project_Backend/SeedData ./SeedData
ENTRYPOINT ["dotnet", "SEM3_Project_Backend.dll"]
