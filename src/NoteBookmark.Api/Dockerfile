FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8000
EXPOSE 8002

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/NoteBookmark.Api/NoteBookmark.Api.csproj", "src/NoteBookmark.Api/"]
COPY ["src/NoteBookmark.Domain/NoteBookmark.Domain.csproj", "src/NoteBookmark.Domain/"]
COPY ["src/NoteBookmark.ServiceDefaults/NoteBookmark.ServiceDefaults.csproj", "src/NoteBookmark.ServiceDefaults/"]
COPY ["src/NoteBookmark.AIServices/NoteBookmark.AIServices.csproj", "src/NoteBookmark.AIServices/"]
RUN dotnet restore "src/NoteBookmark.Api/NoteBookmark.Api.csproj"
COPY . .
WORKDIR "/src/src/NoteBookmark.Api"
RUN dotnet build "NoteBookmark.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "NoteBookmark.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NoteBookmark.Api.dll"]
