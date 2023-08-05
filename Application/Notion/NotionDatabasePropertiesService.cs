using JetbrainsSpaceToNotion.Domain;
using Newtonsoft.Json;
using Notion.Client;

namespace JetbrainsSpaceToNotion.Application.Notion;

internal class NotionDatabasePropertiesService
{
    private const string TitlePropertyName = "제목";
    private const string ProductPropertyName = "제품";
    private const string ParentPropertyName = "부모";

    internal static Dictionary<string, IPropertySchema> GetSchemaCreationDictionary(IEnumerable<string> projectNames)
    {
        return new Dictionary<string, IPropertySchema>
        {
            [TitlePropertyName] = new TitlePropertySchema { Title = new Dictionary<string, object>() },
            [ProductPropertyName] = new SelectPropertySchema
            {
                Select = new OptionWrapper<SelectOptionSchema>
                {
                    Options = projectNames.Select(it => new SelectOptionSchema { Name = it }).ToList()
                }
            }
        };
    }

    internal static Dictionary<string, IUpdatePropertySchema> GetSchemaUpdateDictionary(string databaseId)
    {
        return new Dictionary<string, IUpdatePropertySchema>
        {
            [ParentPropertyName] = new DualRelationPropertySchema
            {
                Relation = new DualRelationPropertySchema.RelationInfo { DatabaseId = new Guid(databaseId) }
            }
        };
    }

    internal static Dictionary<string, PropertyValue> GetValueCreationDictionary(MigrationIssue issue)
    {
        return new Dictionary<string, PropertyValue>
        {
            [TitlePropertyName] =
                new TitlePropertyValue
                {
                    Id = issue.Id.Value,
                    Title = new List<RichTextBase>
                    {
                        new RichTextText { Text = new Text { Content = issue.Title } }
                    }
                },
            [ProductPropertyName] =
                new SelectPropertyValue { Select = new SelectOption { Name = issue.ProjectName } }
        };
    }

    internal static Dictionary<string, PropertyValue> GetValueUpdateDictionary(string parentId)
    {
        return new Dictionary<string, PropertyValue>
        {
            [ParentPropertyName] =
                new RelationPropertyValue { Relation = new List<ObjectId> { new() { Id = parentId } } }
        };
    }

    private class DualRelationPropertySchema : IUpdatePropertySchema
    {
        [JsonProperty("relation")] public RelationInfo Relation { get; set; } = null!;

        public string Name { get; set; } = null!;
        public PropertyType? Type { get; set; }

        public class RelationInfo
        {
            [JsonProperty("dual_property")] public object DualProperty = new();
            [JsonProperty("database_id")] public Guid DatabaseId { get; set; }
        }
    }
}