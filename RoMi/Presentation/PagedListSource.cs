using CommunityToolkit.Common.Collections;

namespace RoMi.Presentation;

/// <summary>
/// A sample implementation of the <see cref="IIncrementalSource{TSource}"/> interface.
/// </summary>
/// <seealso cref="IIncrementalSource{TSource}"/>
public class PagedListSource : IIncrementalSource<object>
{
    private readonly List<object> objectList;

    public PagedListSource(List<object> objectList)
    {
        this.objectList = objectList;
    }

    /// <summary>
    /// Retrieves items based on <paramref name="pageIndex"/> and <paramref name="pageSize"/> arguments.
    /// </summary>
    /// <param name="pageIndex">
    /// The zero-based index of the page that corresponds to the items to retrieve.
    /// </param>
    /// <param name="pageSize">
    /// The number of <see langword="object"/> items to retrieve for the specified <paramref name="pageIndex"/>.
    /// </param>
    /// <param name="cancellationToken">
    /// Used to propagate notification that operation should be canceled.
    /// </param>
    /// <returns>
    /// Returns a collection of <see langword="object"/>.
    /// </returns>
    public async Task<IEnumerable<object>> GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default(CancellationToken))
    {
        return await Task.Run(() =>
        {
            return objectList.Skip(pageIndex * pageSize).Take(pageSize);
        });
    }
}
