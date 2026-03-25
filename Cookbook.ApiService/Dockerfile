FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Cookbook.ApiService/Cookbook.ApiService.csproj Cookbook.ApiService/
COPY Cookbook.ServiceDefaults/Cookbook.ServiceDefaults.csproj Cookbook.ServiceDefaults/

RUN dotnet restore Cookbook.ApiService/Cookbook.ApiService.csproj

COPY . .
RUN dotnet publish Cookbook.ApiService/Cookbook.ApiService.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "Cookbook.ApiService.dll"]
