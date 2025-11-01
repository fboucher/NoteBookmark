FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8000
EXPOSE 8002

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["NoteBookmark.Api/NoteBookmark.Api.csproj", "NoteBookmark.Api/"]
COPY ["NoteBookmark.Domain/NoteBookmark.Domain.csproj", "NoteBookmark.Domain/"]
COPY ["NoteBookmark.ServiceDefaults/NoteBookmark.ServiceDefaults.csproj", "NoteBookmark.ServiceDefaults/"]
RUN dotnet restore "NoteBookmark.Api/NoteBookmark.Api.csproj"
COPY . .
WORKDIR "/src/NoteBookmark.Api"
RUN dotnet build "NoteBookmark.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "NoteBookmark.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NoteBookmark.Api.dll"]
