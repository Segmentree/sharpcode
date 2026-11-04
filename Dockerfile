# API + judge runner. The client is deployed separately (Netlify) and proxies /api here.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY server/ .
RUN dotnet publish SharpCode.Api/SharpCode.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://0.0.0.0:8080 \
    DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    Database__Path=/data/sharpcode.db
EXPOSE 8080
ENTRYPOINT ["dotnet", "SharpCode.Api.dll"]
