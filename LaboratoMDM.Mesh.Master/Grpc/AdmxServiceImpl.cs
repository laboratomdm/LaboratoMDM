using Grpc.Core;
using LaboratoMDM.Mesh.Master.Grpc.Operator.V1;
using LaboratoMDM.PolicyEngine.Domain;
using LaboratoMDM.PolicyEngine.Services;
using System.IO.Compression;

namespace LaboratoMDM.Mesh.Master.Grpc.Services;

public sealed class AdmxServiceImpl : AdmxService.AdmxServiceBase
{
    private readonly IAdmxQueryService _admxQueryService;
    private readonly IAdmxFolderImporter _admxFolderImporter;

    public AdmxServiceImpl(IAdmxQueryService admxQueryService, 
        IAdmxFolderImporter admxFolderImporter)
    {
        _admxQueryService = admxQueryService;
        _admxFolderImporter = admxFolderImporter;
    }

    public override async Task<ListAdmxFilesResponse> ListAdmxFiles(
        ListAdmxFilesRequest request,
        ServerCallContext context)
    {
        // Получаем все загруженные ADMX-файлы
        var snapshots = await _admxQueryService.GetAllSnapshotsAsync();

        var response = new ListAdmxFilesResponse();
        foreach (var snap in snapshots)
        {
            response.Files.Add(MapAdmxFile(snap.File));
        }

        return response;
    }

    public override async Task<Operator.V1.AdmxSnapshot> GetAdmxSnapshot(
        GetAdmxSnapshotRequest request,
        ServerCallContext context)
    {
        if (string.IsNullOrEmpty(request.FileHash))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "FileHash is required"));

        var snapshot = await _admxQueryService.GetSnapshotByHashAsync(request.FileHash);
        if (snapshot == null)
            throw new RpcException(new Status(StatusCode.NotFound, $"ADMX file with hash {request.FileHash} not found"));

        var response = new Operator.V1.AdmxSnapshot
        {
            File = MapAdmxFile(snapshot.File)
        };

        response.Policies.AddRange(snapshot.Policies.Select(MapPolicy));
        response.Categories.AddRange(snapshot.Categories.Select(MapCategory));
        response.Namespaces.AddRange(snapshot.Namespaces.Select(MapNamespace));

        return response;
    }

    public override async Task<ImportAdmxZipResponse> ImportAdmxZip(
    ImportAdmxZipRequest request,
    ServerCallContext context)
    {
        if (request.ZipContent == null || request.ZipContent.Length == 0)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "ZipContent is empty"));

        // Создаём временную папку для распаковки
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            // Записываем zip во временный файл
            var zipFilePath = Path.Combine(tempDir, "import.zip");
            await File.WriteAllBytesAsync(zipFilePath, request.ZipContent.ToByteArray());

            // Генерируем имя папки из zip файла
            var zipFolderName = Path.GetFileNameWithoutExtension(zipFilePath);
            var extractPath = Path.Combine(tempDir, zipFolderName);

            // Распаковываем zip в эту папку
            ZipFile.ExtractToDirectory(zipFilePath, extractPath);

            // Вызываем импорт именно этой папки
            await _admxFolderImporter.ImportDirectoryAsync(extractPath);


            return new ImportAdmxZipResponse
            {
                Success = true,
                Message = $"Imported ADMX files from zip to {tempDir}"
            };
        }
        catch (Exception ex)
        {
            return new ImportAdmxZipResponse
            {
                Success = false,
                Message = $"Failed to import ADMX zip: {ex.Message}"
            };
        }
        finally
        {
            // Удаляем временную папку
            try { 
                Directory.Delete(tempDir, true); 
            } catch { }
        }
    }

    private static Grpc.Operator.V1.AdmxFile MapAdmxFile(AdmxFileEntity file)
    {
        return new Grpc.Operator.V1.AdmxFile
        {
            FileName = file.FileName,
            FileHash = file.FileHash,
            LoadedAtUnix = new DateTimeOffset(file.LoadedAt).ToUnixTimeSeconds()
        };
    }

    private static Grpc.Operator.V1.PolicyDescriptor MapPolicy(PolicyEntity p)
    {
        var descriptor = new Grpc.Operator.V1.PolicyDescriptor
        {
            PolicyHash = p.Hash,
            Name = p.Name,
            Scope = p.Scope switch
            {
                Core.Models.Policy.PolicyScope.User => Grpc.Operator.V1.PolicyScope.User,
                Core.Models.Policy.PolicyScope.Machine => Grpc.Operator.V1.PolicyScope.Machine,
                Core.Models.Policy.PolicyScope.Both => Grpc.Operator.V1.PolicyScope.Both,
                _ => Grpc.Operator.V1.PolicyScope.None
            },
            RegistryKey = p.RegistryKey,
            ValueName = p.ValueName,
            EnabledValue = p.EnabledValue ?? 1,
            DisabledValue = p.DisabledValue ?? 0,
            ParentCategory = p.ParentCategory ?? string.Empty,
            SupportedOnRef = p.SupportedOnRef ?? string.Empty
        };

        foreach (var e in p.Elements)
        {
            descriptor.Elements.Add(new Grpc.Operator.V1.PolicyElement
            {
                IdName = e.IdName,
                Type = e.Type,
                ValueName = e.ValueName ?? string.Empty,
                MaxLength = e.MaxLength ?? 0,
                Required = e.Required,
                ClientExtension = e.ClientExtension ?? string.Empty
            });
        }

        descriptor.RequiredCapabilities.AddRange(p.Capabilities.Select(c => c.Capability));
        descriptor.RequiredHardware.AddRange(p.HardwareRequirements.Select(h => h.HardwareFeature));

        return descriptor;
    }

    private static Grpc.Operator.V1.PolicyCategory MapCategory(PolicyCategoryEntity c)
    {
        return new Grpc.Operator.V1.PolicyCategory
        {
            Name = c.Name,
            DisplayName = c.DisplayName,
            ExplainText = c.ExplainText ?? string.Empty,
            ParentCategoryName = c.ParentCategory?.Name ?? string.Empty
        };
    }

    private static Grpc.Operator.V1.PolicyNamespace MapNamespace(PolicyNamespaceEntity ns)
    {
        return new Grpc.Operator.V1.PolicyNamespace
        {
            Prefix = ns.Prefix,
            Namespace = ns.Namespace
        };
    }
}
