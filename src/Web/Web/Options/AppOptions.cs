// ReSharper disable InconsistentNaming

using System.ComponentModel.DataAnnotations;

namespace Web.Options;

public class AppOptions
{
    public const string SECTION_NAME = "APP_OPTIONS";

    [Required] public string APP_NAME { get; init; } = "cs-instance-web";
    [Required] public required string INSTANCE_API_ENDPOINT { get; init; }
}