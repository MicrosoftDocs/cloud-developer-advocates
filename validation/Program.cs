using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace AdvocateValidation
{
    class Program
    {

        readonly static string _advocatesPath =
#if DEBUG
            Path.Combine("../../../../", "advocates");
#else
            Path.Combine("../", "advocates");
#endif

        readonly static IDeserializer _yamlDeserializer = new DeserializerBuilder().Build();

        static async Task Main(string[] args)
        {
            var advocateList = new List<CloudAdvocateYamlModel>();

            var advocateFiles = Directory.GetFiles(_advocatesPath);

            await foreach (var (path, advocate) in GetAdvocateYmlFiles(advocateFiles).ConfigureAwait(false))
            {
                #region Uncomment to enable GitHub Url Validation
                //var gitHubUrl = advocate.Connect.FirstOrDefault(x => x.Title.Equals("GitHub", StringComparison.OrdinalIgnoreCase))?.Url;

                //if (gitHubUrl is null)
                //    throw new Exception($"Missing GitHub Url: {path}");

                //if (!gitHubUrl.IsWellFormedOriginalString())
                //    throw new Exception($"Invalid GitHub Url: {path}");
                #endregion

                if (string.IsNullOrWhiteSpace(advocate.Metadata.Alias))
                    throw new Exception($"Missing Microsoft Alias: {path}");

                advocateList.Add(advocate);
            }

            var duplicateAliasList = advocateList.GroupBy(x => x.Metadata.Alias).Where(g => g.Count() > 1).Select(x => x.Key);
            foreach (var duplicateAlias in duplicateAliasList)
            {
                throw new Exception($"Duplicate Alias Found; ms.author: {duplicateAlias}");
            }
        }

        static async IAsyncEnumerable<(string path, CloudAdvocateYamlModel advocate)> GetAdvocateYmlFiles(IEnumerable<string> files)
        {
            var ymlFiles = files.Where(x => x.EndsWith(".yml", StringComparison.OrdinalIgnoreCase));

            foreach (var file in ymlFiles)
            {
                var text = await File.ReadAllTextAsync(file).ConfigureAwait(false);

                if (text.StartsWith("### YamlMime:Profile") && !text.StartsWith("### YamlMime:ProfileList"))
                {
                    Console.WriteLine($"Parsing {file}");
                    yield return (file, ParseAdvocateFromYaml(text));
                }
            }
        }

        static CloudAdvocateYamlModel ParseAdvocateFromYaml(in string fileText)
        {
            var stringReaderFile = new StringReader(fileText);

            return _yamlDeserializer.Deserialize<CloudAdvocateYamlModel>(stringReaderFile);
        }
    }
}
