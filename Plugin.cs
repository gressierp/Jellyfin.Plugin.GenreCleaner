using System;
using System.Collections.Generic;
using System.IO; // Essentiel pour Stream
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Drawing; // Essentiel pour ImageFormat

namespace Jellyfin.Plugin.GenreCleaner
{
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages, IHasPluginImage
    {
        public override string Name => "Genre Cleaner";
        public override Guid Id => Guid.Parse("7a4b2c1d-8e9f-4a3b-b2c1-d8e9f4a3b2c1");

        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
        }

        public static Plugin? Instance { get; private set; }

        // Interface IHasPluginImage
        public Stream GetImageResource()
        {
            var type = GetType();
            // Vérifie bien que ton fichier s'appelle GenreCleaner.png dans ton projet
            return type.Assembly.GetManifestResourceStream("Jellyfin.Plugin.GenreCleaner.GenreCleaner.png") 
                   ?? throw new FileNotFoundException("L'image n'a pas été trouvée dans les ressources.");
        }

        // Le format doit être l'énumération, pas un string
        public ImageFormat ImageFormat => ImageFormat.Png;

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
    }
}
