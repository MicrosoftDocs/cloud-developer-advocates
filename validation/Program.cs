using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace AdvocateValidation
{
    class Program
    {
        const string advocatesDirectory = "advocates";
        readonly static IDeserializer _yamlDeserializer = new DeserializerBuilder().Build();

        static async Task Main(string[] args)
        {
            var advocateList = new List<CloudAdvocateYamlModel>();

            var advocateFiles = Directory.GetFiles($"{advocatesDirectory}");

            await foreach (var (path, advocate) in GetAdvocateYmlFiles(advocateFiles).ConfigureAwait(false))
            {
                var gitHubUrl = advocate.Connect.FirstOrDefault(x => x.Title.Equals("GitHub", StringComparison.OrdinalIgnoreCase))?.Url;

                if (gitHubUrl is null)
                    throw new Exception($"Missing GitHub Url: {path}");

                if (!gitHubUrl.IsWellFormedOriginalString())
                    throw new Exception($"Invalid GitHub Url: {path}");

                if (string.IsNullOrWhiteSpace(advocate.Metadata.Alias))
                    throw new Exception($"Missing Microsoft Alias: {path}");

                advocateList.Add(advocate);
            }

            var duplicateAliasList = advocateList.GroupBy(x => x.Metadata.Alias).Where(g => g.Count() > 1).Select(x => x.Key);
            foreach(var duplicateAlias in duplicateAliasList)
            {
                throw new Exception($"Duplicate Alias Found\nms.author: {duplicateAlias}");
            }
        }

        static async IAsyncEnumerable<(string path, CloudAdvocateYamlModel advocate)> GetAdvocateYmlFiles(IEnumerable<string> fileNames)
        {
            var ymlFileNames = fileNames.Where(x => x.EndsWith(".yml", StringComparison.OrdinalIgnoreCase));

            foreach (var fileName in ymlFileNames)
            {
                var path = $"./{advocatesDirectory}/{fileName}";

                var text = await File.ReadAllTextAsync(path).ConfigureAwait(false);

                if (text.Contains("### YamlMime:Profile"))
                    yield return (path, ParseAdvocateFromYaml(text));
            }
        }

        static CloudAdvocateYamlModel ParseAdvocateFromYaml(in string fileText)
        {
            var stringReaderFile = new StringReader(fileText);

            return _yamlDeserializer.Deserialize<CloudAdvocateYamlModel>(stringReaderFile);
        }
    }
}
