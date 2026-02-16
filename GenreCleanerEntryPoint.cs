using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;

namespace Jellyfin.Plugin.GenreCleaner
{
    // On utilise le chemin COMPLET vers l'interface pour forcer le compilateur
    public class GenreCleanerEntryPoint : MediaBrowser.Controller.Plugins.IServerEntryPoint
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
            // On s'abonne à l'événement d'ajout
            _libraryManager.ItemAdded += OnItemAdded;
            return Task.CompletedTask;
        }

        private async void OnItemAdded(object? sender, ItemChangeEventArgs e)
        {
            // Vérification de sécurité sur l'instance et la config
            if (Plugin.Instance == null || Plugin.Instance.Configuration.EnableAutoClean != true) 
                return;

            // Filtrage : Films ou Séries
            if (e.Item is Movie || e.Item is Series)
            {
                // On appelle le moteur centralisé dans Plugin.cs
                if (Plugin.Instance.CleanGenres(e.Item))
                {
                    _logger.LogInformation("GenreCleaner (Auto): Modification des genres détectée pour {0}", e.Item.Name);
                    
                    // On enregistre les changements dans la base Jellyfin
                    await _libraryManager.UpdateItemAsync(e.Item, e.Item, ItemUpdateType.MetadataEdit, default);
                }
            }
        }

        public void Dispose()
        {
            // Libération de l'événement pour éviter les fuites mémoire
            if (_libraryManager != null)
            {
                _libraryManager.ItemAdded -= OnItemAdded;
            }
        }
    }
}
