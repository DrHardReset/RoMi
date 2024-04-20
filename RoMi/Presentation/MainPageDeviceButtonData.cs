namespace RoMi.Presentation;

public class MainPageDeviceButtonData
{
    public string Name { get; set; }
    public object MidiDocument { get; set; }
    public ICommand Command { get; set; }

    public MainPageDeviceButtonData(string name, MidiDocument midiDocument, ICommand command)
    {
        Name = name;
        MidiDocument = midiDocument;
        Command = command;

    }
}
