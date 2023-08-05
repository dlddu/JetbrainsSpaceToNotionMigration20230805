using JetbrainsSpaceToNotion.Application;
using Microsoft.Extensions.Configuration;

namespace JetbrainsSpaceToNotion.Gateway;

internal static class ConfigurationGateway
{
    internal static Configuration GetConfiguration()
    {
        var configuration = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json").AddJsonFile("appsettings.local.json", true).Build();

        var jetbrainsUrl = configuration.GetValue("jetbrains:url");
        var jetbrainsSpaceToken = configuration.GetValue("jetbrains:token");

        var notionToken = configuration.GetValue("notion:token");
        var notionRootPageId = configuration.GetValue("notion:rootPageId");
        var notionDatabaseTitle = configuration.GetValue("notion:databaseTitle");

        return new Configuration(jetbrainsUrl, jetbrainsSpaceToken, notionToken, notionRootPageId, notionDatabaseTitle);
    }

    private static string GetValue(this IConfiguration configuration, string key)
    {
        return configuration.GetRequiredSection(key).Value ??
               throw new Exception($"appsettings.*.json 파일에 {key}에 해당하는 값이 존재하지 않습니다");
    }
}