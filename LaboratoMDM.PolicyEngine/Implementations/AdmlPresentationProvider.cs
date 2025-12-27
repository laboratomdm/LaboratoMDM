using LaboratoMDM.PolicyEngine.Domain;
using LaboratoMDM.PolicyEngine.Utils;
using System.Xml;
public sealed class AdmlPresentationProvider
{
    private readonly string _admlFilePath;
    private readonly string _langCode;

    private static readonly HashSet<string> AllowedElementTypes = new()
    {
        "dropdownList", "text", "checkBox", "listBox", "textBox", "multiTextBox", "decimalTextBox"
    };

    public AdmlPresentationProvider(string admlFilePath)
    {
        _admlFilePath = admlFilePath;
        _langCode = AdmlUtils.ExtractLangCode(admlFilePath);
    }

    public AdmlSnapshot Parse()
    {
        var doc = new XmlDocument();
        doc.Load(_admlFilePath);

        var snapshot = new AdmlSnapshot
        {
            AdmlFile = Path.GetFileName(_admlFilePath),
            Presentations = ParsePresentations(doc)
        };

        return snapshot;
    }

    private List<PresentationEntity> ParsePresentations(XmlDocument doc)
    {
        var ns = doc.DocumentElement?.NamespaceURI ?? string.Empty;
        var nsManager = new XmlNamespaceManager(doc.NameTable);
        nsManager.AddNamespace("adml", ns);

        var presentations = new List<PresentationEntity>();
        var nodes = doc.SelectNodes("//adml:presentationTable/adml:presentation", nsManager);
        if (nodes == null) return presentations;

        foreach (XmlNode pNode in nodes)
        {
            var presentation = new PresentationEntity
            {
                PresentationId = pNode.Attributes?["id"]?.Value ?? string.Empty,
                Elements = ParseElements(pNode, nsManager)
            };
            presentations.Add(presentation);
        }

        return presentations;
    }

    private List<PresentationElementEntity> ParseElements(XmlNode pNode, XmlNamespaceManager nsManager)
    {
        var elements = new List<PresentationElementEntity>();
        var xpath = string.Join(" | ", AllowedElementTypes.Select(t => $"adml:{t}"));
        var elNodes = pNode.SelectNodes(xpath, nsManager);
        if (elNodes == null) return elements;

        foreach (XmlNode elNode in elNodes)
        {
            var type = elNode.LocalName; // LocalName игнорирует префикс
            var element = new PresentationElementEntity
            {
                ElementType = type,
                RefId = elNode.Attributes?["refId"]?.Value,
                DefaultValue = elNode.Attributes?["defaultValue"]?.Value,
                DisplayOrder = int.TryParse(elNode.Attributes?["displayOrder"]?.Value, out var ord) ? ord : 0,
                Attributes = ParseAttributes(elNode, type),
                Translations = ParseTranslations(elNode, nsManager)
            };

            if (type == "textBox")
                element.Children.AddRange(ParseTextBoxChildren(elNode, element, nsManager));

            elements.Add(element);
        }

        return elements;
    }

    private List<PresentationTranslationEntity> ParseTranslations(XmlNode elNode, XmlNamespaceManager nsManager)
    {
        var list = new List<PresentationTranslationEntity>();

        // ищем textList/text (если есть)
        var texts = elNode.SelectNodes("adml:textList/adml:text", nsManager);
        if (texts != null && texts.Count > 0)
        {
            foreach (XmlNode tNode in texts)
            {
                var lang = tNode.Attributes?["lang"]?.Value;
                if (lang != _langCode) continue;

                list.Add(new PresentationTranslationEntity
                {
                    LangCode = lang,
                    TextValue = tNode.InnerText.Trim()
                });
            }
        }
        else
        {
            // fallback: берем InnerText текущей ноды (например label)
            if (!string.IsNullOrWhiteSpace(elNode.InnerText))
            {
                list.Add(new PresentationTranslationEntity
                {
                    LangCode = _langCode,
                    TextValue = elNode.InnerText.Trim()
                });
            }
        }

        return list;
    }


    private List<PresentationElementEntity> ParseTextBoxChildren(XmlNode elNode, PresentationElementEntity parent, XmlNamespaceManager nsManager)
    {
        var children = new List<PresentationElementEntity>();

        var labels = elNode.SelectNodes("adml:label", nsManager);
        if (labels != null)
        {
            foreach (XmlNode labelNode in labels)
            {
                var child = new PresentationElementEntity
                {
                    ElementType = "label",
                    DisplayOrder = int.TryParse(labelNode.Attributes?["displayOrder"]?.Value, out var ord) ? ord : 0,
                    Translations = ParseTranslations(labelNode, nsManager),
                    ParentElementId = parent.Id
                };
                children.Add(child);
            }
        }

        var dv = elNode.SelectNodes("adml:defaultValue", nsManager);
        if (dv != null)
        {
            foreach (XmlNode dvNode in dv)
            {
                var child = new PresentationElementEntity
                {
                    ElementType = "defaultValue",
                    DisplayOrder = int.TryParse(dvNode.Attributes?["displayOrder"]?.Value, out var ord) ? ord : 0,
                    DefaultValue = dvNode.InnerText,
                    ParentElementId = parent.Id
                };
                children.Add(child);
            }
        }

        return children;
    }

    private List<PresentationElementAttributeEntity> ParseAttributes(XmlNode elNode, string type)
    {
        var list = new List<PresentationElementAttributeEntity>();
        if (elNode.Attributes == null) return list;

        foreach (XmlAttribute attr in elNode.Attributes)
        {
            var allowed = type switch
            {
                "dropdownList" => new[] { "refId", "noSort", "defaultItem", "space", "oSort", "nosort" },
                "text" => Array.Empty<string>(),
                "checkBox" => new[] { "refId", "defaultChecked", "noSort", "defaultItem" },
                "listBox" => new[] { "refId", "required" },
                "textBox" => new[] { "refId" },
                "multiTextBox" => new[] { "refId" },
                "decimalTextBox" => new[] { "refId", "defaultValue", "spinStep", "space", "spin" },
                _ => Array.Empty<string>()
            };

            if (allowed.Contains(attr.Name))
            {
                list.Add(new PresentationElementAttributeEntity
                {
                    Name = attr.Name,
                    Value = attr.Value
                });
            }
        }

        return list;
    }
}
