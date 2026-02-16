using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using MediaBrowser.Controller.Entities.Movies;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.GenreCleaner
{
    public class GenreCleanerEntryPoint : IServerEntryPoint
    {
        private readonly ILibraryManager _libraryManager;
        private readonly ILogger<GenreCleanerEntryPoint> _logger;

        public GenreCleanerEntryPoint(ILibraryManager libraryManager, ILogger<GenreCleanerEntryPoint> logger)
        {
            _libraryManager = libraryManager;
            _logger = logger;
        }

        public Task RunAsync()
        {
            _libraryManager.ItemAdded += OnItemAdded;
            return Task.CompletedTask;
        }

        private async void OnItemAdded(object? sender, ItemChangeEventArgs e)
        {
            if (Plugin.Instance?.Configuration.EnableAutoClean != true) return;

            if (e.Item is Movie || e.Item is MediaBrowser.Controller.Entities.TV.Series)
            {
                if (Plugin.Instance.CleanGenres(e.Item))
                {
                    _logger.LogInformation("GenreCleaner (Auto): Nettoyage des genres pour {0}", e.Item.Name);
                    await _libraryManager.UpdateItemAsync(e.Item, e.Item, ItemUpdateType.MetadataEdit, default);
                }
            }
        }

        public void Dispose()
        {
            _libraryManager.ItemAdded -= OnItemAdded;
        }
    }
}
