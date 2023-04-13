using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Benday.CommandsFramework.Tests;

[TestClass]
public class LineWrapUtilitiesFixture
{

    [TestMethod]
    public void WrapString_NoWrappingRequired()
    {
        int linePadding = 10;
        int maxLineLength = 50;

        var input = "a1234567890 b1234567890 c1234567890";
        var expected = "a1234567890 b1234567890 c1234567890";

        var actual = LineWrapUtilities.WrapValue(linePadding, maxLineLength, input);

        Assert.AreEqual<string>(expected, actual, "Wrong value");
    }

    [TestMethod]
    public void WrapString_WrappingRequired_TwoLines()
    {
        int linePadding = 10;
        int maxLineLength = 50;
        var expectedPaddingString = "          ";

        var input = "a1234567890 b1234567890 c1234567890 d1234567890 e1234567890 f1234567890";
        var expected = "a1234567890 b1234567890 c1234567890" + Environment.NewLine +
            expectedPaddingString + "d1234567890 e1234567890 f1234567890";

        var actual = LineWrapUtilities.WrapValue(linePadding, maxLineLength, input);

        Assert.AreEqual<string>(expected, actual, "Wrong value");
    }

    [TestMethod]
    public void WrapString_WrappingRequired_ThreeLines()
    {
        int linePadding = 10;
        int maxLineLength = 50;
        var expectedPaddingString = "          ";

        var input = "a1234567890 b1234567890 c1234567890 d1234567890 e1234567890 f1234567890 g1234567890 h1234567890 i1234567890";
        var expected = "a1234567890 b1234567890 c1234567890" + Environment.NewLine +
            expectedPaddingString + "d1234567890 e1234567890 f1234567890" + Environment.NewLine +
            expectedPaddingString + "g1234567890 h1234567890 i1234567890";

        var actual = LineWrapUtilities.WrapValue(linePadding, maxLineLength, input);

        Assert.AreEqual<string>(expected, actual, "Wrong value");
    }

    [TestMethod]
    public void WrapString_WrappingRequired_FourLines()
    {
        int linePadding = 10;
        int maxLineLength = 50;
        var expectedPaddingString = "          ";

        var input = "a1234567890 b1234567890 c1234567890 d1234567890 e1234567890 f1234567890 g1234567890 h1234567890 i1234567890 j1234567890 k1234567890 l1234567890 m1234567890";
        var expected = "a1234567890 b1234567890 c1234567890" + Environment.NewLine +
            expectedPaddingString + "d1234567890 e1234567890 f1234567890" + Environment.NewLine +
            expectedPaddingString + "g1234567890 h1234567890 i1234567890" + Environment.NewLine +
            expectedPaddingString + "j1234567890 k1234567890 l1234567890" + Environment.NewLine +
            expectedPaddingString + "m1234567890";

        var actual = LineWrapUtilities.WrapValue(linePadding, maxLineLength, input);

        Assert.AreEqual<string>(expected, actual, "Wrong value");
    }

}
