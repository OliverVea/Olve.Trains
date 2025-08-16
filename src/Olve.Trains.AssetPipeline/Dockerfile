##############################
# (Optional) Stage 1: tools for assets
##############################
FROM mcr.microsoft.com/dotnet/runtime:9.0 AS asset-builder
WORKDIR /app
RUN apt-get update && \
    apt-get install -y awscli wget tar unzip && \
    rm -rf /var/lib/apt/lists/*

##############################
# Stage 2: build & publish
##############################
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS publish
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# 1) Copy solution + central props first (cache-friendly)
COPY ./Directory.Build.props ./
COPY ./Directory.Packages.props ./

# 2) Copy project files for all referenced projects
COPY ./src/Olve.Engine3D/Olve.Engine3D.csproj ./src/Olve.Engine3D/
COPY ./src/Olve.Trains.AssetPipeline/Olve.Trains.AssetPipeline.csproj ./src/Olve.Trains.AssetPipeline/

# 3) Restore (using the solution or the app csproj)
RUN dotnet restore ./src/Olve.Trains.AssetPipeline/Olve.Trains.AssetPipeline.csproj

# 4) Bring in the rest of the source
COPY . .

# 5) Publish the target app
RUN dotnet publish ./src/Olve.Trains.AssetPipeline/Olve.Trains.AssetPipeline.csproj \
    -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final stage: runtime with ASP.NET shared framework
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Olve.Trains.AssetPipeline.dll"]

