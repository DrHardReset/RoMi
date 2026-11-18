using System.Collections.ObjectModel;
using Microsoft.UI.Xaml.Data;
using Windows.Foundation;

namespace RoMi.Presentation;

/// <summary>
/// Provides items of an <see cref="IIncrementalSource{T}"/> page by page.
/// </summary>
public class IncrementalLoadingCollection<TSource, T>(TSource source, int itemsPerPage)
    : ObservableCollection<T>, ISupportIncrementalLoading
    where TSource : IIncrementalSource<T>
{
    private int currentPageIndex;
    private bool isLoading;

    public Action? OnEndLoading { get; set; }

    public bool HasMoreItems { get; private set; } = true;

    public IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
        => LoadAsync().AsAsyncOperation();

    private async Task<LoadMoreItemsResult> LoadAsync()
    {
        if (isLoading)
        {
            return new LoadMoreItemsResult { Count = 0 };
        }

        isLoading = true;
        try
        {
            List<T> items = (await source.GetPagedItemsAsync(currentPageIndex, itemsPerPage)).ToList();
            foreach (T item in items)
            {
                Add(item);
            }

            currentPageIndex++;
            HasMoreItems = items.Count >= itemsPerPage;
            return new LoadMoreItemsResult { Count = (uint)items.Count };
        }
        finally
        {
            isLoading = false;
            OnEndLoading?.Invoke();
        }
    }
}
