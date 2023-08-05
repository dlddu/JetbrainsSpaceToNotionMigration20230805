namespace JetbrainsSpaceToNotion.Domain;

public interface IMigrationAttachment
{
}

public record MigrationImageAttachment(string Id, string ImageUrl) : IMigrationAttachment;

public record MigrationFileAttachment(string FileUrl, string FileName) : IMigrationAttachment;

public record MigrationVideoAttachment(string VideoUrl, string FileName) : IMigrationAttachment;