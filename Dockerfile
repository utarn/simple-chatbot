FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
ARG DEBIAN_FRONTEND=noninteractive
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /solution
COPY . .
WORKDIR /solution/src/Web
RUN dotnet restore "Web.csproj" -v=q
RUN dotnet publish "/solution/src/Web/Web.csproj" --output /app  --configuration Release

FROM base AS final
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "ChatbotApi.Web.dll"]
