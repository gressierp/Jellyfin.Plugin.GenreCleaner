using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Tasks;
using MediaBrowser.Model.Entities;
using MediaBrowser.Controller.Entities;
using Jellyfin.Data.Enums;
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

        // UI Strings translated to English
        public string Name => "Clean Library Genres";
        public string Key => "GenreCleanupTask";
        public string Description => "Applies genre mapping rules to all movies and series in the library.";
        public string Category => "Library";

        public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            // Query for Movies and Series - Compatible with 10.11.5 strict typing
            var query = new InternalItemsQuery 
            { 
                IncludeItemTypes = new[] { BaseItemKind.Movie, BaseItemKind.Series }, 
                Recursive = true,
                IsVirtualItem = false
            };
            
            var items = _libraryManager.GetItemList(query);

            for (int i = 0; i < items.Count; i++)
            {
                // Check if user cancelled the task
                cancellationToken.ThrowIfCancellationRequested();

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
                    // Correct Enum for 10.11.5
                    Type = TaskTriggerInfoType.DailyTrigger, 
                    TimeOfDayTicks = TimeSpan.FromHours(3).Ticks 
                } 
            };
        }
    }
}
