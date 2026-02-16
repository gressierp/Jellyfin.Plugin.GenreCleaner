using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using MediaBrowser.Controller.Library;

namespace Jellyfin.Plugin.GenreCleaner
{
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        public override string Name => "Genre Cleaner";
        public override Guid Id => Guid.Parse("7a4b2c1d-8e9f-4a3b-b2c1-d8e9f4a3b2c1");

        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
        }

        public static Plugin? Instance { get; private set; }

        public Stream GetImageResource()
        {
            var type = GetType();
            var resourceName = "Jellyfin.Plugin.GenreCleaner.GenreCleaner.png";
            return type.Assembly.GetManifestResourceStream(resourceName) 
                   ?? throw new FileNotFoundException($"L'image {resourceName} est introuvable.");
        }

        public ImageFormat ImageFormat => ImageFormat.Png;

        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new[] { new PluginPageInfo { Name = "GenreCleaner", EmbeddedResourcePath = GetType().Namespace + ".genre_config.html" } };
        }

        // --- LE MOTEUR DE NETTOYAGE CENTRALISÉ ---
        public bool CleanGenres(BaseItem item)
        {
            if (item.Genres == null || item.Genres.Length == 0) return false;
            var config = Configuration;
            if (string.IsNullOrWhiteSpace(config.Mappings)) return false;

            // Préparation du dictionnaire
            var mappingRules = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var lines = config.Mappings.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var parts = line.Split('=');
                if (parts.Length == 2) mappingRules[parts[0].Trim()] = parts[1].Trim();
            }

            var newGenresList = new List<string>();
            bool hasChanged = false;

            foreach (var g in item.Genres)
            {
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
                item.Genres = newGenresList.Distinct().ToArray();
                var lockedFields = item.LockedFields.ToList();
                if (!lockedFields.Contains(MetadataField.Genres))
                {
                    lockedFields.Add(MetadataField.Genres);
                    item.LockedFields = lockedFields.ToArray();
                }
            }

            return hasChanged;
        }
    }
}
