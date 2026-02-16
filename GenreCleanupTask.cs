public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
{
    var query = new InternalItemsQuery { IncludeItemTypes = new[] { BaseItemKind.Movie }, Recursive = true, IsVirtualItem = false };
    var movies = _libraryManager.GetItemList(query);

    for (int i = 0; i < movies.Count; i++)
    {
        if (Plugin.Instance != null && Plugin.Instance.CleanGenres(movies[i]))
        {
            await _libraryManager.UpdateItemAsync(movies[i], movies[i], ItemUpdateType.MetadataEdit, cancellationToken);
        }
        progress.Report((double)i / movies.Count * 100);
    }
}
