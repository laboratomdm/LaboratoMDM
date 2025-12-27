using LaboratoMDM.Core.Models.Policy;
using LaboratoMDM.PolicyEngine.Domain;
using LaboratoMDM.PolicyEngine.Implementations;
using LaboratoMDM.PolicyEngine.Services.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace LaboratoMDM.PolicyEngine.Services;

public sealed class AdmxFolderImporter : IAdmxFolderImporter
{
    private readonly IAdmxImportService _importService;
    private readonly ILogger<AdmxFolderImporter> _logger;

    public AdmxFolderImporter(
        IAdmxImportService importService,
        ILogger<AdmxFolderImporter> logger)
    {
        _importService = importService;
        _logger = logger;
    }

    public async Task ImportDirectoryAsync(
        string admxDirectoryPath,
        CancellationToken ct = default)
    {
        if (!Directory.Exists(admxDirectoryPath))
            throw new DirectoryNotFoundException(admxDirectoryPath);

        foreach (var file in Directory.EnumerateFiles(admxDirectoryPath, "*.admx"))
        {
            await ImportFileAsync(file, ct);
        }
    }

    public async Task ImportFileAsync(
        string admxFilePath,
        CancellationToken ct = default)
    {
        if (!File.Exists(admxFilePath))
            throw new FileNotFoundException(admxFilePath);

        _logger.LogInformation("Importing ADMX file {File}", admxFilePath);

        var hash = await ComputeHashAsync(admxFilePath, ct);

        var provider = CreateProviderForFile(admxFilePath);

        var policies = provider.LoadPolicies();

        var importModel = BuildImportModel(
            admxFilePath,
            hash,
            provider.Namespaces,
            provider.Categories,
            policies);

        await _importService.ImportAsync(importModel, ct);
    }

    private static AdmxPolicyProvider CreateProviderForFile(string file)
    {
        return new AdmxPolicyProvider(
            file,
            NullLogger<AdmxPolicyProvider>.Instance);
    }

    private static async Task<string> ComputeHashAsync(
        string file,
        CancellationToken ct)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        await using var stream = File.OpenRead(file);
        var hash = await sha.ComputeHashAsync(stream, ct);
        return Convert.ToHexString(hash);
    }

    private static AdmxImportModel BuildImportModel(
        string filePath,
        string fileHash,
        IReadOnlyList<PolicyNamespaceDefinition> policyNamespaceDefinitions,
        IReadOnlyList<PolicyCategoryDefinition> policyCategoryDefinitions,
        IReadOnlyList<PolicyDefinition> policies)
    {
        return new AdmxImportModel
        {
            FileName = Path.GetFileName(filePath),
            FileHash = fileHash,
            Namespaces = policyNamespaceDefinitions
                .Select(PolicyNamespaceMapper.ToEntity)
                .ToList(),
            Categories = policyCategoryDefinitions
                .Select(PolicyCategoryMapper.ToEntity)
                .ToList(),
            Policies = policies
                .Select(PolicyMapper.ToEntity)
                .ToList()
        };
    }

}

