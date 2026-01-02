using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace RoMi.Models;

[JsonSerializable(typeof(DependencyReport))]
[JsonSerializable(typeof(List<Problem>))]
[JsonSerializable(typeof(List<Project>))]
[JsonSerializable(typeof(Problem))]
[JsonSerializable(typeof(Project))]
[JsonSerializable(typeof(Framework))]
[JsonSerializable(typeof(TopLevelPackage))]
[JsonSerializable(typeof(List<Framework>))]
[JsonSerializable(typeof(List<TopLevelPackage>))]
internal partial class DependencyReportJsonContext : JsonSerializerContext { }

public class DependencyReport
{
    private const string filePath = $"{nameof(RoMi)}.Assets.LibraryDependencyReport.json";

    [JsonPropertyName("version")]
    public int Version { get; set; } = 0;
    [JsonPropertyName("parameters")]
    public string Parameters { get; set; } = string.Empty;
    [JsonPropertyName("problems")]
    public List<Problem> Problems { get; set; } = [];
    [JsonPropertyName("projects")]
    public List<Project> Projects { get; set; } = [];

    public static async Task<DependencyReport?> Read()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        using Stream stream = assembly.GetManifestResourceStream(filePath) ?? throw new Exception("Library dependency report resource file could not be found.");
        using StreamReader reader = new(stream);
        string fileContent = await reader.ReadToEndAsync();

        Match match = Regex.Match(fileContent, @"(\{[\s\S]*\})");

        if (!match.Success || match.Groups.Count < 2)
        {
            throw new Exception($"Library dependency report could not be parsed:\n{fileContent}");
        }

        string jsonString = match.Groups[1].Value;
        var jsonByte = Encoding.UTF8.GetBytes(jsonString);

        using MemoryStream memoryStream = new(jsonByte);
        return await JsonSerializer.DeserializeAsync(memoryStream, DependencyReportJsonContext.Default.DependencyReport);
    }

    public List<TopLevelPackage> GetAllDistinctTopLevelPackages()
    {
        List<TopLevelPackage> list = [];

        foreach (Project project in Projects)
        {
            foreach (Framework framework in project.Frameworks)
            {
                foreach (TopLevelPackage topLevelPackage in framework.TopLevelPackages)
                {
                    if (!list.Contains(topLevelPackage))
                    {
                        list.Add(topLevelPackage);
                    }
                }
            }
        }

        list.Sort(delegate (TopLevelPackage x, TopLevelPackage y)
        {
            if (x.Id == null && y.Id == null) return 0;
            else if (x.Id == null) return -1;
            else if (y.Id == null) return 1;
            else return x.Id.CompareTo(y.Id);
        });

        return list;
    }
}

public class Framework
{
    [JsonPropertyName("framework")]
    public string FrameworkName { get; set; } = string.Empty;
    [JsonPropertyName("topLevelPackages")]
    public List<TopLevelPackage> TopLevelPackages { get; set; } = [];
}

public class Problem
{
    [JsonPropertyName("level")]
    public string Level { get; set; } = string.Empty;
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}

public class Project
{
    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;
    [JsonPropertyName("frameworks")]
    public List<Framework> Frameworks { get; set; } = [];
}

public class TopLevelPackage
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    [JsonPropertyName("requestedVersion")]
    public string RequestedVersion { get; set; } = string.Empty;
    [JsonPropertyName("resolvedVersion")]
    public string ResolvedVersion { get; set; } = string.Empty;
    [JsonPropertyName("autoReferenced")]
    public string AutoReferenced { get; set; } = string.Empty;

    public bool Equals(TopLevelPackage? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(other, this))
        {
            return true;
        }

        return string.Equals(Id, other.Id) && string.Equals(ResolvedVersion, other.ResolvedVersion);
    }

    public override bool Equals(object? obj)
    {
        if (obj is null)
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        return Equals(obj as TopLevelPackage);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id, ResolvedVersion);
    }

    public static bool operator ==(TopLevelPackage left, TopLevelPackage right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(TopLevelPackage left, TopLevelPackage right)
    {
        return !Equals(left, right);
    }
}
