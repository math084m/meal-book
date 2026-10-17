# Production image: ASP.NET API serving the Angular build from wwwroot.
# The Angular base href is /meal-book/ because a reverse proxy mounts the app there
# and strips the prefix before forwarding, so the API itself listens at the root.
FROM node:24-alpine AS frontend
WORKDIR /app
COPY src/frontend/meal-book/package*.json ./
RUN npm ci
COPY src/frontend/meal-book/ .
ARG APP_BASE_HREF=/meal-book/
RUN npm run build -- --base-href="${APP_BASE_HREF}"

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS backend
WORKDIR /source
COPY src/backend/MealBook.Domain/MealBook.Domain.csproj MealBook.Domain/
COPY src/backend/MealBook.Application/MealBook.Application.csproj MealBook.Application/
COPY src/backend/MealBook.Infrastructure/MealBook.Infrastructure.csproj MealBook.Infrastructure/
COPY src/backend/MealBook.Api/MealBook.Api.csproj MealBook.Api/
RUN dotnet restore MealBook.Api/MealBook.Api.csproj
COPY src/backend/ .
RUN dotnet publish MealBook.Api/MealBook.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
COPY --from=backend /app/publish .
COPY --from=frontend /app/dist/meal-book/browser ./wwwroot
ENTRYPOINT ["dotnet", "MealBook.Api.dll"]
