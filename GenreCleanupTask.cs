using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Tasks;
using MediaBrowser.Model.Entities;
using Jellyfin.Data.Enums; // Requis pour BaseItemKind
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

        public GenreCleanupTask(ILibraryManager libraryManager)
        {
            _libraryManager = libraryManager;
        }

        public string Name => "Nettoyer les genres de films";
        public string Key => "GenreCleanupTask";
        public string Description => "Applique le mapping des genres défini dans la configuration.";
        public string Category => "Library";

        public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            var config = Plugin.Instance?.Configuration;
            if (config == null || config.Mappings == null || config.Mappings.Count == 0) return;

            var movies = _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = new[] { BaseItemKind.Movie }, // Correction CS0029
                Recursive = true,
                IsVirtualItem = false
            }).ToList();

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
                    movie.Genres = finalGenres;
                    var lockedFields = movie.LockedFields.ToList();
                    if (!lockedFields.Contains(MetadataField.Genres))
                    {
                        lockedFields.Add(MetadataField.Genres);
                        movie.LockedFields = lockedFields.ToArray();
                    }
                    
                    // Correction CS7036 : Ajout des paramètres manquants
                    await _libraryManager.UpdateItemAsync(movie, movie, ItemUpdateType.MetadataEdit, cancellationToken);
                }
                progress.Report((double)i / movies.Count * 100);
            }
        }

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            return new[] { new TaskTriggerInfo { Type = TaskTriggerInfo.TriggerDaily, TimeOfDayTicks = TimeSpan.FromHours(3).Ticks } };
        }
    }
}
