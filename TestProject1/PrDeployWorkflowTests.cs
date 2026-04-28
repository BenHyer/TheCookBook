namespace TestProject1;

public class PrDeployWorkflowTests
{
    [Fact]
    public void PreviewWorkflow_UsesStablePreviewUrlsAndCommitSpecificImageTags()
    {
        var workflow = File.ReadAllText(GetWorkflowPath()).Replace("\r\n", "\n");

        Assert.Contains("on:\n  pull_request:\n    types: [ opened, synchronize, reopened, closed ]", workflow);
        Assert.Contains("NS: cookbook-pr-${{ github.event.number }}", workflow);
        Assert.Contains("BENCAMPUS_HOST: pr-${{ github.event.number }}.bencampus.duckdns.org", workflow);
        Assert.Contains("BENHHOME_HOST: pr-${{ github.event.number }}.benhhome.duckdns.org", workflow);
        Assert.Contains("benhyer/cookbook-api:pr-${{ github.event.number }}-${{ github.sha }}", workflow);
        Assert.Contains("benhyer/cookbook-web:pr-${{ github.event.number }}-${{ github.sha }}", workflow);
        Assert.Contains("IMAGE_TAG: pr-${{ github.event.number }}-${{ github.sha }}", workflow);
        Assert.Contains("${sha}", workflow);
        Assert.Contains("updates automatically on each push.", workflow);
    }

    private static string GetWorkflowPath()
    {
        var repoRoot = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            ".."));

        return Path.Combine(repoRoot, ".github", "workflows", "pr-deploy.yml");
    }
}