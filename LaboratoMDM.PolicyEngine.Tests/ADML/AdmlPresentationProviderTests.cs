using System.IO;
using System.Threading.Tasks;
using LaboratoMDM.PolicyEngine.Domain;
using LaboratoMDM.PolicyEngine.Implementations;
using Xunit;

namespace LaboratoMDM.PolicyEngine.Tests.ADML;

public class AdmlPresentationProviderTests
{
    private const string XmlContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<policyDefinitionResources xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" revision=""1.0"" schemaVersion=""1.0"" xmlns=""http://schemas.microsoft.com/GroupPolicy/2006/07/PolicyDefinitions"">
  <displayName>введите отображаемое имя</displayName>
  <description>введите описание здесь</description>
  <resources>
    <stringTable></stringTable>
    <presentationTable>
      <presentation id=""AllowTelemetry"">
        <dropdownList refId=""AllowTelemetry"" noSort=""true"" defaultItem=""1"" />
      </presentation>
      <presentation id=""TelemetryProxy"">
        <textBox refId=""TelemetryProxyName"">
          <label>Имя прокси-сервера:</label>
        </textBox>
      </presentation>
      <presentation id=""CommercialID"">
        <textBox refId=""CommercialIdValue"">
          <label>Коммерческий идентификатор:</label>
        </textBox>
      </presentation>
      <presentation id=""DisableEnterpriseAuthProxy"">
        <dropdownList refId=""DisableEnterpriseAuthProxy"" noSort=""true"" defaultItem=""1"" />
      </presentation>
      <presentation id=""ConfigureMicrosoft365UploadEndpoint"">
        <textBox refId=""ConfigureMicrosoft365UploadEndpointValue"">
          <label>Настраиваемая конечная точка для загрузки Аналитики компьютеров:</label>
        </textBox>
      </presentation>
    </presentationTable>
  </resources>
</policyDefinitionResources>";

    [Fact]
    public void Parse_ShouldReturnCorrectSnapshot()
    {
        // Arrange: создаем временный файл с ADML
        var tempDir = Path.Combine(Path.GetTempPath(), "ru-RU");
        Directory.CreateDirectory(tempDir);

        var tempFile = Path.Combine(tempDir, "test.adml");
        File.WriteAllText(tempFile, XmlContent);

        var provider = new AdmlPresentationProvider(tempFile);

        // Act
        var snapshot = provider.Parse();

        // Assert
        Assert.NotNull(snapshot);
        Assert.Equal(Path.GetFileName(tempFile), snapshot.AdmlFile);
        Assert.Equal(5, snapshot.Presentations.Count);

        // Проверяем первый presentation: AllowTelemetry
        var allowTelemetry = snapshot.Presentations[0];
        Assert.Equal("AllowTelemetry", allowTelemetry.PresentationId);
        Assert.Single(allowTelemetry.Elements);

        var dropdown = allowTelemetry.Elements[0];
        Assert.Equal("dropdownList", dropdown.ElementType);
        Assert.Equal("AllowTelemetry", dropdown.RefId);
        Assert.Contains(dropdown.Attributes, a => a.Name == "noSort" && a.Value == "true");
        Assert.Contains(dropdown.Attributes, a => a.Name == "defaultItem" && a.Value == "1");

        // Проверяем TelemetryProxy textBox с label
        var telemetryProxy = snapshot.Presentations[1];
        Assert.Equal("TelemetryProxy", telemetryProxy.PresentationId);
        Assert.Single(telemetryProxy.Elements);
        var textBox = telemetryProxy.Elements[0];
        Assert.Equal("textBox", textBox.ElementType);
        Assert.Equal("TelemetryProxyName", textBox.RefId);
        Assert.Single(textBox.Children);
        var label = textBox.Children[0];
        Assert.Equal("label", label.ElementType);
        Assert.Equal("Имя прокси-сервера:", label.Translations[0].TextValue);

        // Clean up
        File.Delete(tempFile);
    }
}
