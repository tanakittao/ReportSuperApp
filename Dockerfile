FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["EnterpriseDashboard.csproj", "./"]
RUN dotnet restore "EnterpriseDashboard.csproj"
COPY . .
RUN dotnet publish "EnterpriseDashboard.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
# Render จะส่งพอร์ตมาทาง Environment Variable
ENV ASPNETCORE_URLS=http://+:${PORT:-8080}
ENTRYPOINT ["dotnet", "EnterpriseDashboard.dll"]