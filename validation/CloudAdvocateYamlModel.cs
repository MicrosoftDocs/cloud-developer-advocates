using System;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace AdvocateValidation
{
    class CloudAdvocateYamlModel
    {
        [YamlMember(Alias = "uid")]
        public string Uid { get; set; } = string.Empty;

        [YamlMember(Alias = "name")]
        public string Name { get; set; } = string.Empty;

        [YamlMember(Alias = "metadata")]
        public Metadata Metadata { get; set; } = new Metadata();

        [YamlMember(Alias = "remarks")]
        public string Remarks { get; set; } = string.Empty;

        [YamlMember(Alias = "tagline")]
        public string Tagline { get; set; } = string.Empty;

        [YamlMember(Alias = "image")]
        public Image Image { get; set; } = new Image();

        [YamlMember(Alias = "connect")]
        public List<Connect> Connect { get; set; } = new List<Connect>();

        [YamlMember(Alias = "location")]
        public Location Location { get; set; } = new Location();
    }

    class Connect
    {
        [YamlMember(Alias = "title")]
        public string Title { get; set; } = string.Empty;

        [YamlMember(Alias = "url")]
        public Uri? Url { get; set; }
    }

    class Image
    {
        [YamlMember(Alias = "alt")]
        public string Alt { get; set; } = string.Empty;

        [YamlMember(Alias = "src")]
        public string Src { get; set; } = string.Empty;
    }

    class Location
    {
        [YamlMember(Alias = "display")]
        public string Display { get; set; } = string.Empty;

        [YamlMember(Alias = "lat")]
        public double Lat { get; set; }

        [YamlMember(Alias = "long")]
        public double Long { get; set; }
    }

    class Metadata
    {
        [YamlMember(Alias = "title")]
        public string Title { get; set; } = string.Empty;

        [YamlMember(Alias = "description")]
        public string Description { get; set; } = string.Empty;

        [YamlMember(Alias = "ms.author")]
        public string Alias { get; set; } = string.Empty;
    }
}
