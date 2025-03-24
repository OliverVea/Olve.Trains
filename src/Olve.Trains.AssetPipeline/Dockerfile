##############################
# Stage 1: Retrieve the dependencies for the Asset Pipeline
##############################
FROM mcr.microsoft.com/dotnet/runtime:9.0 AS asset-builder
WORKDIR /app

# Install additional tools needed for asset processing, including Shader Slang dependencies.
RUN apt-get update && \
    apt-get install -y awscli wget tar unzip && \
    rm -rf /var/lib/apt/lists/*

# Download and extract the Shader Slang Linux tarball.
# RUN wget -O slang.zip https://github.com/shader-slang/slang/releases/download/v2025.6.2/slang-2025.6.2-linux-x86_64.zip

# RUN unzip slang.zip -d /usr/local/slang-2025.6.2-linux-x86_64 \
#     && ls -l /usr/local/ \
#     && ls -l /usr/local/slang-2025.6.2-linux-x86_64/

# Verify the Slang installation.
# RUN /usr/local/slang-2025.6.2-linux-x86_64/bin/slangc -h

##############################
# Stage 2: Build the Asset Pipeline
##############################
FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Olve.Trains.AssetPipeline/Olve.Trains.AssetPipeline.csproj", "Olve.Trains.AssetPipeline/"]
RUN dotnet restore "Olve.Trains.AssetPipeline/Olve.Trains.AssetPipeline.csproj"
COPY . .
WORKDIR "/src/Olve.Trains.AssetPipeline"
RUN dotnet build "Olve.Trains.AssetPipeline.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Olve.Trains.AssetPipeline.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

##############################
# Final Stage: Create the final image
##############################
FROM mcr.microsoft.com/dotnet/runtime:9.0 AS final
WORKDIR /app

# Copy the published asset pipeline app
COPY --from=publish /app/publish .

# Copy the Shader Slang binaries.
# COPY --from=asset-builder /usr/local/slang-2025.6.2-linux-x86_64 /usr/local/slang-2025.6.2-linux-x86_64

# Add the Shader Slang binaries to the PATH.
# ENV PATH="/usr/local/slang-2025.6.2-linux-x86_64/bin:${PATH}"

# Set the entrypoint to run the .NET application.
ENTRYPOINT ["dotnet", "/app/Olve.Trains.AssetPipeline.dll"]

