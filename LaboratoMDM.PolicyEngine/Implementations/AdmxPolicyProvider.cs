#nullable enable
using System.Xml.Linq;
using LaboratoMDM.Core.Models.Policy;
using Microsoft.Extensions.Logging;

namespace LaboratoMDM.PolicyEngine.Implementations;

internal sealed class QNameResolver
{
    private readonly Dictionary<string, string> _prefixes;

    public QNameResolver(IEnumerable<PolicyNamespaceDefinition> namespaces)
    {
        _prefixes = namespaces.ToDictionary(
            n => n.Prefix,
            n => n.Namespace,
            StringComparer.OrdinalIgnoreCase);
    }

    public (string Namespace, string Name) Resolve(string qname)
    {
        var parts = qname.Split(':', 2);
        if (parts.Length != 2)
            throw new InvalidOperationException($"Invalid QName: {qname}");

        if (!_prefixes.TryGetValue(parts[0], out var ns))
            throw new InvalidOperationException($"Unknown namespace prefix: {parts[0]}");

        return (ns, parts[1]);
    }
}

public sealed class AdmxPolicyProvider : IPolicyProvider
{
    private static readonly XNamespace Ns =
        "http://schemas.microsoft.com/GroupPolicy/2006/07/PolicyDefinitions";

    private readonly ILogger _logger;
    private readonly string _admxFilePath;

    private readonly Dictionary<string, PolicyDefinition> _policies =
        new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<PolicyNamespaceDefinition> Namespaces { get; private set; } = [];
    public IReadOnlyList<PolicyCategoryDefinition> Categories { get; private set; } = [];
    public Dictionary<string, SupportedOnDefinition> SupportedOnDefinitions { get; private set; } = [];

    public AdmxPolicyProvider(string admxFilePath, ILogger logger)
    {
        _admxFilePath = admxFilePath;
        _logger = logger;

        LoadInternal();
    }

    public IReadOnlyList<PolicyDefinition> LoadPolicies() =>
        _policies.Values.ToList();

    public PolicyDefinition? FindPolicy(string name) =>
        _policies.TryGetValue(name, out var p) ? p : null;

    private void LoadInternal()
    {
        _logger.LogInformation("Loading ADMX file {File}", _admxFilePath);

        var doc = XDocument.Load(_admxFilePath);

        Namespaces = ParseNamespaces(doc);
        var resolver = new QNameResolver(Namespaces);

        Categories = ParseCategories(doc);
        SupportedOnDefinitions = ParseSupportedOn(doc);

        foreach (var policy in ParsePolicies(doc, resolver))
        {
            if (!_policies.TryAdd(policy.Name, policy))
            {
                _logger.LogWarning(
                    "Duplicate policy {Name} in {File}",
                    policy.Name,
                    _admxFilePath);
            }
        }
    }
    private static IReadOnlyList<PolicyNamespaceDefinition> ParseNamespaces(XDocument doc)
    {
        var root = doc.Descendants(Ns + "policyNamespaces").SingleOrDefault();
        if (root == null)
            return [];

        return root.Elements()
            .Select(e => new PolicyNamespaceDefinition
            {
                Prefix = (string)e.Attribute("prefix")!,
                Namespace = (string)e.Attribute("namespace")!,
                IsTarget = e.Name.LocalName == "target"
            })
            .ToList();
    }

    private static IReadOnlyList<PolicyCategoryDefinition> ParseCategories(XDocument doc)
    {
        return doc.Descendants(Ns + "category")
            .Select(c => new PolicyCategoryDefinition
            {
                Name = (string)c.Attribute("name")!,
                ParentCategoryRef =
                    c.Element(Ns + "parentCategory")?.Attribute("ref")?.Value,
                DisplayName =
                    (string?)c.Attribute("displayName") ?? string.Empty,
                ExplainText =
                    c.Element(Ns + "explainText")?.Value?.Trim()
            })
            .ToList();
    }

    private static Dictionary<string, SupportedOnDefinition> ParseSupportedOn(XDocument doc)
    {
        var dict = new Dictionary<string, SupportedOnDefinition>();

        var defs = doc.Descendants(Ns + "supportedOn")
            .Descendants(Ns + "definition");

        foreach (var d in defs)
        {
            var name = (string)d.Attribute("name")!;
            var exprRoot = d.Elements().FirstOrDefault();

            if (exprRoot != null)
            {
                dict[name] = new SupportedOnDefinition
                {
                    Name = name,
                    RootExpression = ParseSupportedExpression(exprRoot)
                };
            }
        }

        return dict;
    }

    private static SupportedOnExpression ParseSupportedExpression(XElement el)
    {
        return el.Name.LocalName switch
        {
            "and" => new SupportedOnAnd
            {
                Items = el.Elements().Select(ParseSupportedExpression).ToList()
            },
            "or" => new SupportedOnOr
            {
                Items = el.Elements().Select(ParseSupportedExpression).ToList()
            },
            "reference" => new SupportedOnReference
            {
                Ref = (string)el.Attribute("ref")!
            },
            "range" => new SupportedOnRange
            {
                Ref = (string)el.Attribute("ref")!,
                MinVersionIndex = (int?)el.Attribute("minVersionIndex"),
                MaxVersionIndex = (int?)el.Attribute("maxVersionIndex")
            },
            _ => throw new NotSupportedException(el.Name.LocalName)
        };
    }

    private static IEnumerable<PolicyDefinition> ParsePolicies(
        XDocument doc,
        QNameResolver resolver)
    {
        return doc.Descendants(Ns + "policy")
            .Select(p =>
            {
                var classAttr = (string?)p.Attribute("class");
                var presentationRef = (string?)p.Attribute("presentation");
                if (presentationRef != null)
                {
                    presentationRef = ExtractPresentationRef(presentationRef);
                }

                var categoryName = p.Elements(Ns + "parentCategory").FirstOrDefault()?.Attribute("ref")?.Value;
                return new PolicyDefinition
                {
                    Name = (string)p.Attribute("name")!,
                    DisplayName = ExtractStringRef(p.Attribute("displayName")?.Value),
                    ExplainText = ExtractStringRef(p.Attribute("explainText")?.Value),
                    Scope = classAttr switch
                    {
                        "User" => PolicyScope.User,
                        "Machine" => PolicyScope.Machine,
                        "Both" => PolicyScope.Both,
                        _ => PolicyScope.None
                    },
                    RegistryKey = (string?)p.Attribute("key") ?? string.Empty,
                    ValueName = (string?)p.Attribute("valueName") ?? string.Empty,
                    ParentCategoryRef = categoryName,
                    PresentationRef = presentationRef,
                    SupportedOnRef =
                        p.Element(Ns + "supportedOn")?.Attribute("ref")?.Value,
                    
                    Elements = ParseElements(p)
                };
            });
    }

    private static IReadOnlyList<PolicyElementDefinition> ParseElements(XElement policy)
    {
        var elementsRoot = policy.Element(Ns + "elements");
        if (elementsRoot == null)
            return [];

        return elementsRoot.Elements()
            .Select(e => new PolicyElementDefinition
            {
                Type = e.Name.LocalName,
                IdName = (string)e.Attribute("id")!,
                ValueName = (string?)e.Attribute("valueName"),
                MaxLength = (int?)e.Attribute("maxLength"),
                Required = (bool?)e.Attribute("required") ?? false
            })
            .ToList();
    }

    private static string ExtractPresentationRef(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        return value
            .Replace("$(presentation.", "")
            .Replace(")", "");
    }

    private static string? ExtractStringRef(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        return value
            .Replace("$(string.", "")
            .Replace(")", "");
    }
}
