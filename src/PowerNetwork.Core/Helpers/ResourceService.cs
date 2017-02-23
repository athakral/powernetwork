using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace PowerNetwork.Core.Helpers {
    public class ResourceService {

        private readonly Dictionary<string, string> _englishTexts = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _spanishTexts = new Dictionary<string, string>();

        private static readonly object Lock = new object();

        private static ResourceService _instance;

        private ResourceService(IHostingEnvironment hostingEnvironment) {
            Load(_spanishTexts, Path.Combine(hostingEnvironment.WebRootPath, "languageFiles/texts.es.tsv"));
            Load(_englishTexts, Path.Combine(hostingEnvironment.WebRootPath, "languageFiles/texts.en.tsv"));
        }

        public static ResourceService Instance(IHostingEnvironment hostingEnvironment) {
            _instance = _instance ?? (new ResourceService(hostingEnvironment));
            return _instance;
        }

        public Dictionary<string, string> GetMap(string language = "es") {
            return language == "en" ? _englishTexts : _spanishTexts;
        }

        private void Load(Dictionary<string, string> texts, string path) {
            if (texts.Count > 0) return;

            lock (Lock) {
                if (texts.Count > 0) return;

                var lines = File.ReadAllLines(path);

                foreach (var line in lines) {
                    var parts = line.Split(new[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2) {
                        texts.Add(parts[0], parts[1]);
                    }
                }
            }
        }
    }
}