# Recipe Render uses to build and run the server (Render has no built-in .NET runtime, but it runs Docker).

# Stage 1 - build: an image with the full .NET SDK
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Project files first, so the NuGet restore is cached when only code changes
COPY Tandem.sln .
COPY Tandem.Core/Tandem.Core.csproj Tandem.Core/
COPY Tandem.Data/Tandem.Data.csproj Tandem.Data/
COPY Tandem.Service/Tandem.Service.csproj Tandem.Service/
COPY Tandem.API/Tandem.API.csproj Tandem.API/
RUN dotnet restore Tandem.API/Tandem.API.csproj

COPY . .
RUN dotnet publish Tandem.API/Tandem.API.csproj -c Release -o /app --no-restore

# Stage 2 - run: a small image with the runtime only, without the SDK and the source code
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app .

# The server listens on port 8080 (the default of this image); on Render set the PORT variable to 8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Tandem.API.dll"]
