using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Tasks;
using MediaBrowser.Model.Entities;
using MediaBrowser.Controller.Entities; // Nécessaire pour InternalItemsQuery en 10.11.5
using System;
using System.Collections.Generic;
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

        public string Name => "Nettoyer les genres (Manuel)";
        public string Key => "GenreCleanupTask";
        public string Description => "Applique les règles de remplacement à toute la bibliothèque.";
        public string Category => "Library";

        public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            // Correction 10.11.5 : Vérifier que le namespace Entities est bien inclus
            var query = new InternalItemsQuery 
            { 
                IncludeItemTypes = new[] { "Movie", "Series" }, 
                Recursive = true,
                IsVirtualItem = false
            };
            
            var items = _libraryManager.GetItemList(query);

            for (int i = 0; i < items.Count; i++)
            {
                // On appelle le moteur centralisé dans Plugin.cs (version 10.11.5)
                if (Plugin.Instance != null && Plugin.Instance.CleanGenres(items[i]))
                {
                    await _libraryManager.UpdateItemAsync(items[i], items[i], ItemUpdateType.MetadataEdit, cancellationToken);
                }
                
                if (items.Count > 0)
                {
                    progress.Report((double)i / items.Count * 100);
                }
            }
        }

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            return new[] 
            { 
                new TaskTriggerInfo 
                { 
                    Type = (TaskTriggerInfoType)0, // Stable pour 10.11.x
                    TimeOfDayTicks = TimeSpan.FromHours(3).Ticks 
                } 
            };
        }
    }
}
