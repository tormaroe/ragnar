
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
                objContext.Set("self", obj);

                // Evaluate the block in the object context
                interpreter.Evaluate(block, objContext);

                return obj;
            }

            throw new Exception($"make does not support type {args[0]}");
        }, 2));

        // in object word
        ctx.Set("in", new Native((args, refs, context, interpreter, isTail) =>
        {
            if (args[0] is not ObjectValue obj) throw new Exception("in requires an object.");
            if (args[1] is not Word word) throw new Exception("in requires a word.");

            // Return a new word bound to the object's context
            return new Word(word.Name, obj.Context);
        }, 2));

        // get word
        ctx.Set("get", new Native((args, refs, context, interpreter, isTail) =>
        {
            if (args[0] is Word w)
            {
                if (w.Binding != null) return w.Binding.Get(w.Name);
                return context.Get(w.Name);
            }
            return args[0]; // get on non-word is identity
        }, 1));
    }
}
