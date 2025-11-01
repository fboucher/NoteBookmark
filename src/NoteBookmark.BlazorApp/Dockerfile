FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8004
EXPOSE 8006

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["NoteBookmark.BlazorApp/NoteBookmark.BlazorApp.csproj", "NoteBookmark.BlazorApp/"]
COPY ["NoteBookmark.Domain/NoteBookmark.Domain.csproj", "NoteBookmark.Domain/"]
COPY ["NoteBookmark.ServiceDefaults/NoteBookmark.ServiceDefaults.csproj", "NoteBookmark.ServiceDefaults/"]
RUN dotnet restore "NoteBookmark.BlazorApp/NoteBookmark.BlazorApp.csproj"
COPY . .
WORKDIR "/src/NoteBookmark.BlazorApp"
RUN dotnet build "NoteBookmark.BlazorApp.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "NoteBookmark.BlazorApp.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NoteBookmark.BlazorApp.dll"]
