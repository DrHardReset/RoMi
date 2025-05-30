namespace RoMi.Presentation.Controls;

// class shall be partial for trimming and AOT compatibility
public partial class TreeItemTypeTemplateSelector : DataTemplateSelector
{
    public DataTemplate? BranchTemplate { get; set; }
    public DataTemplate? LeafTemplate { get; set; }

    protected override DataTemplate? SelectTemplateCore(object item)
    {
        if (item is TreeItem treeItem)
        {
            return treeItem.Type switch
            {
                TreeItemType.Branch => BranchTemplate,
                TreeItemType.Leaf => LeafTemplate,
                _ => base.SelectTemplateCore(item)
            };
        }

        return base.SelectTemplateCore(item);
    }
}
