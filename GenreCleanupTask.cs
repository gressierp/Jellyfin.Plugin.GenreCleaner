using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Tasks;
using MediaBrowser.Model.Entities;
using Jellyfin.Data.Enums;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<GenreCleanupTask> _logger;

        public GenreCleanupTask(ILibraryManager libraryManager, ILogger<GenreCleanupTask> logger)
        {
            _libraryManager = libraryManager;
            _logger = logger;
        }

        public string Name => "Nettoyer les genres de films";
        public string Key => "GenreCleanupTask";
        public string Description => "Applique le mapping des genres (Spécifique 10.11.5).";
        public string Category => "Library";

        public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            var config = Plugin.Instance?.Configuration;
            if (config == null || config.Mappings == null || config.Mappings.Count == 0)
            {
                _logger.LogWarning("GenreCleaner: Aucune règle de mapping trouvée.");
                return;
            }

            _logger.LogInformation("GenreCleaner: Démarrage du scan...");

            // En 10.11.5, on utilise GetItemList avec le bon type de retour
            var query = new InternalItemsQuery
            {
                IncludeItemTypes = new[] { BaseItemKind.Movie },
                Recursive = true,
                IsVirtualItem = false
            };

            var movies = _libraryManager.GetItemList(query);
            int modifiedCount = 0;

            for (int i = 0; i < movies.Count; i++)
            {
                var movie = movies[i];
                var originalGenres = movie.Genres;
                
                if (originalGenres == null || originalGenres.Length == 0) continue;

                var newGenresList = new List<string>();
                foreach (var g in originalGenres)
                {
                    var mapping = config.Mappings.FirstOrDefault(m => m.OldGenre.Equals(g, StringComparison.OrdinalIgnoreCase));
                    newGenresList.Add(mapping != null ? mapping.NewGenre : g);
                }
                
                var finalGenres = newGenresList.Distinct().ToArray();

                if (!originalGenres.SequenceEqual(finalGenres))
                {
                    _logger.LogInformation("GenreCleaner: Mise à jour de '{0}'", movie.Name);

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

            _logger.LogInformation("GenreCleaner: {0} films mis à jour.", modifiedCount);
        }

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            return new[] { 
                new TaskTriggerInfo { 
                    // On utilise le transtypage pour contourner les changements de noms
                    Type = (TaskTriggerInfoType)0, // 0 correspond à 'Daily' dans Jellyfin
                    TimeOfDayTicks = TimeSpan.FromHours(3).Ticks 
                } 
            };
        }
    }
}
