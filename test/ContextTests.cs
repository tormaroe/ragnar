namespace rebelly.tests;

public class ContextTests
{
    [Fact]
    public void Context_Hierarchy_ResolvesCorrectly()
    {
        var parent = new Context();
        parent.Set("global", new Integer(100));

        var child = new Context(parent);
        child.Set("local", new Integer(5));

        Assert.Equal(100, ((Integer)child.Get("global")).Number);
        Assert.Equal(5, ((Integer)child.Get("local")).Number);
        Assert.Throws<Exception>(() => parent.Get("local"));
    }
}