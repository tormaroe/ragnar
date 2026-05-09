namespace rebelly.tests;

public class LoaderTests
{
    [Fact]
    public void Load_NestedBlocks_CreatesCorrectHierarchy()
    {
        var input = "[ 1 [ 2 ] ]";
        var tokens = new Lexer(input).Tokenize();
        var root = new Loader().Load(tokens);

        // root is the invisible program block, so root.Children[0] is our outer [ ]
        var outerBlock = Assert.IsType<Block>(root.Children[0]);
        Assert.Equal(2, outerBlock.Children.Count);
        Assert.IsType<Integer>(outerBlock.Children[0]);
        Assert.IsType<Block>(outerBlock.Children[1]);
    }
}