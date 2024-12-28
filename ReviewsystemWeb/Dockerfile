# Base image for runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0-nanoserver-1809 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Stage to build the project
FROM mcr.microsoft.com/dotnet/sdk:8.0-nanoserver-1809 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Update the path to include the subdirectory
COPY ["ReviewsystemWeb/ReviewsystemWeb.csproj", "ReviewsystemWeb/"]
WORKDIR "/src/ReviewsystemWeb"

RUN dotnet restore "ReviewsystemWeb.csproj"

# Copy everything inside the ReviewsystemWeb folder
COPY ReviewsystemWeb/. .
RUN dotnet build "ReviewsystemWeb.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Stage to publish the project
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "ReviewsystemWeb.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final runtime image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ReviewsystemWeb.dll"]
