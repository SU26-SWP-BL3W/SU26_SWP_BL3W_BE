# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy file project và restore dependencies
COPY ["SEAL_Backend/SEAL_Backend.csproj", "SEAL_Backend/"]
COPY ["SEAL.Application/SEAL_Application.csproj", "SEAL.Application/"]
COPY ["SEAL.Domain/SEAL_Domain.csproj", "SEAL.Domain/"]
COPY ["SEAL.Infrastructure/SEAL_Infrastructure.csproj", "SEAL.Infrastructure/"]
RUN dotnet restore "SEAL_Backend/SEAL_Backend.csproj"

# Copy toàn bộ code và build
COPY . .
WORKDIR "/src/SEAL_Backend"
RUN dotnet build "SEAL_Backend.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish "SEAL_Backend.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Run
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8080
EXPOSE 8081
ENTRYPOINT ["dotnet", "SEAL_Backend.dll"]
