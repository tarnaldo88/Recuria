FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Recuria/Recuria.Api/Recuria.Api.csproj", "Recuria/Recuria.Api/"]
COPY ["Recuria/Recuria.Application/Recuria.Application.csproj", "Recuria/Recuria.Application/"]
COPY ["Recuria/Recuria.Domain/Recuria.Domain.csproj", "Recuria/Recuria.Domain/"]
COPY ["Recuria/Recuria.Infrastructure/Recuria.Infrastructure.csproj", "Recuria/Recuria.Infrastructure/"]
RUN dotnet restore "Recuria/Recuria.Api/Recuria.Api.csproj"
COPY . .
WORKDIR "/src/Recuria/Recuria.Api"
RUN dotnet publish "Recuria.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Recuria.Api.dll"]
