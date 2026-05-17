namespace Ragnar.Natives;

public static class BlockFunctions
{
    public static void Add(Context ctx)
    {
        ctx.Set("reduce", new Native((args, refs, context, interpreter, isTail) =>
        {
            if (args[0] is not Block inputBlock)
                throw new Exception("reduce expects a block.");

            var results = new List<Value>();
            int index = 0;

            // We keep calling Next until we've exhausted the block
            while (index < inputBlock.Children.Count)
            {
                results.Add(interpreter.Next(inputBlock, ref index, context));
            }

            return new Block(results);
        }, 1).WithTitle("Evaluates expressions within a block and returns a new block with the results."));

        // block? [value]
        ctx.Set("block?", new Native((args, refs, _, _, _) =>
        {
            return new Logic(args[0] is Block);
        }, 1).WithTitle("Returns true if the value is a block."));
    }
}
