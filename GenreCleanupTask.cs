using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Tasks;
using MediaBrowser.Model.Entities;
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
                IncludeItemTypes = new[] { "Movie" },
                Recursive = true
            });

            for (int i = 0; i < movies.Count; i++)
            {
                var movie = movies[i];
                var originalGenres = movie.Genres;
                var newGenres = originalGenres.Select(g => 
                    config.Mappings.FirstOrDefault(m => m.OldGenre.Equals(g, StringComparison.OrdinalIgnoreCase))?.NewGenre ?? g
                ).Distinct().ToArray();

                if (!originalGenres.SequenceEqual(newGenres))
                {
                    movie.Genres = newGenres;
                    var lockedFields = movie.LockedFields.ToList();
                    if (!lockedFields.Contains(MetadataField.Genres))
                    {
                        lockedFields.Add(MetadataField.Genres);
                        movie.LockedFields = lockedFields.ToArray();
                    }
                    
                    await _libraryManager.UpdateItemAsync(movie, ItemUpdateType.MetadataEdit, cancellationToken);
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
