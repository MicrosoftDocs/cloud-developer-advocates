#FROM mcr.microsoft.com/dotnet/core/runtime:3.1-alpine
#COPY src/ /
#ENTRYPOINT [ "dotnet", "/AdvocateValidation.dll" ]


FROM mcr.microsoft.com/dotnet/core/runtime:3.1-buster-slim AS base
WORKDIR /validation

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["validation/AdvocateValidation.csproj", ""]
RUN dotnet restore "./AdvocateValidation.csproj"
COPY validation/ .
WORKDIR "/src/."
RUN dotnet build "AdvocateValidation.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AdvocateValidation.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "/AdvocateValidation.dll"]