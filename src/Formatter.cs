using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ragnar;

public static class Formatter
{
    public static string Format(Value value, int indentLevel = 0, Context? context = null)
    {
        string indent = new string(' ', indentLevel * 4);

        if (value is Block block)
        {
            if (value is Paren paren)
            {
                return FormatParenBody(paren, indentLevel, context);
            }
            return FormatBlockBody(block, indentLevel, context);
        }

        if (value is Function func)
        {
            var sb = new StringBuilder();
            sb.Append("func [ ");
            if (!string.IsNullOrEmpty(func.Title))
            {
                sb.Append($"\"{func.Title.Replace("\"", "\\\"")}\" ");
            }
            foreach (var p in func.MainParameters)
            {
                if (!p.Evaluate) sb.Append("'");
                sb.Append(p.Name + " ");
            }
            foreach (var r in func.Refinements)
            {
                sb.Append("/" + r.Name + " ");
                foreach (var arg in r.Args) sb.Append(arg + " ");
            }
            sb.Append("] ");
            sb.Append(FormatBlockBody(func.Body, indentLevel, context));
            return sb.ToString();
        }

        if (value is ObjectValue obj)
        {
            var sb = new StringBuilder();
            sb.AppendLine("make object! [");
            string nextIndent = new string(' ', (indentLevel + 1) * 4);
            foreach (var kvp in obj.Context.GetOwnBindings())
            {
                if (kvp.Key == "self") continue;
                sb.Append(nextIndent);
                sb.Append(kvp.Key);
                sb.Append(": ");
                sb.AppendLine(Format(kvp.Value, indentLevel + 1, context));
            }
            sb.Append(indent);
            sb.Append("]");
            return sb.ToString();
        }

        return value.ToString();
    }

    public static string FormatBlockChildren(Block block, int indentLevel = 0, Context? context = null)
    {
        if (block.Children.Count == 0) return "";

        var sb = new StringBuilder();
        string indent = new string(' ', indentLevel * 4);
        var exprs = GroupIntoExpressions(block.Children, context);
        for (int i = 0; i < exprs.Count; i++)
        {
            sb.Append(indent);
            sb.AppendLine(FormatExpression(exprs[i], indentLevel, context));
        }
        return sb.ToString().TrimEnd();
    }

    private static string FormatBlockBody(Block block, int indentLevel, Context? context, bool isObject = false, bool isSwitchCases = false, bool forceMultiline = false)
    {
        if (block.Children.Count == 0) return "[]";

        if (!isObject && !isSwitchCases && !forceMultiline && IsSimpleBlock(block))
        {
            return "[ " + string.Join(" ", block.Children.Select(c => Format(c, 0, context))) + " ]";
        }

        string indent = new string(' ', indentLevel * 4);
        string nextIndent = new string(' ', (indentLevel + 1) * 4);
        var sb = new StringBuilder();
        sb.AppendLine("[");

        if (isSwitchCases)
        {
            int idx = 0;
            while (idx < block.Children.Count)
            {
                var choice = block.Children[idx++];
                sb.Append(nextIndent);
                sb.Append(Format(choice, indentLevel + 1, context));

                if (idx < block.Children.Count)
                {
                    var val = block.Children[idx++];
                    sb.Append(" ");
                    if (val is Block valBlock)
                    {
                        sb.AppendLine(FormatBlockBody(valBlock, indentLevel + 1, context, forceMultiline: true));
                    }
                    else
                    {
                        sb.AppendLine(Format(val, indentLevel + 1, context));
                    }
                }
                else
                {
                    sb.AppendLine();
                }
            }
        }
        else
        {
            var exprs = GroupIntoExpressions(block.Children, context);
            foreach (var expr in exprs)
            {
                sb.Append(nextIndent);
                sb.AppendLine(FormatExpression(expr, indentLevel + 1, context));
            }
        }

        sb.Append(indent);
        sb.Append("]");
        return sb.ToString();
    }

    private static string FormatParenBody(Paren paren, int indentLevel, Context? context)
    {
        if (paren.Children.Count == 0) return "()";
        if (IsSimpleBlock(paren))
        {
            return "( " + string.Join(" ", paren.Children.Select(c => Format(c, 0, context))) + " )";
        }

        string indent = new string(' ', indentLevel * 4);
        string nextIndent = new string(' ', (indentLevel + 1) * 4);
        var sb = new StringBuilder();
        sb.AppendLine("(");
        var exprs = GroupIntoExpressions(paren.Children, context);
        foreach (var expr in exprs)
        {
            sb.Append(nextIndent);
            sb.AppendLine(FormatExpression(expr, indentLevel + 1, context));
        }
        sb.Append(indent);
        sb.Append(")");
        return sb.ToString();
    }

    private static string FormatExpression(List<Value> expr, int indentLevel, Context? context)
    {
        if (expr.Count == 0) return "";

        var first = expr[0];
        string firstName = "";
        if (first is Word w) firstName = w.Name;
        else if (first is Path p && p.Parts.Count > 0 && p.Parts[0] is Word wHead) firstName = wHead.Name;

        // 1. make object! [ ... ]
        if (expr.Count == 3 && firstName == "make" && expr[1] is Word w2 && w2.Name == "object!" && expr[2] is Block objBody)
        {
            return "make object! " + FormatBlockBody(objBody, indentLevel, context, isObject: true);
        }

        // 2. func [spec] [body], does [body]
        if (expr.Count == 3 && firstName == "func" && expr[1] is Block spec && expr[2] is Block body)
        {
            return "func " + Format(spec, 0, context) + " " + FormatBlockBody(body, indentLevel, context, forceMultiline: true);
        }
        if (expr.Count == 2 && firstName == "does" && expr[1] is Block doesBody)
        {
            return "does " + FormatBlockBody(doesBody, indentLevel, context, forceMultiline: true);
        }

        // 3. either cond [ block1 ] [ block2 ]
        if (expr.Count >= 3 && firstName == "either" && expr[^2] is Block b1 && expr[^1] is Block b2)
        {
            var cond = expr.Skip(1).Take(expr.Count - 3).ToList();
            string condStr = string.Join(" ", cond.Select(c => Format(c, 0, context)));
            return $"either {condStr} " + FormatBlockBody(b1, indentLevel, context, forceMultiline: true) + " " + FormatBlockBody(b2, indentLevel, context, forceMultiline: true);
        }

        // 4. if, foreach, while, loop, forever
        if (expr.Count >= 2 && (firstName == "if" || firstName == "foreach" || firstName == "while" || firstName == "loop" || firstName == "forever") && expr[^1] is Block loopBody)
        {
            var prefix = expr.Take(expr.Count - 1).ToList();
            string prefixStr = string.Join(" ", prefix.Select(p => Format(p, 0, context)));
            return prefixStr + " " + FormatBlockBody(loopBody, indentLevel, context, forceMultiline: true);
        }

        // 5. switch / switch/default
        if (firstName == "switch" && expr.Count >= 3)
        {
            if (expr[^2] is Block casesBlock && expr[^1] is Block defaultBlock)
            {
                var prefix = expr.Take(expr.Count - 2).ToList();
                string prefixStr = string.Join(" ", prefix.Select(p => Format(p, 0, context)));
                return prefixStr + " " + FormatBlockBody(casesBlock, indentLevel, context, isSwitchCases: true) + " " + FormatBlockBody(defaultBlock, indentLevel, context, forceMultiline: true);
            }
            else if (expr[^1] is Block casesBlockOnly)
            {
                var prefix = expr.Take(expr.Count - 1).ToList();
                string prefixStr = string.Join(" ", prefix.Select(p => Format(p, 0, context)));
                return prefixStr + " " + FormatBlockBody(casesBlockOnly, indentLevel, context, isSwitchCases: true);
            }
        }

        // Default: join with spaces
        return string.Join(" ", expr.Select(v => Format(v, indentLevel, context)));
    }

    private static bool IsSimpleBlock(Block block)
    {
        if (block.Children.Count > 6) return false;

        foreach (var child in block.Children)
        {
            if (child is Block or Paren or Function or ObjectValue)
                return false;
        }

        int totalLen = 0;
        foreach (var child in block.Children)
        {
            totalLen += child.ToString().Length + 1;
        }
        if (totalLen > 40) return false;

        return true;
    }

    public static List<List<Value>> GroupIntoExpressions(List<Value> values, Context? context)
    {
        var expressions = new List<List<Value>>();
        int index = 0;
        while (index < values.Count)
        {
            expressions.Add(NextExpression(values, ref index, context));
        }
        return expressions;
    }

    private static List<Value> NextExpression(List<Value> values, ref int index, Context? context)
    {
        if (index >= values.Count) return new List<Value>();

        var result = NextPrefixExpression(values, ref index, context);

        while (index < values.Count)
        {
            var nextVal = values[index];
            if (nextVal is Word w && context != null && context.TryGet(w.Name, out var opVal) && opVal is Op)
            {
                index++;
                var right = NextPrefixExpression(values, ref index, context);
                result.Add(nextVal);
                result.AddRange(right);
            }
            else
            {
                break;
            }
        }

        return result;
    }

    private static List<Value> NextPrefixExpression(List<Value> values, ref int index, Context? context)
    {
        if (index >= values.Count) return new List<Value>();

        var current = values[index++];
        var result = new List<Value> { current };

        if (current is SetWord)
        {
            result.AddRange(NextExpression(values, ref index, context));
            return result;
        }

        if (current is Word w)
        {
            int arity = GetWordArity(w.Name, context);
            for (int i = 0; i < arity; i++)
            {
                result.AddRange(NextExpression(values, ref index, context));
            }
            return result;
        }

        if (current is Path path)
        {
            int arity = GetPathArity(path, context);
            for (int i = 0; i < arity; i++)
            {
                result.AddRange(NextExpression(values, ref index, context));
            }
            return result;
        }

        return result;
    }

    private static int GetWordArity(string name, Context? context)
    {
        if (context == null) return 0;
        if (context.TryGet(name, out var val))
        {
            if (val is Native n) return n.Arity;
            if (val is Function f) return f.MainParameters.Count;
        }
        return 0;
    }

    private static int GetPathArity(Path path, Context? context)
    {
        if (context == null) return 0;
        try
        {
            var first = path.Parts[0];
            Value currentVal;
            if (first is GetWord gw)
            {
                currentVal = context.Get(gw.Name);
            }
            else if (first is Word w)
            {
                currentVal = context.Get(w.Name);
            }
            else
            {
                return 0;
            }

            int i = 1;
            for (; i < path.Parts.Count; i++)
            {
                var segment = path.Parts[i];
                if (currentVal is Native or Function)
                {
                    break;
                }
                if (currentVal is ObjectValue obj && segment is Word key)
                {
                    currentVal = obj.Context.Get(key.Name);
                }
                else
                {
                    return 0;
                }
            }

            if (currentVal is Native n) return n.Arity;
            if (currentVal is Function f)
            {
                int arity = f.MainParameters.Count;
                for (int j = i; j < path.Parts.Count; j++)
                {
                    if (path.Parts[j] is Word rw)
                    {
                        var refSpec = f.Refinements.FirstOrDefault(r => r.Name == rw.Name);
                        if (refSpec.Name != null)
                        {
                            arity += refSpec.Args.Count;
                        }
                    }
                }
                return arity;
            }
        }
        catch
        {
            // Ignore resolution errors
        }
        return 0;
    }
}
