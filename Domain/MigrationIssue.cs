namespace JetbrainsSpaceToNotion.Domain;

public record MigrationIssue(string ProjectName, MigrationParentIssue? Parent, MigrationIssueId Id, string Title,
    string? Description, IEnumerable<IMigrationAttachment> Attachments, IEnumerable<MigrationComment> Comments);