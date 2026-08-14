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

# Npgsql can libgssapi khi Postgres dam phan GSSAPI/Kerberos. Image aspnet
# (Debian slim) khong co san -> loi "Cannot load library libgssapi_krb5.so.2"
# roi khong ket noi duoc DB. DB cua Render khong dinh vi khong dam phan GSSAPI,
# nhung Postgres tu dung tren VPS thi co.
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8080
EXPOSE 8081
ENTRYPOINT ["dotnet", "SEAL_Backend.dll"]
