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
    public TreeItem? Parent { get; private set; }

    // Lazy-loading property for children
    public List<TreeItem> Children
    {
        get
        {
            LoadChildren();
            return children ?? [];
        }
    }

    public bool AreChildrenLoaded => children != null;

    private readonly Lock loadLock = new();
    // Method for explicitly loading children
    public void LoadChildren()
    {
        // Called from the UI thread (expanding) and from a background thread (selection)
        lock (loadLock)
        {
            if (children == null && childrenFactory != null)
            {
                List<TreeItem> loaded = childrenFactory();

                foreach (TreeItem child in loaded)
                {
                    child.Parent = this;
                }

                children = loaded;
            }
        }
    }

    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
