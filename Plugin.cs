using System;
using System.Collections.Generic;
using System.IO;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Drawing; // Requis pour ImageFormat

namespace Jellyfin.Plugin.GenreCleaner
{
    // On garde IHasPluginImage car maintenant le build passe
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

        // CORRECTION ICI : Doit retourner un Stream
        public Stream GetImageResource()
        {
            var type = GetType();
            var resourceName = "Jellyfin.Plugin.GenreCleaner.GenreCleaner.png";
            return type.Assembly.GetManifestResourceStream(resourceName) 
                   ?? throw new FileNotFoundException($"L'image {resourceName} est introuvable dans les ressources.");
        }

        // CORRECTION ICI : Ne pas utiliser ThumbImageFormat (string) mais ImageFormat (enum)
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
