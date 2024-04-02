using System.Net;
using System.Net.Http.Headers;
using System.Text;
using GitHubApiStatus;
using YamlDotNet.Serialization;

namespace AdvocateValidation;
class Program
{
    readonly static GitHubApiStatusService _gitHubApiStatusService = new();

    readonly static string[] linkTypesToIgnore = ["LinkedIn", "Reddit"];

    readonly static string _advocatesPath =
#if DEBUG
        Path.Combine("../../../../", "advocates");
#else
            Path.Combine("../", "advocates");
#endif

    readonly static IDeserializer _yamlDeserializer = new DeserializerBuilder().Build();

    static async Task Main()
    {
        const string gitHub = "GitHub";

        List<CloudAdvocateYamlModel> advocateList = [];

        string[] advocateFiles = Directory.GetFiles(_advocatesPath);

        List<(string, string)> parsingErrors = [];
        List<(string, string)> parsingWarnings = [];

        await foreach ((string filePath, CloudAdvocateYamlModel advocate) in GetAdvocateYmlFiles(advocateFiles).ConfigureAwait(false))
        {
            if (string.IsNullOrWhiteSpace(advocate?.Metadata.Alias))
            {
                parsingErrors.Add((filePath, "Missing Microsoft Alias"));
                continue;
            }

            if (string.IsNullOrWhiteSpace(advocate.Metadata.Team))
            {
                parsingErrors.Add((filePath, "Missing Team"));
                continue;
            }

            foreach (Connect connect in advocate.Connect)
            {
                if (linkTypesToIgnore.Contains(connect.Title, StringComparer.OrdinalIgnoreCase))
                {
#if DEBUG
                    Console.WriteLine($"{connect.Title} doesn't like being validated, skipping {connect.Url} from {filePath}.");
#endif
                    continue;
                }

                try
                {
                    if (connect.Title.Equals(gitHub, StringComparison.OrdinalIgnoreCase))
                    {
                        await EnsureValidGitHubUri(filePath, connect.Url, gitHub).ConfigureAwait(false);
                    }
                    else
                    {
                        await EnsureValidUri(filePath, connect.Url, connect.Title).ConfigureAwait(false);
                    }
                }
                catch (ValidationException ex)
                {
                    parsingErrors.Add((filePath, ex.Message));
                }
                catch (ValidationWarningException ex)
                {
                    parsingWarnings.Add((filePath, ex.Message));
                }
            }

            try
            {
                EnsureValidImage(filePath, advocate.Image);
            }
            catch (ValidationException ex)
            {
                parsingErrors.Add((filePath, ex.Message));
            }

            advocateList.Add(advocate);
        }

        IEnumerable<string> duplicateAliasList = advocateList.GroupBy(x => x.Metadata.Alias).Where(g => g.Count() > 1).Select(x => x.Key);
        foreach (string duplicateAlias in duplicateAliasList)
        {
            parsingErrors.Add(("", $"Duplicate Alias Found; ms.author: {duplicateAlias}"));
        }

        foreach ((string filePath, string parsingWarning) in parsingWarnings)
        {
            Console.WriteLine($"::warning file={filePath}::{parsingWarning}");
        }

        if (parsingErrors.Count != 0)
        {
            Console.WriteLine("Validation Failed");
            foreach ((string filePath, string parsingError) in parsingErrors)
            {
                Console.WriteLine($"::error file={filePath}::{parsingError}");
            }
            throw new Exception("Validation Failed");
        }
        else
        {
            Console.WriteLine("Validation Completed Successfully");
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

    static async Task EnsureValidUri(string filePath, Uri? uri, string uriName)
    {
        if (uri is null)
            throw new ValidationException($"Missing '{uriName}' Url: {uri} - File: {filePath}");

        if (!uri.IsWellFormedOriginalString())
            throw new ValidationException($"URI for '{uriName}' is malformed. Url: {uri} - File: {filePath}");

        if (uri.Scheme == Uri.UriSchemeHttp)
            Console.WriteLine($"::warning file={filePath}:: '{uriName}' Url is HTTP, you really should be hosting on HTTPS. Url: {uri}.");

        HttpClient _client = new();
        _client.DefaultRequestHeaders.UserAgent.Clear();
        _client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36 Edg/122.0.0.0");
        HttpResponseMessage response = await _client.GetAsync(uri).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode && response.StatusCode is HttpStatusCode.NotFound)
            throw new ValidationException($"Failed to resolve URI for '{uriName}' ({response.StatusCode}). Url: {uri} - File: {filePath}");
        else if (!response.IsSuccessStatusCode)
            throw new ValidationWarningException($"Failed to resolve URI for '{uriName}' ({response.StatusCode}) but we're going to ignore it. Url: {uri} - File: {filePath}");
    }

    static async Task EnsureValidGitHubUri(string filePath, Uri? uri, string uriName)
    {
        if (uri is null)
            throw new ValidationException($"Missing '{uriName}' Url: {uri} - File: {filePath}");

        if (!uri.IsWellFormedOriginalString())
            throw new ValidationException($"URI for '{uriName}' is malformed. Url: {uri} - File: {filePath}");

        bool hasReceivedGitHubAbuseLimitResponse;

        do
        {
            HttpClient _client = new();
            HttpResponseMessage response = await _client.GetAsync(uri).ConfigureAwait(false);
            hasReceivedGitHubAbuseLimitResponse = _gitHubApiStatusService.IsAbuseRateLimit(response.Headers, out var delta);

            if (hasReceivedGitHubAbuseLimitResponse && delta is TimeSpan timeRemaining)
            {
                Console.WriteLine($"Rate Limit Exceeded. Retrying in {timeRemaining.TotalSeconds} seconds");
                await Task.Delay(timeRemaining).ConfigureAwait(false);
            }
            else if (!response.IsSuccessStatusCode)
            {
                throw new ValidationException($"Rate limit for '{uriName}' failed with {response.StatusCode}. Url: {uri} - File: {filePath}");
            }
        }
        while (hasReceivedGitHubAbuseLimitResponse);
    }

    static void EnsureValidImage(in string filePath, in Image? cloudAdvocateImage)
    {
        if (cloudAdvocateImage is null || string.IsNullOrWhiteSpace(cloudAdvocateImage.Src))
            throw new ValidationException($"Image Source Missing: {filePath}");

        string filePathRelativeToValidation = Path.Combine(_advocatesPath, cloudAdvocateImage.Src);

        if (!File.Exists(filePathRelativeToValidation))
            throw new ValidationException($"Image Source Missing: {filePathRelativeToValidation}");

        using FileStream fileStream = new(filePathRelativeToValidation, FileMode.Open);
        using BinaryReader binaryReader = new(fileStream, Encoding.UTF8);

        System.Drawing.Size imageSize = ImageService.GetDimensions(binaryReader);

        if (imageSize.Height <= 0)
            throw new ValidationException($"Invalid Image Height (must be greater than 0): {filePath}");

        if (imageSize.Width <= 0)
            throw new ValidationException($"Invalid Image Width (must be greater than 0): {filePath}");

        if (imageSize.Height != imageSize.Width)
            throw new ValidationException($"Invalid Image (Height and Width must be equal - {imageSize.Height} x {imageSize.Width}): {filePath}");

        if (cloudAdvocateImage.Alt is null)
            throw new ValidationException($"Image Alt Text Missing: {filePath}");
    }
}

class ValidationException(string message) : Exception(message);

class ValidationWarningException(string message) : Exception(message);