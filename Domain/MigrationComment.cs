namespace JetbrainsSpaceToNotion.Domain;

public record MigrationComment(string Text, IEnumerable<IMigrationAttachment>? Attachments);