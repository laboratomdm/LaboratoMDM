#nullable enable
using System.Text.RegularExpressions;
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
            .Select(e => 
            {
                var type = ResolvePolicyElementType(e.Name.LocalName.ToLowerInvariant());
                return type switch
                {
                    PolicyElementType.LIST => ParseListType(e),
                    PolicyElementType.DECIMAL => ParseDecimalType(e),
                    PolicyElementType.TEXT => ParseTextType(e),
                    PolicyElementType.MULTITEXT => ParseMultiTextType(e),
                    _ => new PolicyElementDefinition
                    {
                        Type = type,
                        IdName = (string)e.Attribute("id")!,
                        ValueName = (string?)e.Attribute("valueName"),
                        MaxLength = (int?)e.Attribute("maxLength"),
                        Required = (bool?)e.Attribute("required") ?? false,
                        Childs = ParseElementItems(e)
                    }
                };
            })
            .ToList();
    }

    private static List<PolicyElementItemDefinition> ParseElementItems(XElement element)
    {
        var type = ResolvePolicyElementType(element.Name.LocalName.ToLowerInvariant());

        return type switch
        {
            PolicyElementType.ENUM => ParseEnumItems(element),
            PolicyElementType.BOOLEAN => ParseBooleanType(element),
            _ => throw new InvalidOperationException($"Has no child item definition for elements with type: {type}")
        };
    }
    private static List<PolicyElementItemDefinition> ParseEnumItems(XElement elements)
    {
        var items = elements.Element(Ns + "item");
        if(items == null)
            return [];

        return items.Elements().Select(item =>
        {
            var itemValue = ParseItemValue(item);
            return new PolicyElementItemDefinition()
            {
                RegistryKey = item.Attribute("key")?.Value,
                ValueName = item.Attribute("valueName")?.Value,
                DisplayName = ExtractStringRef(item.Attribute("displayName")?.Value),
                ParentType = PolicyChildType.ELEMENTS,
                Type = PolicyElementItemType.VALUE,
                ValueType = itemValue.Type,
                Value = itemValue.Value,
                Childs = ParseValueListType(item)
            };
        }).ToList();
    }

    private static List<PolicyElementItemDefinition> ParseValueListType(XElement valueListElement)
    {
        var items = valueListElement.Element(Ns + "item");
        if(items == null)
        {
            return [];
        }

        return items.Elements().Select(item =>
        {
            var itemValue = ParseItemValue(item);
            return new PolicyElementItemDefinition()
            {
                DisplayName = ExtractStringRef(item.Attribute("displayName")?.Value),
                RegistryKey = item.Attribute("key")?.Value,
                ValueName = item.Attribute("key")?.Value,
                Value = itemValue.Value,
                ValueType = itemValue.Type,
                ParentType = PolicyChildType.ELEMENTS,
                Type = PolicyElementItemType.VALUE_LIST
            };
        }).ToList();
    }

    private static List<PolicyElementItemDefinition> ParseBooleanType(XElement booleanElement)
    {
        var trueValueElement = booleanElement.Element("trueValue")?.Elements().FirstOrDefault()
            ?? throw new ArgumentNullException("Boolean item type always should have trueValue.");
        var falseValueElement = booleanElement.Element("falseValue")?.Elements().FirstOrDefault()
            ?? throw new ArgumentNullException("Boolean item type always should have falseValue.");

        var trueItemValue = ParseItemValue(trueValueElement);
        var falseItemValue = ParseItemValue(falseValueElement);

        var idName = booleanElement.Attribute("id")?.Value;
        var registryKey = booleanElement.Attribute("key")?.Value;
        var valueName = booleanElement.Attribute("valueName")?.Value;
        var required = booleanElement.Attribute("required")?.Value;

        var trueValueDefinition = new PolicyElementItemDefinition()
        {
            IdName = !string.IsNullOrEmpty(idName) ? idName : null,
            RegistryKey = string.IsNullOrEmpty(registryKey) ? registryKey : null,
            ValueName = !string.IsNullOrEmpty(valueName) ? valueName : null,
            Required = !string.IsNullOrEmpty(required) ? bool.Parse(required) : null,
            Value = trueItemValue.Value,
            ValueType = trueItemValue.Type
        };

        var falseValueDefinition = new PolicyElementItemDefinition()
        {
            IdName = !string.IsNullOrEmpty(idName) ? idName : null,
            RegistryKey = string.IsNullOrEmpty(registryKey) ? registryKey : null,
            ValueName = !string.IsNullOrEmpty(valueName) ? valueName : null,
            Required = !string.IsNullOrEmpty(required) ? bool.Parse(required) : null,
            Value = falseItemValue.Value,
            ValueType = falseItemValue.Type
        };

        return [trueValueDefinition, falseValueDefinition];
    }

    private static PolicyElementDefinition ParseListType(XElement element)
    {
        var additive = element.Attribute("additive")?.Value;
        var explicitValue = element.Attribute("explicitValue")?.Value;

        return new PolicyElementDefinition()
        {
            IdName = element.Attribute("id")?.Value
                ?? throw new ArgumentNullException("List elements always should contain attribute id."),
            Type = PolicyElementType.LIST,
            RegistryKey = element.Attribute("key")?.Value,
            ValueName = element.Attribute("valueName")?.Value,
            ValuePrefix = element.Attribute("valuePrefix")?.Value,
            Additive = !string.IsNullOrEmpty(additive) ? bool.Parse(additive) : null,
            ExplicitValue = !string.IsNullOrEmpty(explicitValue) ? bool.Parse(explicitValue) : null,
            ClientExtension = element.Attribute("clientExtension")?.Value
        };
    }



    private static PolicyElementDefinition ParseDecimalType(XElement element)
    {
        var idName = element.Attribute("id")?.Value;
        var registryKey = element.Attribute("key")?.Value;
        var value = element.Attribute("value")?.Value;
        var valueName = element.Attribute("valueName")?.Value;
        var minValue = element.Attribute("minValue")?.Value;
        var maxValue = element.Attribute("maxValue")?.Value;
        var maxvalue = element.Attribute("maxvalue")?.Value;
        var required = element.Attribute("required")?.Value;
        var storeAsText = element.Attribute("storeAsText")?.Value;
        var clientExtension = element.Attribute("clientExtension")?.Value;

        return new PolicyElementDefinition()
        {
            IdName = !string.IsNullOrEmpty(idName) ? idName : null,
            RegistryKey = string.IsNullOrEmpty(registryKey) ? registryKey : null,
            Value = !string.IsNullOrEmpty(value) ? int.Parse(value) : null,
            ValueName = !string.IsNullOrEmpty(valueName) ? valueName : null,
            MinValue = !string.IsNullOrEmpty(minValue) ? long.Parse(minValue) : null,
            MaxValue = !string.IsNullOrEmpty(maxValue) ? long.Parse(maxValue) : null,
            Maxvalue = !string.IsNullOrEmpty(maxvalue) ? long.Parse(maxvalue) : null,
            Required = !string.IsNullOrEmpty(required) ? bool.Parse(required) : null,
            StoreAsText = !string.IsNullOrEmpty(storeAsText) ? bool.Parse(storeAsText) : null,
            ClientExtension = !string.IsNullOrEmpty(clientExtension) ? clientExtension : null
        };
    }

    private static PolicyElementDefinition ParseTextType(XElement element)
    {
        var idName = element.Attribute("id")?.Value;
        var registryKey = element.Attribute("key")?.Value;
        var valueName = element.Attribute("valueName")?.Value;
        var minValue = element.Attribute("minValue")?.Value;
        var maxValue = element.Attribute("maxValue")?.Value;
        var required = element.Attribute("required")?.Value;
        var maxLength = element.Attribute("maxLength")?.Value;
        var clientExtension = element.Attribute("clientExtension")?.Value;
        var expandable = element.Attribute("expandable")?.Value;

        return new PolicyElementDefinition()
        {
            IdName = !string.IsNullOrEmpty(idName) ? idName : null,
            RegistryKey = string.IsNullOrEmpty(registryKey) ? registryKey : null,
            ValueName = !string.IsNullOrEmpty(valueName) ? valueName : null,
            MinValue = !string.IsNullOrEmpty(minValue) ? long.Parse(minValue) : null,
            MaxValue = !string.IsNullOrEmpty(maxValue) ? long.Parse(maxValue) : null,
            Required = !string.IsNullOrEmpty(required) ? bool.Parse(required) : null,
            ClientExtension = !string.IsNullOrEmpty(clientExtension) ? clientExtension : null,
            MaxLength = !string.IsNullOrEmpty(maxLength) ? int.Parse(maxLength) : null,
            Expandable = !string.IsNullOrEmpty(expandable) ? bool.Parse(expandable) : null
        };
    }

    private static PolicyElementDefinition ParseMultiTextType(XElement element)
    {
        var registryKey = element.Attribute("key")?.Value;
        var required = element.Attribute("required")?.Value;
        var maxLength = element.Attribute("maxLength")?.Value;
        var maxStrings = element.Attribute("maxStrings")?.Value;

        return new PolicyElementDefinition()
        {
            IdName = element.Attribute("id")?.Value,
            RegistryKey = string.IsNullOrEmpty(registryKey) ? registryKey : null,
            ValueName = element.Attribute("valueName")?.Value,
            Required = !string.IsNullOrEmpty(required) ? bool.Parse(required) : null,
            MaxLength = !string.IsNullOrEmpty(maxLength) ? int.Parse(maxLength) : null,
            MaxStrings = string.IsNullOrEmpty(maxStrings) ? int.Parse(maxStrings) : null
        };
    }

    private static (PolicyElementItemValueType Type, string? Value) ParseItemValue(XElement item)
    {
        var type = item.Element(Ns + "value")?.Elements().FirstOrDefault();
        if (type == null)
        {
            throw new InvalidOperationException($"Polciy elements item always should contait value elements.");
        }

        return type.Name.LocalName.ToLowerInvariant() switch
        {
            "delete" => (PolicyElementItemValueType.DELETE, null),
            "decimal" => (PolicyElementItemValueType.DECIMAL, type.Attribute("value")?.Value),
            "string" => (PolicyElementItemValueType.STRING, type.Value),
            _ => throw new InvalidOperationException($"Has no elements item value type with name: {type.Name.LocalName.ToLowerInvariant()}")
        };
    }

    private static PolicyElementType ResolvePolicyElementType(string tagName)
    {
        return tagName.ToLowerInvariant() switch
        {
            "list" => PolicyElementType.LIST,
            "enum" => PolicyElementType.ENUM,
            "text" => PolicyElementType.TEXT,
            "multitext" => PolicyElementType.MULTITEXT,
            "boolean" => PolicyElementType.BOOLEAN,
            "decimal" => PolicyElementType.DECIMAL,
            _ => throw new NotImplementedException($"Has no definition for elements with type: {tagName}")
        };
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
