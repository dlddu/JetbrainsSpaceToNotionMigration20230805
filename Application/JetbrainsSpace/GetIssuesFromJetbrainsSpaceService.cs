using JetBrains.Space.Client;
using JetBrains.Space.Client.ChannelItemRecordPartialBuilder;
using JetBrains.Space.Client.GetMessagesResponsePartialBuilder;
using JetBrains.Space.Client.IssuePartialBuilder;
using JetBrains.Space.Common;
using JetbrainsSpaceToNotion.Domain;
using JetbrainsSpaceToNotion.Extension;

namespace JetbrainsSpaceToNotion.Application.JetbrainsSpace;

internal static class GetIssuesFromJetbrainsSpaceService
{
    internal static IEnumerable<MigrationIssue> Execute(string url, string token)
    {
        var uri = new Uri(url);
        var authTokens = new AuthenticationTokens(token);
        var connection = new BearerTokenConnection(uri, authTokens);

        var projectClient = new ProjectClient(connection);
        var chatClient = new ChatClient(connection);

        var projects = projectClient.GetAllProjectsAsyncEnumerable().ToBlockingEnumerable() ??
                       throw new Exception("프로젝트 목록을 조회할 수 없습니다.");

        var issues = projects.SelectMany(project => projectClient.Planning.Issues
            .GetAllIssuesAsyncEnumerable(ProjectIdentifier.Key(project.Key.Key), IssuesSorting.CREATED, false,
                partial: _ =>
                    _.WithId().WithTitle().WithDescription().WithAttachments().WithSubItemsList().WithParents())
            .ToBlockingEnumerable().Select(issue => new { project.Name, issue }));

        return issues.Select(args =>
        {
            var projectName = args.Name;
            var issue = args.issue;

            var attachments = issue.Attachments.Select(it => ToMigrationAttachment(it, url));

            var comments = chatClient.Messages
                .GetChannelMessagesAsync(ChannelIdentifier.Issue(IssueIdentifier.Id(issue.Id)),
                    MessagesSorting.FromOldestToNewest, 16,
                    partial: _ => _.WithMessages(partial => partial.WithText().WithAttachments().WithDetails())).Result
                .Messages.Where(it => it.Details is M2TextItemContent).Select(comment =>
                    new MigrationComment(comment.Text,
                        comment.Attachments?.Select(it => ToMigrationAttachment(it, url))));

            var parentIssue = issue.Parents.SingleOrDefault();

            var migrationIssue = new MigrationIssue(projectName,
                parentIssue?.Pipe(it => new MigrationParentIssue(new MigrationIssueId(it.Id), it.Title)),
                new MigrationIssueId(issue.Id), issue.Title, issue.Description, attachments, comments);

            return migrationIssue;
        });
    }

    private static IMigrationAttachment ToMigrationAttachment(AttachmentInfo attachmentInfo, string baseUrl)
    {
        return attachmentInfo.Details switch
        {
            FileAttachment fileAttachment => new MigrationFileAttachment($"{baseUrl}/d/{fileAttachment.Id}",
                fileAttachment.Filename),
            ImageAttachment imageAttachment => new MigrationImageAttachment(imageAttachment.Id,
                $"{baseUrl}/d/{imageAttachment.Id}"),
            VideoAttachment videoAttachment => new MigrationVideoAttachment($"{baseUrl}/d/{videoAttachment.Id}",
                videoAttachment.Name ?? string.Empty),
            _ => throw new Exception("알 수 없는 Attachment입니다.")
        };
    }
}