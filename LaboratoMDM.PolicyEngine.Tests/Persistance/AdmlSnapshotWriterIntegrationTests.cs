using LaboratoMDM.PolicyEngine.Domain;
using LaboratoMDM.PolicyEngine.Persistence;
using LaboratoMDM.PolicyEngine.Tests.Support;
using Microsoft.Data.Sqlite;

namespace LaboratoMDM.PolicyEngine.Tests.Persistence;

public class AdmlSnapshotWriterIntegrationTests
    : IClassFixture<SqliteFileSchemaFixture>
{
    private readonly SqliteConnection _conn;
    private readonly AdmlSnapshotWriter _writer;
    private readonly PresentationQueryRepository _queryRepo;

    public AdmlSnapshotWriterIntegrationTests(SqliteFileSchemaFixture fixture)
    {
        _conn = fixture.Connection;
        _writer = new AdmlSnapshotWriter(_conn);
        _queryRepo = new PresentationQueryRepository(_conn);
    }

    [Fact]
    public async Task SaveSnapshot_ShouldPersistFullPresentationGraph()
    {
        var snapshot = CreateTestSnapshot();

        await _writer.SaveSnapshot(snapshot);

        await AssertDatabaseCountsAsync(
            presentations: 1,
            elements: 2,
            labelsWithParent: 1,
            attributes: 1,
            translations: 3);

        await AssertPresentationGraphAsync(snapshot.Presentations[0].PresentationId);
    }

    [Fact]
    public async Task GetSnapshot_ShouldLoadFullGraph()
    {
        // Сначала сохраняем snapshot, чтобы проверить загрузку
        var snapshot = CreateTestSnapshot();
        await _writer.SaveSnapshot(snapshot);

        var loadedSnapshot = await _queryRepo.GetSnapshotAsync("test.adml");
        Assert.NotNull(loadedSnapshot);
        Assert.Single(loadedSnapshot.Presentations);

        AssertPresentationGraphLoaded(loadedSnapshot.Presentations[0]);
    }

    // ------------------ приватные вспомогательные методы ------------------

    private AdmlSnapshot CreateTestSnapshot()
    {
        var snapshot = new AdmlSnapshot
        {
            AdmlFile = "test.adml",
            Presentations =
            [
                new PresentationEntity
                {
                    PresentationId = "TestPresentation",
                    Elements =
                    [
                        new PresentationElementEntity
                        {
                            ElementType = PresentationElementTypes.TextBox,
                            RefId = "Policy_Value_1",
                            DisplayOrder = 0,
                            Attributes =
                            [
                                new PresentationElementAttributeEntity
                                {
                                    Name = "required",
                                    Value = "true"
                                }
                            ],
                            Translations =
                            [
                                new PresentationTranslationEntity
                                {
                                    LangCode = "en-US",
                                    TextValue = "Enter value"
                                }
                            ],
                            Children =
                            [
                                new PresentationElementEntity
                                {
                                    ElementType = "label",
                                    DisplayOrder = 0,
                                    Translations =
                                    [
                                        new PresentationTranslationEntity
                                        {
                                            LangCode = "en-US",
                                            TextValue = "Test label"
                                        },
                                        new PresentationTranslationEntity
                                        {
                                            LangCode = "ru-RU",
                                            TextValue = "Тестовая метка"
                                        }
                                    ]
                                }
                            ]
                        }
                    ]
                }
            ]
        };

        // Вручную ParentElementId (будет резолвиться writer’ом)
        var parent = snapshot.Presentations[0].Elements[0];
        var label = parent.Children[0];
        label.ParentElementId = parent.Id;

        return snapshot;
    }

    private async Task AssertDatabaseCountsAsync(
        long presentations,
        long elements,
        long labelsWithParent,
        long attributes,
        long translations)
    {
        await using var cmd = _conn.CreateCommand();

        cmd.CommandText = "SELECT COUNT(*) FROM Presentations";
        Assert.Equal(presentations, (long)(await cmd.ExecuteScalarAsync())!);

        cmd.CommandText = "SELECT COUNT(*) FROM PresentationElements";
        Assert.Equal(elements, (long)(await cmd.ExecuteScalarAsync())!);

        cmd.CommandText = """
            SELECT COUNT(*)
            FROM PresentationElements
            WHERE ElementType = 'label'
              AND ParentElementId IS NOT NULL
        """;
        Assert.Equal(labelsWithParent, (long)(await cmd.ExecuteScalarAsync())!);

        cmd.CommandText = "SELECT COUNT(*) FROM PresentationElementAttributes";
        Assert.Equal(attributes, (long)(await cmd.ExecuteScalarAsync())!);

        cmd.CommandText = "SELECT COUNT(*) FROM PresentationTranslations";
        Assert.Equal(translations, (long)(await cmd.ExecuteScalarAsync())!);
    }

    private async Task AssertPresentationGraphAsync(string presentationId)
    {
        var loadedPresentation = await _queryRepo.GetPresentationByIdAsync(presentationId);
        Assert.NotNull(loadedPresentation);
        Assert.Equal(presentationId, loadedPresentation!.PresentationId);
        Assert.Single(loadedPresentation.Elements);

        AssertPresentationGraphLoaded(loadedPresentation);
    }

    private void AssertPresentationGraphLoaded(PresentationEntity presentation)
    {
        var parent = presentation.Elements[0];
        Assert.Equal(PresentationElementTypes.TextBox, parent.ElementType);
        Assert.Single(parent.Attributes);
        Assert.Single(parent.Translations);
        Assert.Single(parent.Children);

        var label = parent.Children[0];
        Assert.Equal("label", label.ElementType);
        Assert.Equal(2, label.Translations.Count);
        Assert.Contains(label.Translations, t => t.LangCode == "en-US" && t.TextValue == "Test label");
        Assert.Contains(label.Translations, t => t.LangCode == "ru-RU" && t.TextValue == "Тестовая метка");
    }
}
