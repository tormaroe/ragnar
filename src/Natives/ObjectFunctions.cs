
namespace Ragnar.Natives;

public static class ObjectFunctions
{
    public static void Add(Context ctx)
    {
        // object! (type constant)
        ctx.Set("object!", new Word("object!"));

        // make object! [ ... ]
        ctx.Set("make", new Native((args, refs, context, interpreter, isTail) =>
        {
            if (args.Count < 2) throw new Exception("make requires 2 arguments.");
            
            var typeWord = args[0] as Word;
            if (typeWord?.Name == "object!")
            {
                if (args[1] is not Block block) throw new Exception("make object! requires a block.");

                // Create a new context for the object
                var objContext = new Context(context);
                var obj = new ObjectValue(objContext);
                
                // Add 'self' to the context
                objContext.SetLocal("self", obj);

                // Evaluate the block in the object context
                interpreter.Evaluate(block, objContext);

                return obj;
            }

            throw new Exception($"make does not support type {args[0]}");
        }, 2).WithTitle("Creates a new value or object."));

        // in object word
        ctx.Set("in", new Native((args, refs, context, interpreter, isTail) =>
        {
            if (args[0] is not ObjectValue obj) throw new Exception("in requires an object.");
            if (args[1] is not Word word) throw new Exception("in requires a word.");

            // Return a new word bound to the object's context
            return new Word(word.Name, obj.Context);
        }, 2).WithTitle("Returns a word bound to an object's context."));

        // get word
        ctx.Set("get", new Native((args, refs, context, interpreter, isTail) =>
        {
            if (args[0] is Word w)
            {
                if (w.Binding != null) return w.Binding.Get(w.Name);
                return context.Get(w.Name);
            }
            return args[0]; // get on non-word is identity
        }, 1).WithTitle("Returns the value of a word."));

        // set word value
        ctx.Set("set", new Native((args, refs, context, interpreter, isTail) =>
        {
            if (args[0] is Word w)
            {
                if (w.Binding != null) w.Binding.Set(w.Name, args[1]);
                else context.Set(w.Name, args[1]);
                return args[1];
            }
            throw new Exception("set requires a word.");
        }, 2).WithTitle("Sets a word to a value."));

        // bind block object
        ctx.Set("bind", new Native((args, refs, context, interpreter, isTail) =>
        {
            if (args[0] is not Block block) throw new Exception("bind requires a block.");
            if (args[1] is not ObjectValue obj) throw new Exception("bind requires an object.");

            BindBlock(block, obj.Context);
            return block;
        }, 2).WithTitle("Binds all words in a block to an object's context."));

        // context?
        ctx.Set("context?", new Native((args, refs, context, interpreter, isTail) =>
        {
            return new ObjectValue(context);
        }, 0).WithTitle("Returns the current context as an object."));
    }

    private static void BindBlock(Block block, Context context)
    {
        foreach (var child in block.Children)
        {
            if (child is Word w) w.Binding = context;
            else if (child is Block b) BindBlock(b, context);
        }
    }
}
