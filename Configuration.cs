using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.GenreCleaner
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        // Ces noms doivent correspondre EXACTEMENT à ceux utilisés dans le script JS du fichier HTML
        public string Mappings { get; set; }
        public bool EnableAutoClean { get; set; }

        public PluginConfiguration()
        {
            // Valeurs par défaut lors du premier lancement
            Mappings = string.Empty;
            EnableAutoClean = true;
        }
    }
}
