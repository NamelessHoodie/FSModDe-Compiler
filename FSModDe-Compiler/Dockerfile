FROM mcr.microsoft.com/dotnet/runtime:5.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["FSModDe-Compiler/FSModDe-Compiler.csproj", "FSModDe-Compiler/"]
RUN dotnet restore "FSModDe-Compiler/FSModDe-Compiler.csproj"
COPY . .
WORKDIR "/src/FSModDe-Compiler"
RUN dotnet build "FSModDe-Compiler.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FSModDe-Compiler.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FSModDe-Compiler.dll"]
