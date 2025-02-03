using System;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace AdvocateValidation
{
    class CloudAdvocateYamlModel
    {
        [YamlMember(Alias = "uid")]
        public string Uid { get; init; } = string.Empty;

        [YamlMember(Alias = "name")]
        public string Name { get; init; } = string.Empty;

        [YamlMember(Alias = "metadata")]
        public Metadata Metadata { get; init; } = new();

        [YamlMember(Alias = "remarks")]
        public string Remarks { get; init; } = string.Empty;

        [YamlMember(Alias = "tagline")]
        public string Tagline { get; init; } = string.Empty;

        [YamlMember(Alias = "image")]
        public Image Image { get; init; } = new Image();

        [YamlMember(Alias = "connect")]
        public List<Connect> Connect { get; init; } = new();

        [YamlMember(Alias = "location")]
        public Location Location { get; init; } = new Location();
    }

    class Connect
    {
        [YamlMember(Alias = "title")]
        public string Title { get; init; } = string.Empty;

        [YamlMember(Alias = "url")]
        public Uri? Url { get; init; }
    }

    class Image
    {
        [YamlMember(Alias = "alt")]
        public string Alt { get; init; } = string.Empty;

        [YamlMember(Alias = "src")]
        public string Src { get; init; } = string.Empty;
    }

    class Location
    {
        [YamlMember(Alias = "display")]
        public string Display { get; init; } = string.Empty;

        [YamlMember(Alias = "lat")]
        public double Lat { get; init; }

        [YamlMember(Alias = "long")]
        public double Long { get; init; }
    }

    class Metadata
    {
        [YamlMember(Alias = "title")]
        public string Title { get; init; } = string.Empty;

        [YamlMember(Alias = "description")]
        public string Description { get; init; } = string.Empty;

        [YamlMember(Alias = "ms.author")]
        public string Alias { get; init; } = string.Empty;

        [YamlMember(Alias = "team")]
        public string Team { get; init; } = string.Empty;

        [YamlMember(Alias = "dockerCaptain")]
        public bool DockerCaptain { get; init; }

        [YamlMember(Alias = "langchainCommunityCampion")]
        public bool LangchainCommunityCampion { get; init; }

        [YamlMember(Alias = "hasiCorpAmbassador")]
        public bool HasiCorpAmbassador { get; init; }

        [YamlMember(Alias = "gde")]
        public bool Gde { get; init; }

        [YamlMember(Alias = "javaChampion")]
        public bool JavaChampion { get; init; }

        [YamlMember(Alias = "cncfAmbassador")]
        public bool CncfAmbassador { get; init; }

        [YamlMember(Alias = "mvp")]
        public bool Mvp { get; init; }

        [YamlMember(Alias = "rd")]
        public bool Rd { get; init; }

        [YamlMember(Alias = "vExpert")]
        public bool VExpert { get; init; }

        [YamlMember(Alias = "mct")]
        public bool Mct { get; init; }

        [YamlMember(Alias = "mlsa")]
        public bool Mlsa { get; init; }

        [YamlMember(Alias = "jakartaEEAmbassador")]
        public bool JakartaEEAmbassador { get; init; }
    }
}
