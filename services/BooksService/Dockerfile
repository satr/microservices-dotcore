FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY shared/Library.Contracts/ shared/Library.Contracts/
COPY services/BooksService/ services/BooksService/

RUN dotnet restore services/BooksService/BooksService.csproj
RUN dotnet publish services/BooksService/BooksService.csproj -c Release -o /app/out

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/out .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "BooksService.dll"]

