FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Directory.Build.props", "."]
COPY ["Directory.Packages.props", "."]
COPY ["ServerEye.API/ServerEye.API.csproj", "ServerEye.API/"]
COPY ["ServerEye.Core/ServerEye.Core.csproj", "ServerEye.Core/"]
COPY ["ServerEye.Infrastructure/ServerEye.Infrastructure.csproj", "ServerEye.Infrastructure/"]
RUN dotnet restore "ServerEye.API/ServerEye.API.csproj"
COPY . .
WORKDIR "/src/ServerEye.API"
RUN dotnet build "ServerEye.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ServerEye.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ServerEye.API.dll"]
