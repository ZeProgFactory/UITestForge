using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace UITestForge;

public class DevFlowAgent : INotifyPropertyChanged
{
   public event PropertyChangedEventHandler? PropertyChanged;
   private void OnPropertyChanged([CallerMemberName] string? name = null)
      => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

   [JsonPropertyName("id")]
   public string Id { get; set; } = string.Empty;

   private string _project = string.Empty;
   [JsonPropertyName("project")]
   public string Project { get => _project; set { _project = value; OnPropertyChanged(); } }

   private string _tfm = string.Empty;
   [JsonPropertyName("tfm")]
   public string Tfm { get => _tfm; set { _tfm = value; OnPropertyChanged(); } }

   private string _platform = string.Empty;
   [JsonPropertyName("platform")]
   public string Platform { get => _platform; set { _platform = value; OnPropertyChanged(); } }

   private string _appName = string.Empty;
   [JsonPropertyName("appName")]
   public string AppName { get => _appName; set { _appName = value; OnPropertyChanged(); } }

   private int _port;
   [JsonPropertyName("port")]
   public int Port { get => _port; set { _port = value; OnPropertyChanged(); } }

   private string _version = string.Empty;
   [JsonPropertyName("version")]
   public string Version { get => _version; set { _version = value; OnPropertyChanged(); } }

   private string _sessionId = string.Empty;
   [JsonPropertyName("sessionId")]
   public string SessionId { get => _sessionId; set { _sessionId = value; OnPropertyChanged(); } }

   private DateTimeOffset _connectedAt;
   [JsonPropertyName("connectedAt")]
   public DateTimeOffset ConnectedAt { get => _connectedAt; set { _connectedAt = value; OnPropertyChanged(); } }

   public string DisplayName => $"{AppName} ({Platform})";
}

[JsonSerializable(typeof(List<DevFlowAgent>))]
[JsonSerializable(typeof(List<TreeNode>))]
[JsonSerializable(typeof(List<ElementInfo>))]
[JsonSerializable(typeof(TapRequest))]
[JsonSerializable(typeof(ActionResponse))]
internal partial class DevFlowJsonContext : JsonSerializerContext { }
