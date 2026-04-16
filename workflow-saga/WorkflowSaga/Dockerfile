FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY shared/Library.Contracts/ shared/Library.Contracts/
COPY workflow-saga/WorkflowSaga/ workflow-saga/WorkflowSaga/

RUN dotnet restore workflow-saga/WorkflowSaga/WorkflowSaga.csproj
RUN dotnet publish workflow-saga/WorkflowSaga/WorkflowSaga.csproj -c Release -o /app/out

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/out .
ENTRYPOINT ["dotnet", "WorkflowSaga.dll"]

