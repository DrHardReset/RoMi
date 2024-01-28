using System.Collections.ObjectModel;
using Uno.Extensions.Navigation;
using Windows.Storage.Pickers;

namespace RoMi.Presentation
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly IStringLocalizer localizer;
        private readonly INavigator navigator;
        private readonly IHttpClientFactory httpClientFactory;
        private HttpClient? httpClient = null;
        private readonly string midiDirName = "midi_pdfs";

        [ObservableProperty]
        private StorageFolder? localMidiPdfFolder = null;

        [ObservableProperty]
        private string? name;

        [ObservableProperty]
        private bool isLoading = false;

        private int isLoadingCounter = 0;
        private int IsLoadingCounter
        {
            get { return isLoadingCounter; }
            set
            {
                isLoadingCounter = value;

                if (isLoadingCounter > 0)
                {
                    IsLoading = true;
                }
                else
                {
                    IsLoading = false;
                }
            }
        }

        [ObservableProperty]
        private ObservableCollection<MainPageDeviceButtonData> mainPageDeviceButtonData = new();

        [ObservableProperty]
        private string downloadPdfUrl = string.Empty;
    
        public string? Title { get; }

        public ICommand GoToAboutPage { get; }
        public ICommand OnPageLoaded { get; }
        public ICommand DoOpenPdfFile { get; }
        public ICommand DoDownloadPdfFile { get; }

        public MainViewModel(IStringLocalizer localizer, INavigator navigator, IHttpClientFactory factory)
        {
            this.localizer = localizer;
            this.navigator = navigator;
            httpClientFactory = factory;
            Title = "RoMi";
            GoToAboutPage = new AsyncRelayCommand(GoToAboutView);
            OnPageLoaded = new AsyncRelayCommand(OpenMidiDirPdfs);
            DoOpenPdfFile = new AsyncRelayCommand(OpenPdfFile);
            DoDownloadPdfFile = new AsyncRelayCommand(DownloadPdfFile);
        }

        private async Task CreateAndOpenMidiDir()
        {
            StorageFolder localFolder = await StorageFolder.GetFolderFromPathAsync(AppDomain.CurrentDomain.BaseDirectory);
            LocalMidiPdfFolder = await localFolder.CreateFolderAsync(midiDirName, CreationCollisionOption.OpenIfExists);
        }

        public async Task OpenMidiDirPdfs()
        {
            await CreateAndOpenMidiDir();
            IReadOnlyList<StorageFile> files = await LocalMidiPdfFolder!.GetFilesAsync();
            List<Task> tasks = new List<Task>();

            foreach (StorageFile file in files)
            {
                tasks.Add(ParsePdfFile(file));
            }

            _ = Task.WhenAll(tasks);
        }

        private async Task OpenPdfFile()
        {
            StorageFile? file = await PickAndCopyFile();
            
            if (file == null)
            {
                return;
            }

            _ = ParsePdfFile(file);
        }

        private async Task DownloadPdfFile()
        {
            StorageFile? file = await Download(DownloadPdfUrl);

            if (file == null)
            {
                return;
            }

            _ = ParsePdfFile(file);
        }

        private async Task ParsePdfFile(StorageFile file)
        {
            IsLoadingCounter++;

            try
            {
                MidiDocument midiDocument = await MidiDocumentationFile.Parse(file.Path);
                // Remove eventually already existing entries
                MainPageDeviceButtonData.Remove(x => x.Name == midiDocument.DeviceName);

                Func<Task> func = new(async () =>
                {
                    await GoToMidiView(midiDocument);
                });
                ICommand command = new AsyncRelayCommand(func);
                MainPageDeviceButtonData.Add(new MainPageDeviceButtonData(midiDocument.DeviceName, midiDocument, command));
                MainPageDeviceButtonData = new ObservableCollection<MainPageDeviceButtonData>(MainPageDeviceButtonData.OrderBy(x => x.Name));
            }
            catch (Exception ex)
            {
                _ = navigator.ShowMessageDialogAsync(this, title: "Error parsing PDF file", content: $"The PDF file could not be parsed:\n{ex.Message}");
                IsLoadingCounter--;
                await file.DeleteAsync();
            }
        }

        private async Task GoToAboutView()
        {
            await navigator.NavigateViewModelAsync<AboutViewModel>(this);
        }

        private async Task GoToMidiView(MidiDocument midiDocument)
        {
            await navigator.NavigateViewModelAsync<MidiViewModel>(this, data: midiDocument);
        }

        /// <summary>
        /// Picks the file from local storage and stores it in variable.
        /// </summary>
        private async Task<StorageFile?> PickAndCopyFile()
        {
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".pdf");
            picker.SuggestedStartLocation = PickerLocationId.Downloads;
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            StorageFile file = await picker.PickSingleFileAsync();

            if (file == null)
            {
                return null;
            }

            string fileName = Path.GetFileName(file.Path);
            bool cancel = await CancelIfPdfAlreadyExists(fileName);

            if (cancel)
            {
                return null;
            }

            return await file.CopyAsync(LocalMidiPdfFolder!, fileName, NameCollisionOption.ReplaceExisting);
        }

        private async Task<StorageFile?> Download(string url)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(url))
                {
                    _ = navigator.ShowMessageDialogAsync(this, title: "No URL provided", content: $"Please enter the URL to the Roland MIDI implementation file.");
                    return null;
                }

                string fileName = Path.GetFileName(url);
                bool cancel = await CancelIfPdfAlreadyExists(fileName);
                
                if (cancel)
                {
                    return null;
                }

                if (httpClient == null)
                {
                    httpClient = httpClientFactory.CreateClient();
                }

                byte[] responseBytes = await httpClient.GetByteArrayAsync(url);
                StorageFile fullFilePath = await LocalMidiPdfFolder!.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
                await FileIO.WriteBytesAsync(fullFilePath, responseBytes);
                return fullFilePath;
            }
            catch (HttpRequestException ex)
            {
                string content = string.Format($"{localizer["MainView.PdfDownloadErrer"]}", url);

                await navigator.ShowMessageDialogAsync(
                    this,
                    title: string.Empty,
                    content: content + "\n" + ex
                );

                return null;
            }
        }

        private async Task<bool> CancelIfPdfAlreadyExists(string pdfFileName)
        {
            var alreadyExistingItem = await LocalMidiPdfFolder!.TryGetItemAsync(pdfFileName);

            if (alreadyExistingItem == null)
            {
                return false;
            }

            string content = string.Format($"{localizer["MainView.PdfFileAlreadyExisting"]}", pdfFileName);
            string yes = $"{localizer["General.Yes"]}";
            string no = $"{localizer["General.No"]}";

            string? result = await navigator.ShowMessageDialogAsync<string>(
                this,
                title: string.Empty,
                content: content,
                buttons: new[]
                {
                    new DialogAction(yes),
                    new DialogAction(no)
                }
            );

            if (result == null || result == no)
            {
                return true;
            }

            return false;
        }
    }
}
