using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RoMi.Presentation.Controls;

public enum TreeItemType
{
    Branch,
    Leaf
}

public class TreeItem : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private readonly Func<List<TreeItem>>? childrenFactory;
    private List<TreeItem>? children;

    public TreeItemType Type { get; set; }
    public MidiTableEntry MidiTableEntry { get; set; }

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

            return children ?? new List<TreeItem>();
        }
    }

    public bool AreChildrenLoaded => children != null;

    // Constructor for lazy loading
    public TreeItem(MidiTableEntry midiTableEntry, TreeItemType type, Func<List<TreeItem>>? childrenFactory = null)
    {
        MidiTableEntry = midiTableEntry;
        Type = type;
        this.childrenFactory = childrenFactory;
        children = null;
    }

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

