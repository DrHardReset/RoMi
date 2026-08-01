using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RoMi.Models;

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(List<LibraryInfo>))]
[JsonSerializable(typeof(LibraryInfo))]
[JsonSerializable(typeof(List<ValidationError>))]
[JsonSerializable(typeof(ValidationError))]
internal partial class DependencyReportJsonContext : JsonSerializerContext { }

public class DependencyReport
{
    private const string filePath = $"{nameof(RoMi)}.Assets.LibraryDependencyReport.json";

    public List<LibraryInfo> Packages { get; set; } = [];

    public static async Task<DependencyReport?> Read()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        using Stream stream = assembly.GetManifestResourceStream(filePath)
            ?? throw new Exception("Library dependency report resource file could not be found.");

        List<LibraryInfo>? packages = await JsonSerializer.DeserializeAsync(
            stream,
            DependencyReportJsonContext.Default.ListLibraryInfo);

        return new DependencyReport
        {
            Packages = packages ?? []
        };
    }

    public List<LibraryInfo> GetAllDistinctPackages()
    {
        List<LibraryInfo> list = [];

        foreach (LibraryInfo package in Packages)
        {
            if (!string.IsNullOrWhiteSpace(package.PackageId) && !list.Contains(package))
            {
                list.Add(package);
            }
        }

        list.Sort((x, y) => string.Compare(x.PackageId, y.PackageId, StringComparison.OrdinalIgnoreCase));

        return list;
    }
}

public class LibraryInfo : IEquatable<LibraryInfo>
{
    public string PackageId { get; set; } = string.Empty;
    public string PackageVersion { get; set; } = string.Empty;
    public string? PackageProjectUrl { get; set; }
    public string? Copyright { get; set; }
    public string? Authors { get; set; }
    public string? Description { get; set; }
    public string? License { get; set; }
    public string? LicenseUrl { get; set; }
    public List<ValidationError> ValidationErrors { get; set; } = [];
    public int LicenseInformationOrigin { get; set; }

    /// <summary>
    /// True if nuget-license could resolve a license without any validation errors.
    /// </summary>
    public bool HasLicenseInfo => !string.IsNullOrWhiteSpace(License) && ValidationErrors.Count == 0;

    /// <summary>
    /// Fallback text for the UI when no license could be determined for this package.
    /// </summary>
    public string LicenseDisplay
    {
        get
        {
            if (!HasLicenseInfo)
            {
                return "Unknown license";
            }

            return Uri.IsWellFormedUriString(License, UriKind.Absolute) ? "View License" : License!;
        }
    }

    public bool Equals(LibraryInfo? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return string.Equals(PackageId, other.PackageId, StringComparison.OrdinalIgnoreCase)
            && string.Equals(PackageVersion, other.PackageVersion, StringComparison.OrdinalIgnoreCase);
    }

    public override bool Equals(object? obj) => Equals(obj as LibraryInfo);

    public override int GetHashCode() =>
        HashCode.Combine(PackageId?.ToLowerInvariant(), PackageVersion?.ToLowerInvariant());

    public static bool operator ==(LibraryInfo? left, LibraryInfo? right) => Equals(left, right);
    public static bool operator !=(LibraryInfo? left, LibraryInfo? right) => !Equals(left, right);
}

public class ValidationError
{
    public string Error { get; set; } = string.Empty;
    public string Context { get; set; } = string.Empty;
}
