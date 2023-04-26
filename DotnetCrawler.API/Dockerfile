#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

#Depending on the operating system of the host machines(s) that will build or run the containers, the image specified in the FROM statement may need to be changed.
#For more information, please see https://aka.ms/containercompat

FROM mcr.microsoft.com/dotnet/aspnet:3.1 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:3.1 AS build
WORKDIR /src
COPY ["DotnetCrawler.API/DotnetCrawler.API.csproj", "DotnetCrawler.API/"]
COPY ["DotnetCrawler.Core/DotnetCrawler.Core.csproj", "DotnetCrawler.Core/"]
COPY ["DotnetCrawler.Data/DotnetCrawler.Data.csproj", "DotnetCrawler.Data/"]
RUN dotnet restore "DotnetCrawler.API/DotnetCrawler.API.csproj"
COPY . .
WORKDIR "/src/DotnetCrawler.API"
RUN dotnet build "DotnetCrawler.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DotnetCrawler.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DotnetCrawler.API.dll"]