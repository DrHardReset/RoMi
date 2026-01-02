namespace RoMi.Presentation;

public class MainPageDeviceButtonData(string name, MidiDocument midiDocument, ICommand command)
{
    public string Name { get; set; } = name;
    public object MidiDocument { get; set; } = midiDocument;
    public ICommand Command { get; set; } = command;
}
