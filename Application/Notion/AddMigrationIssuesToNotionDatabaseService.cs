using System.Collections.Immutable;
using System.Text.RegularExpressions;
using JetbrainsSpaceToNotion.Domain;
using JetbrainsSpaceToNotion.Extension;
using Notion.Client;

namespace JetbrainsSpaceToNotion.Application.Notion;

internal class AddMigrationIssuesToNotionDatabaseService
{
    private static readonly Regex ImageAttachmentRegex = new(@"!\[.*\]\(/d/(.*)\?f=0\)");
    private readonly NotionClient _client;
    private readonly Dictionary<MigrationIssueId, string> _migrationPages = new();

    internal AddMigrationIssuesToNotionDatabaseService(string token)
    {
        _client = NotionClientFactory.Create(new ClientOptions { AuthToken = token });
    }

    internal void Execute(string rootPageId, string databaseTitle, IReadOnlyCollection<MigrationIssue> issues)
    {
        var projectNames = issues.Select(it => it.ProjectName).Distinct();
        var databaseId = CreateDatabase(rootPageId, databaseTitle, projectNames);
        var attachmentDatabaseId = CreateAttachmentDatabase(rootPageId);
        var migrationIssues = issues.ToImmutableDictionary(issue => issue.Id);

        migrationIssues.ForEach(it => TryCreatePage(it.Value, migrationIssues,
            new DatabaseParentInput { DatabaseId = databaseId },
            new DatabaseParentInput { DatabaseId = attachmentDatabaseId }));
    }

    private string CreateDatabase(string rootPageId, string databaseTitle, IEnumerable<string> projectNames)
    {
        var databaseId = _client.Databases.CreateAsync(new DatabasesCreateParameters
        {
            Parent = new ParentPageInput { PageId = rootPageId },
            Title = new List<RichTextBaseInput>
            {
                new RichTextTextInput { Text = new Text { Content = databaseTitle } }
            },
            Properties = NotionDatabasePropertiesService.GetSchemaCreationDictionary(projectNames)
        }).Result.Id;

        _client.Databases.UpdateAsync(databaseId,
            new DatabasesUpdateParameters
            {
                Properties = NotionDatabasePropertiesService.GetSchemaUpdateDictionary(databaseId)
            }).Wait();

        return databaseId;
    }

    private string CreateAttachmentDatabase(string rootPageId)
    {
        var databaseId = _client.Databases.CreateAsync(new DatabasesCreateParameters
        {
            Parent = new ParentPageInput { PageId = rootPageId },
            Title = new List<RichTextBaseInput>
            {
                new RichTextTextInput { Text = new Text { Content = "TempAttachmentRepository" } }
            },
            Properties = new Dictionary<string, IPropertySchema>
            {
                ["Title"] = new TitlePropertySchema { Title = new Dictionary<string, object>() }
            }
        }).Result.Id;

        return databaseId;
    }

    private void TryCreatePage(MigrationIssue issue,
        ImmutableDictionary<MigrationIssueId, MigrationIssue> migrationIssues, IPageParentInput databasePage,
        IPageParentInput attachmentDatabasePage)
    {
        if (_migrationPages.ContainsKey(issue.Id)) return;

        var parameter = new PagesCreateParameters
        {
            Properties = NotionDatabasePropertiesService.GetValueCreationDictionary(issue), Parent = databasePage
        };

        if (issue.Description is not null)
            parameter.Children = GetBlock(issue, attachmentDatabasePage);

        var pageId = _client.Pages.CreateAsync(parameter).Result.Id;

        _migrationPages.Add(issue.Id, pageId);

        issue.Comments.ForEach(comment => CreateComments(new ParentPageInput { PageId = pageId }, comment));

        if (issue.Parent is null) return;

        TryCreatePage(migrationIssues[issue.Parent.Id], migrationIssues, databasePage, attachmentDatabasePage);

        _client.Pages.UpdatePropertiesAsync(pageId,
            NotionDatabasePropertiesService.GetValueUpdateDictionary(_migrationPages[issue.Parent.Id])).Wait();
    }

    private void CreateComments(ParentPageInput parentPageInput, MigrationComment comment)
    {
        CreateComment(parentPageInput, comment.Text);

        comment.Attachments?.ForEach(attachment =>
        {
            var content = attachment switch
            {
                MigrationImageAttachment image => image.ImageUrl,
                MigrationFileAttachment file => file.FileUrl,
                MigrationVideoAttachment video => video.VideoUrl,
                _ => throw new ArgumentOutOfRangeException(nameof(attachment), attachment, null)
            };
            CreateComment(parentPageInput, content);
        });
    }

    private void CreateComment(ParentPageInput parentPageInput, string content)
    {
        _client.Comments.CreateAsync(new CreateCommentParameters
        {
            Parent = parentPageInput,
            RichText = new List<RichTextBaseInput>
            {
                new RichTextTextInput { Text = new Text { Content = content } }
            }
        }).Wait();
    }

    private IList<IBlock> GetBlock(MigrationIssue issue, IPageParentInput attachmentDatabasePage)
    {
        var descriptionMatches =
            ImageAttachmentRegex.Matches(issue.Description!).Select(it => it.Groups[1].Value).ToList();
        var descriptionSplit = ImageAttachmentRegex.Split(issue.Description!).Subtract(descriptionMatches);
        var imageDictionary = issue.Attachments.OfType<MigrationImageAttachment>()
            .ToDictionary(image => image.Id, it => it);
        var imageMatches = descriptionMatches.Select(it => imageDictionary[it].ImageUrl)
            .Select(it => GetAttachmentPage(it, attachmentDatabasePage));

        return descriptionSplit.Select(GetParagraph).Cross(imageMatches).ToList();
    }

    private IBlock GetAttachmentPage(string url, IPageParentInput attachmentDatabasePage)
    {
        var pageId = _client.Pages.CreateAsync(new PagesCreateParameters
        {
            Properties = new Dictionary<string, PropertyValue>
            {
                ["Title"] = new TitlePropertyValue
                {
                    Id = url,
                    Title = new List<RichTextBase>
                    {
                        new RichTextText { Text = new Text { Content = url } }
                    }
                }
            },
            Parent = attachmentDatabasePage
        }).Result.Id;

        return new LinkToPageBlock { LinkToPage = new PageParent { Type = ParentType.PageId, PageId = pageId } };
    }

    private static IBlock GetParagraph(string text)
    {
        return new ParagraphBlock
        {
            Paragraph = new ParagraphBlock.Info
            {
                RichText = new[] { new RichTextText { Text = new Text { Content = text.Trim() } } }
            }
        };
    }
}