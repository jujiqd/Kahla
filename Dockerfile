FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY Kahla.Server/Kahla.Server.csproj Kahla.Server/
COPY Kahla.SDK/Kahla.SDK.csproj Kahla.SDK/
RUN dotnet restore "Kahla.Server/Kahla.Server.csproj"
COPY . .
WORKDIR "/src/Kahla.Server"
RUN dotnet build "Kahla.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Kahla.Server.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Kahla.Server.dll"]
