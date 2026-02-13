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
        public string Description => "Applique le mapping des genres et log les modifications (Compatible 10.11.5).";
        public string Category => "Library";

        public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            var config = Plugin.Instance?.Configuration;
            if (config == null || config.Mappings == null || config.Mappings.Count == 0)
            {
                _logger.LogWarning("GenreCleaner: Aucune règle de mapping trouvée dans la configuration.");
                return;
            }

            _logger.LogInformation("GenreCleaner: Démarrage de l'analyse pour Jellyfin 10.11.5...");

            var query = new InternalItemsQuery
            {
                IncludeItemTypes = new[] { BaseItemKind.Movie },
                Recursive = true,
                IsVirtualItem = false
            };

            // En 10.11.5, GetItems retourne un IEnumerable<BaseItem>
            var movies = _libraryManager.GetItems(query).ToList();
            int modifiedCount = 0;

            _logger.LogInformation("GenreCleaner: {0} films trouvés dans la bibliothèque.", movies.Count);

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

                // On vérifie si une modification est nécessaire
                if (!originalGenres.OrderBy(g => g).SequenceEqual(finalGenres.OrderBy(g => g)))
                {
                    _logger.LogInformation("GenreCleaner: Mise à jour de '{0}' | [{1}] -> [{2}]", 
                        movie.Name, string.Join(", ", originalGenres), string.Join(", ", finalGenres));

                    movie.Genres = finalGenres;
                    
                    // Verrouillage du champ Genre pour éviter l'écrasement par les scrapers
                    var lockedFields = movie.LockedFields.ToList();
                    if (!lockedFields.Contains(MetadataField.Genres))
                    {
                        lockedFields.Add(MetadataField.Genres);
                        movie.LockedFields = lockedFields.ToArray();
                    }
                    
                    // En 10.11.5, UpdateItemAsync nécessite (item, affectation, type, cancellationToken)
                    await _libraryManager.UpdateItemAsync(movie, movie, ItemUpdateType.MetadataEdit, cancellationToken);
                    modifiedCount++;
                }
                
                // Mise à jour de la barre de progression dans Jellyfin
                progress.Report((double)i / movies.Count * 100);
            }

            _logger.LogInformation("GenreCleaner: Travail terminé. {0} films mis à jour avec succès.", modifiedCount);
        }

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            // Par défaut, la tâche tourne toutes les nuits à 3h du matin
            return new[] { 
                new TaskTriggerInfo { 
                    Type = TaskTriggerInfo.TriggerDaily, 
                    TimeOfDayTicks = TimeSpan.FromHours(3).Ticks 
                } 
            };
        }
    }
}
