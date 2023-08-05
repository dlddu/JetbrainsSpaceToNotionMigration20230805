// See https://aka.ms/new-console-template for more information

using System.Collections.Immutable;
using JetbrainsSpaceToNotion.Application.JetbrainsSpace;
using JetbrainsSpaceToNotion.Application.Notion;
using JetbrainsSpaceToNotion.Gateway;

Console.WriteLine("Hello, World!");

var (jetbrainsUrl, jetbrainsSpaceToken, notionToken, notionRootPageId, notionDatabaseTitle) =
    ConfigurationGateway.GetConfiguration();

var migrationIssues = GetIssuesFromJetbrainsSpaceService.Execute(jetbrainsUrl, jetbrainsSpaceToken);

new AddMigrationIssuesToNotionDatabaseService(notionToken).Execute(notionRootPageId, notionDatabaseTitle,
    migrationIssues.ToImmutableList());

Console.WriteLine("Goodbye, World!");