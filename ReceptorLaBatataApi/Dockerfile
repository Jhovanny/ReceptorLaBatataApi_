# 1. Etapa de construcción
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiamos el archivo del proyecto y restauramos dependencias
COPY ["ReceptorLaBatataApi.csproj", "./"]
RUN dotnet restore "./ReceptorLaBatataApi.csproj"

# Copiamos el resto del código y compilamos
COPY . .
RUN dotnet publish "ReceptorLaBatataApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 2. Etapa de ejecución
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ReceptorLaBatataApi.dll"]