namespace JetbrainsSpaceToNotion.Application;

internal record Configuration(string JetbrainsUrl, string JetbrainsSpaceToken, string NotionToken,
    string NotionRootPageId, string NotionDatabaseTitle);