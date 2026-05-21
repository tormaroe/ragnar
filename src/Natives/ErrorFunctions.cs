namespace Ragnar.Natives;

public static class ErrorFunctions
{
    public static void Add(Context ctx)
    {
        // try [block]
        ctx.Set("try", new Native((args, refs, context, interpreter, isTail) =>
        {
            if (args[0] is not Block block)
                throw new Exception("try expects a block.");

            try
            {
                return interpreter.Evaluate(block, context, isTail);
            }
            // Do not catch Break/Continue/Return exceptions as they are control flow within the interpreter
            catch (BreakException) { throw; }
            catch (ContinueException) { throw; }
            catch (ReturnException) { throw; }
            catch (ThrowException) { throw; } // 'throw' control flow exception
            catch (Exception ex)
            {
                return new ErrorValue(ex.Message, ex);
            }
        }, 1).WithTitle("Tries to evaluate a block and returns its value or an error."));

        // attempt [block]
        ctx.Set("attempt", new Native((args, refs, context, interpreter, isTail) =>
        {
            if (args[0] is not Block block)
                throw new Exception("attempt expects a block.");

            try
            {
                return interpreter.Evaluate(block, context, isTail);
            }
            catch (BreakException) { throw; }
            catch (ContinueException) { throw; }
            catch (ReturnException) { throw; }
            catch (ThrowException) { throw; }
            catch (Exception)
            {
                return new Word("none");
            }
        }, 1).WithTitle("Tries to evaluate a block and returns its value or none on error."));

        // catch [block]
        ctx.Set("catch", new Native((args, refs, context, interpreter, isTail) =>
        {
            if (args[0] is not Block block)
                throw new Exception("catch expects a block.");

            try
            {
                return interpreter.Evaluate(block, context, isTail);
            }
            catch (ThrowException ex)
            {
                return ex.ThrownValue;
            }
        }, 1).WithTitle("Catches a throw from a block and returns its value."));

        // throw value
        ctx.Set("throw", new Native((args, refs, context, interpreter, isTail) =>
        {
            throw new ThrowException(args[0]);
        }, 1).WithTitle("Throws a value from a block, to be handled by catch."));
    }
}
