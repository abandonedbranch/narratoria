using Microsoft.VisualStudio.TestTools.UnitTesting;
using Narratoria.Components;

namespace Narratoria.Tests;

[TestClass]
public class StageMetadataProviderTests
{
    [TestMethod]
    public void EstimatePromptTokens_WordCount()
    {
        Assert.AreEqual(0, StageMetadataProvider.EstimatePromptTokens(""));
        Assert.AreEqual(3, StageMetadataProvider.EstimatePromptTokens("You enter tavern"));
    }
}
