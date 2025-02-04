using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.NativeAot;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Benchmarks;

public partial class ProjectParser {

    [GeneratedRegex(@"<\s*PublishAot\s*>\s*true\s*</\s*PublishAot\s*>", RegexOptions.IgnoreCase)]
    private static partial Regex publishAotPattern();

    [GeneratedRegex(@"<\s*PublishSingleFile\s*>\s*true\s*</\s*PublishSingleFile\s*>", RegexOptions.IgnoreCase)]
    private static partial Regex publishSingleFilePattern();

    private const string NORMAL_CSPROJ_FILENAME      = @"C:\Users\Ben\Documents\Projects\Kasa\Kasa\Kasa.csproj";
    private const string SINGLE_FILE_CSPROJ_FILENAME = @"C:\Users\Ben\Documents\Projects\GandiDynamicDns\GandiDynamicDns\GandiDynamicDns.csproj";

    private readonly string normalCsprojContents     = File.ReadAllText(NORMAL_CSPROJ_FILENAME, Encoding.UTF8);
    private readonly string singleFileCsprojContents = File.ReadAllText(SINGLE_FILE_CSPROJ_FILENAME, Encoding.UTF8);

    private static readonly XPathExpression XPATH_SINGLE_FILE = XPathExpression.Compile("/Project/PropertyGroup[PublishSingleFile[text() = \"true\"] or PublishAot[text() = \"true\"]]");

    [Benchmark]
    public bool regexFound() => publishAotPattern().IsMatch(singleFileCsprojContents) || publishSingleFilePattern().IsMatch(singleFileCsprojContents);

    [Benchmark]
    public bool regexNotFound() => publishAotPattern().IsMatch(normalCsprojContents) || publishSingleFilePattern().IsMatch(normalCsprojContents);

    [Benchmark]
    public bool linqFound() => linqHasElementWithText(singleFileCsprojContents, ["PublishAot", "PublishSingleFile"], "true");

    [Benchmark]
    public bool linqNotFound() => linqHasElementWithText(normalCsprojContents, ["PublishAot", "PublishSingleFile"], "true");

    [Benchmark]
    public bool domFound() => domHasElementWithText(singleFileCsprojContents, ["PublishAot", "PublishSingleFile"], "true");

    [Benchmark]
    public bool domNotFound() => domHasElementWithText(normalCsprojContents, ["PublishAot", "PublishSingleFile"], "true");

    [Benchmark]
    public bool xpathFound() => xpathHasElementWithText(singleFileCsprojContents);

    [Benchmark]
    public bool xpathNotFound() => xpathHasElementWithText(normalCsprojContents);

    private static bool linqHasElementWithText(string xmlDoc, IEnumerable<string> elementNames, string elementText) => XDocument.Parse(xmlDoc)
        .Elements()
        .IntersectBy(elementNames, element => element.Name.LocalName, StringComparer.OrdinalIgnoreCase)
        .Any(element => element.FirstNode is XText text && text.Value.Equals(elementText, StringComparison.OrdinalIgnoreCase));

    private static bool domHasElementWithText(string xmlDoc, IEnumerable<string> elementNames, string elementText) {
        XmlDocument doc = new();
        doc.LoadXml(xmlDoc);
        return elementNames.SelectMany(elementName => doc.GetElementsByTagName(elementName).Cast<XmlElement>())
            .Any(el => el.InnerText.Equals(elementText, StringComparison.OrdinalIgnoreCase));
    }

    private static bool xpathHasElementWithText(string xmlDoc) {
        using StringReader stringReader = new(xmlDoc);
        XPathDocument      doc          = new(stringReader);
        XPathNavigator     xpath        = doc.CreateNavigator();
        return xpath.Matches(XPATH_SINGLE_FILE);
    }

}

internal static class Program {

    private static void Main(string[] args) {
        IConfig config = DefaultConfig.Instance.AddJob(Job.ShortRun);
        config = config.AddJob(Job.ShortRun.WithToolchain(NativeAotToolchain.CreateBuilder()
            .UseNuGet()
            .IlcGenerateCompleteTypeMetadata(false) // Fix "InvalidCastException: Specified cast is not valid" error when using [ShortRunJob(RuntimeMoniker.NativeAot90)]
            .DisplayName("AOT")
            .TargetFrameworkMoniker("net9.0")
            .ToToolchain()));

        BenchmarkRunner.Run<ProjectParser>(config, args);
    }

}