using System;
using System.Collections.Generic;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Jellyfin.Plugin.GenreCleaner;
using MediaBrowser.Model.Drawing;

namespace Jellyfin.Plugin.GenreCleaner
{
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages, IHasPluginImage
    {
        public override string Name => "Genre Cleaner";
        public override Guid Id => Guid.Parse("7a4b2c1d-8e9f-4a3b-b2c1-d8e9f4a3b2c1"); // Ton GUID unique

        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
        }

        public static Plugin? Instance { get; private set; }

        public Stream GetImageResource()
        {
            var type = GetType();
            return type.Assembly.GetManifestResourceStream("Jellyfin.Plugin.GenreCleaner.icon.png");
        }

        public string ThumbImageFormat => "png";

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
