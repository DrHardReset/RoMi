using Microsoft.UI.Xaml.Input;

namespace RoMi.Presentation.Controls;

public sealed partial class MidiIoTreeControl : UserControl, IDisposable
{
    private bool disposed = false;
    private readonly MidiIoTreeControlViewModel? viewModel;
    private readonly Dictionary<TreeItem, bool> expansionState = [];

    public MidiIoTreeControl(MidiIoTreeControlViewModel viewModel)
    {
        this.viewModel = viewModel;
        InitializeComponent();
        DataContext = this.viewModel;

        // We need to do the event handling ourself, as otherwise expanding a tree item would need 2 taps
        TreeViewControl.Tapped += TreeViewControl_Tapped;
        TreeViewControl.Expanding += TreeViewControl_Expanding;

        // Add selection changed handler to properly handle checkbox clicks
        TreeViewControl.SelectionChanged += TreeViewControl_SelectionChanged;
    }

    // Fallback constructor for Designer and XAML-Preview
    public MidiIoTreeControl()
    {
        InitializeComponent();
    }

    private void TreeViewControl_Tapped(object sender, TappedRoutedEventArgs e)
    {
        // Klicks auf die Selektions-Checkbox nicht als Expand behandeln
        if (MidiIoTreeControl.FindAncestor<CheckBox>(e.OriginalSource as DependencyObject) != null)
        {
            return;
        }

        // Find the element that was clicked
        var treeViewItem = MidiIoTreeControl.FindAncestor<TreeViewItem>(e.OriginalSource as FrameworkElement);
        
        if (treeViewItem?.Content is not TreeItem treeItem || treeItem.Type != TreeItemType.Branch)
        {
            return;
        }

        // Mark as handled to prevent default behavior
        e.Handled = true;

        // Toggle expansion state and update UI
        bool isExpanding = ToggleExpansionState(treeItem);
        treeViewItem.IsExpanded = isExpanding;

        // Load children if necessary
        if (isExpanding && !treeItem.AreChildrenLoaded)
        {
            if (FindNodeForTreeItem(TreeViewControl.RootNodes, treeItem) is TreeViewNode node)
            {
                LoadChildrenForTreeItem(treeItem, node);
            }
        }
    }

    private static T? FindAncestor<T>(DependencyObject? element) where T : DependencyObject
    {
        while (element != null)
        {
            if (element is T target)
            {
                return target;
            }

            element = VisualTreeHelper.GetParent(element);
        }
        return default;
    }

    private bool ToggleExpansionState(TreeItem treeItem)
    {
        if (!expansionState.TryGetValue(treeItem, out bool value))
        {
            value = false;
            expansionState[treeItem] = value;
        }

        expansionState[treeItem] = expansionState[treeItem] = !value;
        return expansionState[treeItem];
    }

    private static TreeViewNode? FindNodeForTreeItem(IList<TreeViewNode> nodes, TreeItem treeItem)
    {
        foreach (var node in nodes)
        {
            if (node.Content == treeItem)
            {
                return node;
            }

            if (node.HasChildren)
            {
                var result = FindNodeForTreeItem(node.Children, treeItem);

                if (result != null)
                {
                    return result;
                }
            }
        }

        return null;
    }

    private void TreeViewControl_Expanding(TreeView sender, TreeViewExpandingEventArgs args)
    {
        if (args.Node.Content is TreeItem treeItem)
        {
            LoadChildrenForTreeItem(treeItem, args.Node);
        }
    }

    private static TreeViewNode CreateTreeViewNode(TreeItem treeItem)
    {
        var node = new TreeViewNode { Content = treeItem };

        // Only for Branch elements
        if (treeItem.Type == TreeItemType.Branch)
        {
            if (treeItem.AreChildrenLoaded)
            {
                // Add already loaded children
                foreach (var child in treeItem.Children)
                {
                    node.Children.Add(CreateTreeViewNode(child));
                }
            }
            else
            {
                // show expansion icon
                node.HasUnrealizedChildren = true;
            }
        }

        return node;
    }

    private static void LoadChildrenForTreeItem(TreeItem treeItem, TreeViewNode node)
    {
        if (node.Children.Count > 0 && !node.HasUnrealizedChildren)
        {
            return;
        }

        treeItem.LoadChildren();

        // Update the children in the TreeView node
        for (int i = node.Children.Count - 1; i >= 0; i--)
        {
            if (node.Children[i].Content is null)
            {
                node.Children.RemoveAt(i);
            }
        }

        foreach (var child in treeItem.Children)
        {
            node.Children.Add(CreateTreeViewNode(child));
        }

        node.HasUnrealizedChildren = false;

    }

    private void TreeViewControl_SelectionChanged(TreeView sender, TreeViewSelectionChangedEventArgs e)
    {
        viewModel?.SetSelectedTreeItems(TreeViewControl.SelectedNodes.Select(n => n.Content).OfType<TreeItem>());
    }

    private void HelpButton_Click(object sender, RoutedEventArgs e)
    {
        MonitoringHelpTip.IsOpen = true;
    }

    public new void Dispose()
    {
        if (!disposed)
        {
            TreeViewControl.Tapped -= TreeViewControl_Tapped;
            TreeViewControl.Expanding -= TreeViewControl_Expanding;
            TreeViewControl.SelectionChanged -= TreeViewControl_SelectionChanged;

            viewModel?.Dispose();

            disposed = true;
        }
    }
}
