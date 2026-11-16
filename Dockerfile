FROM node:24-alpine AS frontend
WORKDIR /src/fe_da
COPY fe_da/package.json fe_da/package-lock.json ./
RUN npm ci
COPY fe_da/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS backend
WORKDIR /src
COPY be_da/be_da.csproj be_da/
RUN dotnet restore be_da/be_da.csproj
COPY be_da/ be_da/
RUN dotnet publish be_da/be_da.csproj -c Release -o /out --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=backend /out/ ./
COPY --from=frontend /src/fe_da/dist/ ./wwwroot/
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "be_da.dll"]
