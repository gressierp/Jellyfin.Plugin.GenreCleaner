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
        public string Description => "Applique le mapping des genres défini dans la configuration.";
        public string Category => "Library";

        public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            var config = Plugin.Instance?.Configuration;
            
            // Correction Erreur ligne 34 : On vérifie si la chaîne est vide
            if (config == null || string.IsNullOrWhiteSpace(config.Mappings))
            {
                _logger.LogWarning("GenreCleaner: Aucune règle de mapping trouvée dans la configuration.");
                return;
            }

            // Transformation du texte "Ancien=Nouveau" en Dictionnaire
            var mappingRules = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var lines = config.Mappings.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var line in lines)
            {
                var parts = line.Split('=');
                if (parts.Length == 2)
                {
                    mappingRules[parts[0].Trim()] = parts[1].Trim();
                }
            }

            if (mappingRules.Count == 0) return;

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
                bool hasChanged = false;

                foreach (var g in originalGenres)
                {
                    // Correction Erreur ligne 63 : On cherche dans notre dictionnaire
                    if (mappingRules.TryGetValue(g, out var newGenre))
                    {
                        newGenresList.Add(newGenre);
                        hasChanged = true;
                    }
                    else
                    {
                        newGenresList.Add(g);
                    }
                }
                
                if (hasChanged)
                {
                    var finalGenres = newGenresList.Distinct().ToArray();
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
                    Type = TaskTriggerInfoType.Daily, 
                    TimeOfDayTicks = TimeSpan.FromHours(3).Ticks 
                } 
            };
        }
    }
}
