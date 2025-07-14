using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace XFramework
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum UILayerType
    {
        Background = -1000,
        Default = 0,
        Popup = 1000,
        Info = 2000,
        Top = 3000,
    }
}
