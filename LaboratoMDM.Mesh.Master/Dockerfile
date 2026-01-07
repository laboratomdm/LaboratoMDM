FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["LaboratoMDM.sln", "./"]

COPY ["LaboratoMDM.Mesh.Master/LaboratoMDM.Mesh.Master.csproj", "LaboratoMDM.Mesh.Master/"]
COPY ["LaboratoMDM.Mesh.Protos/LaboratoMDM.Mesh.Protos.csproj", "LaboratoMDM.Mesh.Protos/"]
COPY ["LaboratoMDM.NodeEngine/LaboratoMDM.NodeEngine.csproj", "LaboratoMDM.NodeEngine/"]
COPY ["LaboratoMDM.PolicyEngine/LaboratoMDM.PolicyEngine.csproj", "LaboratoMDM.PolicyEngine/"]
COPY ["LaboratoMDM.Core/LaboratoMDM.Core.csproj", "LaboratoMDM.Core/"]
COPY ["LaboratoMDM.ActiveDirectory.Core/LaboratoMDM.ActiveDirectory.Core.csproj", "LaboratoMDM.ActiveDirectory.Core/"]


RUN dotnet restore "LaboratoMDM.Mesh.Master/LaboratoMDM.Mesh.Master.csproj"

# 2) копируем исходники 
COPY ["LaboratoMDM.Mesh.Master/", "LaboratoMDM.Mesh.Master/"]
COPY ["LaboratoMDM.Mesh.Protos/", "LaboratoMDM.Mesh.Protos/"]
COPY ["LaboratoMDM.NodeEngine/", "LaboratoMDM.NodeEngine/"]
COPY ["LaboratoMDM.PolicyEngine/", "LaboratoMDM.PolicyEngine/"]
COPY ["LaboratoMDM.Core/", "LaboratoMDM.Core/"]
COPY ["LaboratoMDM.ActiveDirectory.Core/", "LaboratoMDM.ActiveDirectory.Core/"]


# publish
RUN dotnet publish "LaboratoMDM.Mesh.Master/LaboratoMDM.Mesh.Master.csproj" -c Release -o /app/publish --no-restore


FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# данные и миграции внутри контейнера
RUN mkdir -p /app/data /app/migrations

COPY --from=build /app/publish/ ./

# миграции КОРНЕ репо 
COPY ["1__policies_tables.sql", "/app/migrations/"]
COPY ["2__pilicies_complience.sql", "/app/migrations/"]
COPY ["3__adml_translation_structure_tables.sql", "/app/migrations/"]
COPY ["4__presentation_tables.sql", "/app/migrations/"]

# если миграции лежат в папке проекта
# COPY ["LaboratoMDM.PolicyEngine/Database/", "/app/migrations/"]

ENV DB_FILE=/app/data/master.db
ENV MIGRATIONS_PATH=/app/migrations

EXPOSE 5000
ENTRYPOINT ["dotnet", "LaboratoMDM.Mesh.Master.dll"]
