# Use the official ASP.NET Core runtime as the base image for running the app in production
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Use the official .NET SDK image for building the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy the project file and restore dependencies
COPY ["ReviewsystemWeb/ReviewsystemWeb.csproj", "ReviewsystemWeb/"]
RUN dotnet restore "ReviewsystemWeb/ReviewsystemWeb.csproj"

# Copy the entire project and build it
COPY . .
WORKDIR "/src/ReviewsystemWeb"
RUN dotnet build "ReviewsystemWeb.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish the app to the /app/publish directory
RUN dotnet publish "ReviewsystemWeb.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Set up the final runtime image
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ReviewsystemWeb.dll"]
