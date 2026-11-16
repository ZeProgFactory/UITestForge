// ── Tree JSON models ─────────────────────────────────────────────────────

using System.Text.Json.Serialization;

namespace UITestForge;

public class TreeNode
{
   [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
   [JsonPropertyName("parentId")] public string? ParentId { get; set; }
   [JsonPropertyName("type")] public string Type { get; set; } = string.Empty;
   [JsonPropertyName("fullType")] public string FullType { get; set; } = string.Empty;
   [JsonPropertyName("automationId")] public string? AutomationId { get; set; }
   [JsonPropertyName("text")] public string? Text { get; set; }
   [JsonPropertyName("isVisible")] public bool IsVisible { get; set; }
   [JsonPropertyName("isEnabled")] public bool IsEnabled { get; set; }
   [JsonPropertyName("isFocused")] public bool IsFocused { get; set; }
   [JsonPropertyName("opacity")] public double Opacity { get; set; }
   [JsonPropertyName("bounds")] public NodeBounds? Bounds { get; set; }
   [JsonPropertyName("windowBounds")] public NodeBounds? WindowBounds { get; set; }
   [JsonPropertyName("state")] public NodeState? State { get; set; }
   [JsonPropertyName("traits")] public List<string>? Traits { get; set; }
   [JsonPropertyName("children")] public List<TreeNode>? Children { get; set; }
}

public class NodeBounds
{
   [JsonPropertyName("x")] public double X { get; set; }
   [JsonPropertyName("y")] public double Y { get; set; }
   [JsonPropertyName("width")] public double Width { get; set; }
   [JsonPropertyName("height")] public double Height { get; set; }
}

public class NodeState
{
   [JsonPropertyName("displayed")] public bool Displayed { get; set; }
   [JsonPropertyName("enabled")] public bool Enabled { get; set; }
   [JsonPropertyName("selected")] public bool Selected { get; set; }
   [JsonPropertyName("focused")] public bool Focused { get; set; }
   [JsonPropertyName("opacity")] public double Opacity { get; set; }
}
