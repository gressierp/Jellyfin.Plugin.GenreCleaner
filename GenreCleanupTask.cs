using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Tasks;
using MediaBrowser.Model.Entities;
using Jellyfin.Data.Enums;
using Microsoft.Extensions.Logging; // Requis pour le logging
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.GenreCleaner
{
    public class GenreCleanupTask : IScheduledTask
    {
        private readonly ILibraryManager _libraryManager;
        private readonly ILogger<GenreCleanupTask> _logger; // Déclaration du logger

        public GenreCleanupTask(ILibraryManager libraryManager, ILogger<GenreCleanupTask> logger)
        {
            _libraryManager = libraryManager;
            _logger = logger;
        }

        public string Name => "Nettoyer les genres de films";
        public string Key => "GenreCleanupTask";
        public string Description => "Applique le mapping des genres et log les modifications.";
        public string Category => "Library";

        public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            var config = Plugin.Instance?.Configuration;
            if (config == null || config.Mappings == null || config.Mappings.Count == 0)
            {
                _logger.LogWarning("GenreCleaner: Aucune règle de mapping trouvée dans la configuration.");
                return;
            }

            _logger.LogInformation("GenreCleaner: Démarrage du nettoyage des genres...");

            var movies = _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = new[] { BaseItemKind.Movie },
                Recursive = true,
                IsVirtualItem = false
            }).ToList();

            int modifiedCount = 0;

            for (int i = 0; i < movies.Count; i++)
            {
                var movie = movies[i];
                var originalGenres = movie.Genres;
                
                var newGenresList = new List<string>();
                foreach (var g in originalGenres)
                {
                    var mapping = config.Mappings.FirstOrDefault(m => m.OldGenre.Equals(g, StringComparison.OrdinalIgnoreCase));
                    newGenresList.Add(mapping != null ? mapping.NewGenre : g);
                }
                
                var finalGenres = newGenresList.Distinct().ToArray();

                if (!originalGenres.SequenceEqual(finalGenres))
                {
                    _logger.LogInformation("GenreCleaner: Modification de '{0}' | Anciens: [{1}] -> Nouveaux: [{2}]", 
                        movie.Name, string.Join(", ", originalGenres), string.Join(", ", finalGenres));

                    movie.Genres = finalGenres;
                    var lockedFields = movie.LockedFields.ToList();
                    if (!lockedFields.Contains(MetadataField.Genres))
                    {
                        lockedFields.Add(MetadataField.Genres);
                        movie.LockedFields = lockedFields.ToArray();
                    }
                    
                    await _libraryManager.UpdateItemAsync(movie, movie, ItemUpdateType.MetadataEdit, cancellationToken);
                    modifiedCount++;
                }
                progress.Report((double)i / movies.Count * 100);
            }

            _logger.LogInformation("GenreCleaner: Nettoyage terminé. {0} films ont été mis à jour.", modifiedCount);
        }

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            return new[] { new TaskTriggerInfo { Type = TaskTriggerInfo.TriggerDaily, TimeOfDayTicks = TimeSpan.FromHours(3).Ticks } };
        }
    }
}
