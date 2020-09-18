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
            const string gitHub = "GitHub";
            const string twitter = "Twitter";
            const string linkedIn = "LinkedIn";

            var advocateList = new List<CloudAdvocateYamlModel>();

            var advocateFiles = Directory.GetFiles(_advocatesPath);

            await foreach (var (filePath, advocate) in GetAdvocateYmlFiles(advocateFiles).ConfigureAwait(false))
            {
                var gitHubUri = advocate.Connect.FirstOrDefault(x => x.Title.Equals(gitHub, StringComparison.OrdinalIgnoreCase))?.Url;
                var twitterUri = advocate.Connect.FirstOrDefault(x => x.Title.Equals(twitter, StringComparison.OrdinalIgnoreCase))?.Url;
                var linkedInUri = advocate.Connect.FirstOrDefault(x => x.Title.Equals(linkedIn, StringComparison.OrdinalIgnoreCase))?.Url;

                EnsureValidUri(filePath, gitHubUri, gitHub);
                EnsureValidUri(filePath, gitHubUri, twitter);
                EnsureValidUri(filePath, linkedInUri, linkedIn);

                if (string.IsNullOrWhiteSpace(advocate.Metadata.Alias))
                    throw new Exception($"Missing Microsoft Alias: {filePath}");

                advocateList.Add(advocate);
            }

            var duplicateAliasList = advocateList.GroupBy(x => x.Metadata.Alias).Where(g => g.Count() > 1).Select(x => x.Key);
            foreach (var duplicateAlias in duplicateAliasList)
            {
                throw new Exception($"Duplicate Alias Found; ms.author: {duplicateAlias}");
            }
        }

        static async IAsyncEnumerable<(string filePath, CloudAdvocateYamlModel advocate)> GetAdvocateYmlFiles(IEnumerable<string> files)
        {
            var ymlFiles = files.Where(x => x.EndsWith(".yml", StringComparison.OrdinalIgnoreCase));

            foreach (var filePath in ymlFiles)
            {
                var text = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);

                if (text.StartsWith("### YamlMime:Profile") && !text.StartsWith("### YamlMime:ProfileList"))
                {
                    Console.WriteLine($"Parsing {filePath}");
                    yield return (filePath, ParseAdvocateFromYaml(text));
                }
            }
        }

        static CloudAdvocateYamlModel ParseAdvocateFromYaml(in string fileText)
        {
            var stringReaderFile = new StringReader(fileText);

            return _yamlDeserializer.Deserialize<CloudAdvocateYamlModel>(stringReaderFile);
        }

        static void EnsureValidUri(in string filePath, in Uri? uri, in string uriName)
        {
            if (uri is null)
                throw new Exception($"Missing {uriName} Url: {filePath}");

            if (!uri.IsWellFormedOriginalString())
                throw new Exception($"Invalid {uriName} Url: {filePath}");
        }
    }
}
