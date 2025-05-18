using Microsoft.UI.Xaml.Input;

namespace RoMi.Presentation.Controls;

public sealed partial class MidiIoTreeControl : UserControl, IDisposable
{
    private bool disposed = false;
    private readonly MidiIoTreeControlViewModel? viewModel;
    private readonly Dictionary<TreeItem, bool> expansionState = new();

    public MidiIoTreeControl(MidiIoTreeControlViewModel viewModel)
    {
        this.viewModel = viewModel;
        this.InitializeComponent();
        DataContextChanged += MidiIoTreeControl_DataContextChanged;
        this.DataContext = this.viewModel;

        // We need to do the event handling ourself, as otherwise expanding a tree item would need 2 taps
        TreeViewControl.Tapped += TreeViewControl_Tapped;
        TreeViewControl.Expanding += TreeViewControl_Expanding;

        // Add selection changed handler to properly handle checkbox clicks
        TreeViewControl.SelectionChanged += TreeViewControl_SelectionChanged;
    }

    // Fallback constructor for Designer and XAML-Preview
    public MidiIoTreeControl()
    {
        this.InitializeComponent();
    }

    private void MidiIoTreeControl_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs e)
    {
        if (e.NewValue is MidiIoTreeControlViewModel)
        {
            DataContextChanged -= MidiIoTreeControl_DataContextChanged;
            PopulateTreeView();
        }
    }

    private void PopulateTreeView()
    {
        if (viewModel == null)
        {
            return;
        }

        TreeViewControl.RootNodes.Clear();

        foreach (var treeItem in viewModel.TreeItems)
        {
            TreeViewControl.RootNodes.Add(CreateTreeViewNode(treeItem));
        }
    }

    private void TreeViewControl_Tapped(object sender, TappedRoutedEventArgs e)
    {
        // Find the element that was clicked
        var treeViewItem = FindAncestor<TreeViewItem>(e.OriginalSource as FrameworkElement);
        
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

    private T? FindAncestor<T>(DependencyObject? element) where T : DependencyObject
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
        if (!expansionState.ContainsKey(treeItem))
        {
            expansionState[treeItem] = false;
        }

        expansionState[treeItem] = !expansionState[treeItem];
        return expansionState[treeItem];
    }

    private TreeViewNode? FindNodeForTreeItem(IList<TreeViewNode> nodes, TreeItem treeItem)
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

    private TreeViewNode CreateTreeViewNode(TreeItem treeItem)
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
                // Add placeholder for the expansion icon
                node.Children.Add(new TreeViewNode());
            }
        }

        return node;
    }

    private void LoadChildrenForTreeItem(TreeItem treeItem, TreeViewNode node)
    {
        if (treeItem.AreChildrenLoaded)
        {
            return;
        }

        treeItem.LoadChildren();

        // Update the children in the TreeView node
        node.Children.Clear();

        foreach (var child in treeItem.Children)
        {
            node.Children.Add(CreateTreeViewNode(child));
        }
    }

    private void TreeViewControl_SelectionChanged(TreeView sender, TreeViewSelectionChangedEventArgs e)
    {
        if (viewModel == null)
        {
            return;
        }

        // Handle deselection first
        foreach (var item in e.RemovedItems)
        {
            if (item is TreeItem treeItem)
            {
                viewModel.SelectedTreeItem = null;
            }
        }

        foreach (var item in e.AddedItems)
        {
            if (item is TreeItem treeItem)
            {
                // Update the selected item in the view model
                viewModel.SelectedTreeItem = treeItem;
            }
        }
    }

    private void HelpButton_Click(object sender, RoutedEventArgs e)
    {
        MonitoringHelpTip.IsOpen = true;
    }

    public void Dispose()
    {
        if (!disposed)
        {
            DataContextChanged -= MidiIoTreeControl_DataContextChanged;
            TreeViewControl.Tapped -= TreeViewControl_Tapped;
            TreeViewControl.Expanding -= TreeViewControl_Expanding;
            TreeViewControl.SelectionChanged -= TreeViewControl_SelectionChanged;

            viewModel?.Dispose();

            disposed = true;
        }
    }
}
