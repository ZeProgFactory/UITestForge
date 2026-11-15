using System.Text.Json;

namespace UITestForge.Tests;

[TestClass]
public sealed class TreeModelTests
{
    [TestMethod]
    public void TreeNode_DeserializesAgentPropertyNamesAndNestedNodes()
    {
        const string json = """
            {
              "id": "root",
              "type": "ContentPage",
              "fullType": "Microsoft.Maui.Controls.ContentPage",
              "isVisible": true,
              "state": { "displayed": true, "enabled": true },
              "children": [
                { "id": "button", "type": "Button", "automationId": "SaveBtn", "text": "Save" }
              ]
            }
            """;

        var node = JsonSerializer.Deserialize<TreeNode>(json);

        Assert.IsNotNull(node);
        Assert.AreEqual("root", node.Id);
        Assert.AreEqual("ContentPage", node.Type);
        Assert.IsTrue(node.State!.Displayed);
        Assert.AreEqual("SaveBtn", node.Children![0].AutomationId);
        Assert.AreEqual("Save", node.Children[0].Text);
    }

    [TestMethod]
    public void TreeNodeItem_UsesAutomationIdAndStateForDisplay()
    {
        var item = new TreeNodeItem
        {
            Node = new TreeNode
            {
                Id = "save",
                Type = "CustomButton",
                FullType = "Controls.CustomButton",
                AutomationId = "SaveBtn",
                IsVisible = true,
                State = new NodeState { Displayed = false, Enabled = true }
            },
            Depth = 2
        };

        Assert.AreEqual("#SaveBtn", item.DisplayLabel);
        Assert.AreEqual("🔘", item.TypeIcon);
        Assert.IsTrue(item.IsHiddenNode);
        Assert.IsFalse(item.HasChildren);
        Assert.AreEqual(" ", item.ExpandIcon);
    }

    [TestMethod]
    public void TreeNodeItem_UpdatesExpandIconAndNotifiesBindings()
    {
        var item = new TreeNodeItem
        {
            Node = new TreeNode { Type = "Grid", Children = [new TreeNode { Type = "Button" }] },
            Depth = 0
        };
        var changedProperties = new List<string?>();
        item.PropertyChanged += (_, args) => changedProperties.Add(args.PropertyName);

        item.IsExpanded = true;

        Assert.IsTrue(item.HasChildren);
        Assert.AreEqual("▾", item.ExpandIcon);
        CollectionAssert.AreEqual(new[] { "IsExpanded", "ExpandIcon" }, changedProperties);
    }
}
