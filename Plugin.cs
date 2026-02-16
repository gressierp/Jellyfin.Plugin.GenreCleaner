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

        private readonly ILibraryManager _libraryManager;

        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer, ILibraryManager libraryManager)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
            _libraryManager = libraryManager;

            // Abonnement aux nouveaux médias
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
            if (Configuration.EnableAutoClean && (e.Item is MediaBrowser.Controller.Entities.Movies.Movie || e.Item is MediaBrowser.Controller.Entities.TV.Series))
            {
                if (CleanGenres(e.Item))
                {
                    _libraryManager.UpdateItemAsync(e.Item, e.Item, ItemUpdateType.MetadataEdit, default);
                }
            }
        }

        public bool CleanGenres(BaseItem item)
        {
            if (item?.Genres == null || item.Genres.Length == 0) return false;
            if (string.IsNullOrWhiteSpace(Configuration.Mappings)) return false;

            var mappingRules = Configuration.Mappings.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Split('='))
                .Where(p => p.Length == 2)
                .ToDictionary(p => p[0].Trim(), p => p[1].Trim(), StringComparer.OrdinalIgnoreCase);

            if (mappingRules.Count == 0) return false;

            var newGenres = item.Genres.Select(g => mappingRules.TryGetValue(g, out var n) ? n : g).Distinct().ToArray();

            if (!item.Genres.SequenceEqual(newGenres))
            {
                item.Genres = newGenres;
                var locked = item.LockedFields.ToList();
                if (!locked.Contains(MetadataField.Genres)) 
                { 
                    locked.Add(MetadataField.Genres); 
                    item.LockedFields = locked.ToArray(); 
                }
                return true;
            }
            return false;
        }

        // Correction de l'erreur CS0115 : On utilise la méthode de l'interface IDisposable
        // sans le mot-clé override si la classe de base ne le définit pas.
        public void Dispose()
        {
            if (_libraryManager != null)
            {
                _libraryManager.ItemAdded -= OnItemAdded;
            }
        }
    }
}
