using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace PowerNetwork.Core.Helpers {
    public class ResourceService {
        private static Dictionary<string, string> _englishTexts = new Dictionary<string, string>();
        private static Dictionary<string, string> _spanishTexts = new Dictionary<string, string>();

        private static readonly object Lock = new object();

        public static Dictionary<string, string> Instance(IHostingEnvironment hostingEnvironment, string language = "es") {
            Load(ref _spanishTexts, Path.Combine(hostingEnvironment.WebRootPath,"languageFiles/texts.es.tsv"));
            Load(ref _englishTexts, Path.Combine(hostingEnvironment.WebRootPath,"languageFiles/texts.en.tsv"));

            return language == "en" ? _englishTexts : _spanishTexts;
        }

        private static void Load(ref Dictionary<string, string> texts, string path) {
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