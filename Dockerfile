FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Qwek.csproj ./
RUN dotnet restore Qwek.csproj

COPY . .
RUN dotnet publish Qwek.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM build AS migrate
RUN dotnet tool install --global dotnet-ef --version 10.0.14
ENV PATH="$PATH:/root/.dotnet/tools"
ENTRYPOINT ["dotnet", "ef", "database", "update", "--project", "Qwek.csproj"]

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

COPY --from=build /app/publish .
RUN mkdir -p /app/uploads/avatars /app/LogFiles && chown -R app:app /app

USER app

ENTRYPOINT ["dotnet", "Qwek.dll"]
