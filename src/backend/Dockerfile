FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
COPY MealBook.Domain/MealBook.Domain.csproj MealBook.Domain/
COPY MealBook.Application/MealBook.Application.csproj MealBook.Application/
COPY MealBook.Infrastructure/MealBook.Infrastructure.csproj MealBook.Infrastructure/
COPY MealBook.Api/MealBook.Api.csproj MealBook.Api/
RUN dotnet restore MealBook.Api/MealBook.Api.csproj
COPY . .
RUN dotnet publish MealBook.Api/MealBook.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MealBook.Api.dll"]
