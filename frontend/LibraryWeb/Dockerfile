FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY frontend/LibraryWeb/ frontend/LibraryWeb/

RUN dotnet restore frontend/LibraryWeb/LibraryWeb.csproj
RUN dotnet publish frontend/LibraryWeb/LibraryWeb.csproj -c Release -o /app/out

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/out .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "LibraryWeb.dll"]

