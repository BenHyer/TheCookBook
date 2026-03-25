FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY CookbookMauiBlazor/CookbookMauiBlazor.Web/CookbookMauiBlazor.Web.csproj CookbookMauiBlazor/CookbookMauiBlazor.Web/
COPY CookbookMauiBlazor/CookbookMauiBlazor.Shared/CookbookMauiBlazor.Shared.csproj CookbookMauiBlazor/CookbookMauiBlazor.Shared/

RUN dotnet restore CookbookMauiBlazor/CookbookMauiBlazor.Web/CookbookMauiBlazor.Web.csproj

COPY . .
RUN dotnet publish CookbookMauiBlazor/CookbookMauiBlazor.Web/CookbookMauiBlazor.Web.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "CookbookMauiBlazor.Web.dll"]
