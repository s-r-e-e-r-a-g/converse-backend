# Use .NET 9.0 SDK for building the project
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copy the project files and restore dependencies
COPY . ./
RUN dotnet restore

# Build the project
RUN dotnet publish -c Release -o /out

# Use .NET 9.0 runtime for running the app
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /out .

# Set the startup command
CMD ["dotnet", "Converse.dll"]

# Expose the port (adjust if needed)
EXPOSE 80
