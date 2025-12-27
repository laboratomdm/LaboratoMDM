#nullable enable
using LaboratoMDM.Core.Models.Policy;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Xml;
using System.Xml.Linq;

namespace LaboratoMDM.PolicyEngine.Implementations;

public sealed class AdmlTranslationProvider : ITranslationProvider
{
    private readonly ILogger _logger;
    private readonly string _admlFilePath;
    private readonly string _admlFileName;

    private readonly Dictionary<(string Id, string Lang), Translation> _translations =
        new();

    public AdmlTranslationProvider(
        string admlFilePath,
        ILogger logger)
    {
        _admlFilePath = admlFilePath;
        _admlFileName = Path.GetFileNameWithoutExtension(admlFilePath);
        _logger = logger;

        LoadInternal();
    }

    public IReadOnlyList<Translation> LoadTranslations() =>
        _translations.Values.ToList();

    public Translation? FindPolicy(string id, string lang) =>
        _translations.TryGetValue((id, lang), out var t) ? t : null;

    private void LoadInternal()
    {
        _logger.LogInformation("Loading ADML file {File}", _admlFilePath);

        if (!File.Exists(_admlFilePath))
            throw new FileNotFoundException("ADML file not found", _admlFilePath);

        var doc = LoadXmlSafely(_admlFilePath);
        var langCode = ExtractLangCode(_admlFilePath);

        ParseStringTable(doc, langCode);
        ParseResources(doc, langCode);

        _logger.LogInformation(
            "Loaded {Count} translations from {File}",
            _translations.Count,
            _admlFilePath);
    }

    private void ParseStringTable(XDocument doc, string lang)
    {
        var table = doc.Descendants("stringTable").SingleOrDefault();
        if (table == null)
            return;

        foreach (var s in table.Elements("string"))
        {
            var id = (string?)s.Attribute("id");
            if (!IsValidStringId(id))
                continue;

            AddTranslation(id!, lang, s.Value);
        }
    }

    private void ParseResources(XDocument doc, string lang)
    {
        var resources = doc.Descendants("resources").SingleOrDefault();
        if (resources == null)
            return;

        foreach (var s in resources.Elements("string"))
        {
            var id = (string?)s.Attribute("id");
            if (!IsValidStringId(id))
                continue;

            AddTranslation(id!, lang, s.Value);
        }
    }

    private void ExtractFromElement(XElement el, string lang)
    {
        var refId = (string?)el.Attribute("refId");
        if (IsValidStringId(refId))
        {
            AddTranslation(refId!, lang, el.Value);
        }

        var valuePrefix = (string?)el.Attribute("valuePrefix");
        if (IsValidStringId(valuePrefix))
        {
            AddTranslation(valuePrefix!, lang, el.Value);
        }
    }

    private void AddTranslation(string id, string lang, string value)
    {
        var key = (id, lang);

        if (_translations.ContainsKey(key))
            return;

        _translations[key] = new Translation
        {
            StringId = id,
            LangCode = lang,
            TextValue = CleanText(value),
            AdmlFilename = _admlFileName
        };
    }

    private static XDocument LoadXmlSafely(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            using var reader = XmlReader.Create(stream, new XmlReaderSettings
            {
                IgnoreComments = true,
                IgnoreWhitespace = true,
                IgnoreProcessingInstructions = true
            });

            return XDocument.Load(reader);
        }
        catch (XmlException ex)
        {
            throw new InvalidDataException($"Invalid XML in ADML file: {path}", ex);
        }
    }

    private static string ExtractLangCode(string admlFilePath)
    {
        var directory = Path.GetDirectoryName(admlFilePath)
            ?? throw new InvalidOperationException(
                $"Cannot determine directory for ADML file: {admlFilePath}");

        var langCode = Path.GetFileName(directory);

        if (string.IsNullOrWhiteSpace(langCode))
            throw new InvalidOperationException(
                $"Cannot determine language code from path: {admlFilePath}");

        // Проверяем, что это валидная locale
        try
        {
            _ = CultureInfo.GetCultureInfo(langCode);
            return langCode;
        }
        catch (CultureNotFoundException)
        {
            throw new InvalidOperationException(
                $"Directory '{langCode}' is not a valid culture name for ADML file '{admlFilePath}'");
        }
    }

    private static bool IsValidStringId(string? id) =>
        !string.IsNullOrWhiteSpace(id) &&
        id.Length <= 255 &&
        !id.Contains('\0');

    private static string CleanText(string text) =>
        string.IsNullOrWhiteSpace(text)
            ? string.Empty
            : text.Trim()
                .Replace("\r\n", " ")
                .Replace("\n", " ")
                .Replace("\t", " ")
                .Trim();
}
