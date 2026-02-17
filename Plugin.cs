using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Drawing;

namespace Jellyfin.Plugin.GenreCleaner
{
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        public override string Name => "Genre Cleaner";
        public override Guid Id => Guid.Parse("7a4b2c1d-8e9f-4a3b-b2c1-d8e9f4a3b2c1");

        public Stream GetImageResource()
        {
            var type = GetType();
            var resourceName = type.Namespace + ".GenreCleaner.png";
            return type.Assembly.GetManifestResourceStream(resourceName) 
                   ?? throw new FileNotFoundException($"Resource not found: {resourceName}");
        }

        public ImageFormat ImageFormat => ImageFormat.Png;
        private readonly ILibraryManager _libraryManager;

        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer, ILibraryManager libraryManager)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
            _libraryManager = libraryManager;

            // Subscribe to item added event for auto-cleaning
            _libraryManager.ItemAdded += OnItemAdded;
        }

        public static Plugin? Instance { get; private set; }

        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new[] 
            { 
                new PluginPageInfo 
                { 
                    Name = "GenreCleaner", 
                    EmbeddedResourcePath = GetType().Namespace + ".genre_config.html" 
                } 
            };
        }

        private void OnItemAdded(object? sender, ItemChangeEventArgs e)
        {
            // Check if auto-clean is enabled and item is Movie or Series
            if (Configuration.EnableAutoClean && (e.Item is MediaBrowser.Controller.Entities.Movies.Movie || e.Item is MediaBrowser.Controller.Entities.TV.Series))
            {
                if (CleanGenres(e.Item))
                {
                    // Update metadata in database
                    _libraryManager.UpdateItemAsync(e.Item, e.Item, ItemUpdateType.MetadataEdit, default);
                }
            }
        }

        public bool CleanGenres(BaseItem item)
        {
            if (item?.Genres == null || item.Genres.Length == 0) return false;
            if (string.IsNullOrWhiteSpace(Configuration.Mappings)) return false;

            // Parse mapping rules (SourceGenre=TargetGenre)
            var mappingRules = Configuration.Mappings.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Split('='))
                .Where(p => p.Length == 2)
                .ToDictionary(p => p[0].Trim(), p => p[1].Trim(), StringComparer.OrdinalIgnoreCase);

            if (mappingRules.Count == 0) return false;

            var currentGenres = item.Genres;
            var newGenresList = new List<string>();
            bool hasChanged = false;

            foreach (var g in currentGenres)
            {
                if (mappingRules.TryGetValue(g, out var target))
                {
                    newGenresList.Add(target);
                    hasChanged = true;
                }
                else
                {
                    newGenresList.Add(g);
                }
            }

            if (hasChanged)
            {
                item.Genres = newGenresList.Distinct().ToArray();
                
                // Lock the Genres field to prevent provider overwrite
                var locked = item.LockedFields.ToList();
                if (!locked.Contains(MetadataField.Genres)) 
                { 
                    locked.Add(MetadataField.Genres); 
                    item.LockedFields = locked.ToArray(); 
                }
            }

            return hasChanged;
        }

        // Standard IDisposable for 10.11.5 compatibility
        public void Dispose()
        {
            if (_libraryManager != null)
            {
                _libraryManager.ItemAdded -= OnItemAdded;
            }
        }
    }
}
