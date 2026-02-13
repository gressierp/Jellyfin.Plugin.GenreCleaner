using MediaBrowser.Model.Plugins;
using System.Collections.Generic;

namespace Jellyfin.Plugin.GenreCleaner
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        // On stocke le mapping sous forme de liste de paires Clé/Valeur
        public List<GenreMapping> Mappings { get; set; }

        public PluginConfiguration()
        {
            Mappings = new List<GenreMapping>();
        }
    }

    public class GenreMapping
    {
        public string OldGenre { get; set; } = string.Empty;
        public string NewGenre { get; set; } = string.Empty;
    }
}
