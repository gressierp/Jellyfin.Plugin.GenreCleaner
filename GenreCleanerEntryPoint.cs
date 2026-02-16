using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins; // C'est celui-ci qui manquait pour IServerEntryPoint
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using MediaBrowser.Controller.Entities.Movies;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;
using System.Linq;

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
            // Vérification de la config
            if (Plugin.Instance?.Configuration.EnableAutoClean != true) return;

            // On traite les Films et les Séries (Series est dans MediaBrowser.Controller.Entities.TV)
            if (e.Item is Movie || e.Item is MediaBrowser.Controller.Entities.TV.Series)
            {
                if (Plugin.Instance.CleanGenres(e.Item))
                {
                    _logger.LogInformation("GenreCleaner (Auto): Genres mis à jour pour {0}", e.Item.Name);
                    // Sauvegarde des métadonnées modifiées
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
