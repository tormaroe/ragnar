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

        ctx.Set("compose", new Native((args, refs, context, interpreter, isTail) =>
        {
            if (args[0] is not Block inputBlock)
                throw new Exception("compose expects a block.");

            var results = new List<Value>();
            
            // Shallow compose: we only look at the top-level elements of the block.
            foreach (var child in inputBlock.Children.Skip(inputBlock.Index))
            {
                if (child is Paren paren)
                {
                    // Evaluate the paren
                    var evaluated = interpreter.Evaluate(paren, context);
                    
                    // Splice if it's a block, otherwise just append
                    if (evaluated is Block evalBlock)
                    {
                        results.AddRange(evalBlock.Children.Skip(evalBlock.Index));
                    }
                    else if (evaluated is not Word w || w.Name != "unset") // if it evaluated to unset, append nothing (rebol behavior)? Wait, in Rebol it appends unless it's unset. Let's just append. Actually, we should check if it's 'none', in Rebol 'none' is appended as none. Unset is not appended. But in Ragnar we might not have Unset value yet. We will just append the value.
                    {
                        results.Add(evaluated);
                    }
                }
                else
                {
                    results.Add(child);
                }
            }

            return new Block(results);
        }, 1).WithTitle("Evaluates only paren expressions within a block, splicing block results."));

        // block? [value]
        ctx.Set("block?", new Native((args, refs, _, _, _) =>
        {
            return new Logic(args[0] is Block);
        }, 1).WithTitle("Returns true if the value is a block."));
    }
}
