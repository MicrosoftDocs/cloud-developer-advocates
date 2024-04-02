using System.Net;
using System.Text;
using AdvocateValidation;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;

int MAX_RETRIES = 3;

bool debug = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") == "Development";

var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
});

ILogger<Program> logger = loggerFactory.CreateLogger<Program>();

string[] linkTypesToIgnore = ["LinkedIn", "Reddit"];

string _advocatesPath = debug ?
    Path.Combine("../../../../", "advocates") :
    Path.Combine("../", "advocates");

IDeserializer _yamlDeserializer = new DeserializerBuilder().Build();

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
            logger.LogInformation("{Title} doesn't like being validated, skipping {Url} from {filePath}.", connect.Title, connect.Url, filePath);
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

IEnumerable<string> duplicateAliasList = advocateList
    .GroupBy(x => x.Metadata.Alias)
    .Where(g => g.Count() > 1)
    .Select(x => x.Key);

foreach (string duplicateAlias in duplicateAliasList)
{
    parsingErrors.Add(("", $"Duplicate Alias Found; ms.author: {duplicateAlias}"));
}

foreach ((string filePath, string parsingWarning) in parsingWarnings)
{
    logger.LogWarning("::warning file={filePath}::{parsingWarning}", filePath, parsingWarning);
}

if (parsingErrors.Count != 0)
{
    logger.LogError("Validation Failed");
    foreach ((string filePath, string parsingError) in parsingErrors)
    {
        logger.LogError("::error file={filePath}::{parsingError}", filePath, parsingError);
    }
    throw new Exception("Validation Failed");
}
else
{
    logger.LogInformation("Validation Completed Successfully");
}

async IAsyncEnumerable<(string filePath, CloudAdvocateYamlModel advocate)> GetAdvocateYmlFiles(IEnumerable<string> files)
{
    var ymlFiles = files.Where(x => x.EndsWith(".yml", StringComparison.OrdinalIgnoreCase));

    foreach (var filePath in ymlFiles)
    {
        var text = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);

        if (text.StartsWith("### YamlMime:Profile") && !text.StartsWith("### YamlMime:ProfileList"))
        {
            logger.LogInformation("Parsing {filePath}", filePath);
            yield return (filePath, ParseAdvocateFromYaml(text));
        }
    }
}

CloudAdvocateYamlModel ParseAdvocateFromYaml(in string fileText)
{
    var stringReaderFile = new StringReader(fileText);

    return _yamlDeserializer.Deserialize<CloudAdvocateYamlModel>(stringReaderFile);
}

async Task EnsureValidUri(string filePath, Uri? uri, string uriName)
{
    if (uri is null)
        throw new ValidationException($"Missing '{uriName}' Url: {uri} - File: {filePath}");

    if (!uri.IsWellFormedOriginalString())
        throw new ValidationException($"URI for '{uriName}' is malformed. Url: {uri} - File: {filePath}");

    if (uri.Scheme == Uri.UriSchemeHttp)
        logger.LogWarning("::warning file={filePath}:: '{uriName}' Url is HTTP, you really should be hosting on HTTPS. Url: {uri}.", filePath, uriName, uri);

    HttpClient _client = new();
    _client.DefaultRequestHeaders.UserAgent.Clear();
    _client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36 Edg/122.0.0.0");
    HttpResponseMessage response = await _client.GetAsync(uri).ConfigureAwait(false);

    if (!response.IsSuccessStatusCode && response.StatusCode is HttpStatusCode.NotFound)
        throw new ValidationException($"Failed to resolve URI for '{uriName}' ({response.StatusCode}). Url: {uri} - File: {filePath}");
    else if (!response.IsSuccessStatusCode)
        throw new ValidationWarningException($"Failed to resolve URI for '{uriName}' ({response.StatusCode}) but we're going to ignore it. Url: {uri} - File: {filePath}");
}

async Task EnsureValidGitHubUri(string filePath, Uri? uri, string uriName)
{
    if (uri is null)
        throw new ValidationException($"Missing '{uriName}' Url: {uri} - File: {filePath}");

    if (!uri.IsWellFormedOriginalString())
        throw new ValidationException($"URI for '{uriName}' is malformed. Url: {uri} - File: {filePath}");

    bool hasReceivedGitHubAbuseLimitResponse = false;
    int retryCount = 0;

    do
    {
        if (retryCount >= MAX_RETRIES)
            throw new ValidationException($"Validation of '{uriName}' failed after {MAX_RETRIES} retries. Url: {uri} - File: {filePath}");

        HttpClient _client = new();
        HttpResponseMessage response = await _client.GetAsync(uri).ConfigureAwait(false);

        if (response.IsSuccessStatusCode)
        {
            return;
        }
        else if (response.StatusCode is HttpStatusCode.TooManyRequests)
        {
            TimeSpan retryAfter = response.Headers.RetryAfter switch
            {
                { Delta: TimeSpan delta } => delta,
                { Date: DateTimeOffset date } => date - DateTimeOffset.UtcNow,
                _ => TimeSpan.FromMinutes(1)
            };

            hasReceivedGitHubAbuseLimitResponse = true;
            retryCount++;
            logger.LogWarning("::warning file={filePath}:: GitHub Rate Limit Exceeded. Retrying in {TotalSeconds} seconds.", filePath, retryAfter.TotalSeconds);
            await Task.Delay(retryAfter).ConfigureAwait(false);
        }
        else if (!response.IsSuccessStatusCode)
        {
            throw new ValidationException($"Validation of '{uriName}' failed with {response.StatusCode}. Url: {uri} - File: {filePath}");
        }
    }
    while (hasReceivedGitHubAbuseLimitResponse);
}

void EnsureValidImage(in string filePath, in Image? cloudAdvocateImage)
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
