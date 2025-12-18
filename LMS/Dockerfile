FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src


COPY ["LMS.sln", "./"]

COPY ["LMS/LMS.API.csproj", "LMS/"]
COPY ["LMS.Application/LMS.Application.csproj", "LMS.Application/"]
COPY ["LMS.Domain/LMS.Domain.csproj", "LMS.Domain/"]
COPY ["LMS.Infrastructure/LMS.Infrastructure.csproj", "LMS.Infrastructure/"]
COPY ["LMS.UnitTests/LMS.UnitTests.csproj", "LMS.UnitTests/"]
COPY ["LMS.IntegrationTests/LMS.IntegrationTests.csproj", "LMS.IntegrationTests/"]


RUN dotnet restore "LMS.sln"


COPY . .


WORKDIR "/src/LMS"
RUN dotnet publish "LMS.API.csproj" -c Release -o /app/publish /p:UseAppHost=false


FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 5298



ENTRYPOINT ["dotnet", "LMS.API.dll"]