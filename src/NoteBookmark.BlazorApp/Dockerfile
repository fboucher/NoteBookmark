FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8004
EXPOSE 8006

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Directory.Build.props", "/src/"]
COPY ["Directory.Packages.props", "/src/"]
COPY ["src/NoteBookmark.BlazorApp/NoteBookmark.BlazorApp.csproj", "src/NoteBookmark.BlazorApp/"]
COPY ["src/NoteBookmark.Domain/NoteBookmark.Domain.csproj", "src/NoteBookmark.Domain/"]
COPY ["src/NoteBookmark.ServiceDefaults/NoteBookmark.ServiceDefaults.csproj", "src/NoteBookmark.ServiceDefaults/"]
RUN dotnet restore "src/NoteBookmark.BlazorApp/NoteBookmark.BlazorApp.csproj"
COPY . .
WORKDIR "/src/src/NoteBookmark.BlazorApp"
RUN dotnet build "NoteBookmark.BlazorApp.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "NoteBookmark.BlazorApp.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NoteBookmark.BlazorApp.dll"]
