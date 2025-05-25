using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RoMi.Presentation.Controls;

public enum TreeItemType
{
    Branch,
    Leaf
}

// class shall be partial for trimming and AOT compatibility
public partial class TreeItem(MidiTableEntry midiTableEntry, TreeItemType type, Func<List<TreeItem>>? childrenFactory = null) : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private readonly Func<List<TreeItem>>? childrenFactory = childrenFactory;
    private List<TreeItem>? children = null;

    public TreeItemType Type { get; set; } = type;
    public MidiTableEntry MidiTableEntry { get; set; } = midiTableEntry;

    private string receivedValue = string.Empty;
    public string ReceivedValue
    {
        get => receivedValue;
        set
        {
            receivedValue = value;
            OnPropertyChanged();
        }
    }

    // Lazy-loading property for children
    public List<TreeItem> Children
    {
        get
        {
            if (children == null && childrenFactory != null)
            {
                children = childrenFactory();
            }

            return children ?? [];
        }
    }

    public bool AreChildrenLoaded => children != null;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    // Method for explicitly loading children
    public void LoadChildren()
    {
        if (children == null && childrenFactory != null)
        {
            children = childrenFactory();
        }
    }
}

