FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "src/LabViroMol.Api/LabViroMol.Api.csproj"

ENV ConnectionStrings__LabViroMol="Host=localhost;Port=5432;Database=designtime;Username=designtime;Password=designtime"

RUN dotnet publish "src/LabViroMol.Api/LabViroMol.Api.csproj" -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "LabViroMol.Api.dll"]
