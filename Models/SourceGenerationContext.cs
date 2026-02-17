using SC_TranslationSetup.Models;
using System.Text.Json.Serialization;

namespace SC_TranslationSetup
{
    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(RootObject))]
    internal partial class SourceGenerationContext : JsonSerializerContext
    {
    }
}
