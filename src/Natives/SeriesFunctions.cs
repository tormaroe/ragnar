namespace Ragnar.Natives;

public static class SeriesFunctions
{
    public static void Add(Context ctx)
    {
        // Helper for positional access relative to series index
        static Value GetAt(Series s, int offset)
        {
            int target = s.Index + offset;
            if (s is Block b)
            {
                return (target >= 0 && target < b.Children.Count) ? b.Children[target] : new Word("none");
            }
            if (s is Text t)
            {
                return (target >= 0 && target < t.Content.Length) ? new Text(t.Content[target].ToString()) : new Word("none");
            }
            return new Word("none");
        }

        // first [10 20] -> 10
        ctx.Set("first", new Native((args, refinements, _, _, _) =>
        {
            if (args[0] is ObjectValue obj)
            {
                var keys = obj.Context.GetOwnBindings().Keys.Select(k => new Word(k)).ToList();
                return new Block(keys);
            }
            if (args[0] is Series s) return GetAt(s, 0);
            throw new Exception("first requires a series or object.");
        }, 1));

        // second [10 20] -> 20
        ctx.Set("second", new Native((args, refinements, _, _, _) =>
        {
            if (args[0] is Series s) return GetAt(s, 1);
            throw new Exception("second requires a series.");
        }, 1));

        // next [10 20] -> [20]
        ctx.Set("next", new Native((args, refinements, _, _, _) =>
        {
            if (args[0] is Series s) return s.At(s.Index + 1);
            throw new Exception("next requires a series.");
        }, 1));

        // last [10 20] -> 20
        ctx.Set("last", new Native((args, refinements, _, _, _) =>
        {
            if (args[0] is Series s)
            {
                if (s is Block b) return GetAt(s, b.Children.Count - s.Index - 1);
                if (s is Text t) return GetAt(s, t.Content.Length - s.Index - 1);
            }
            throw new Exception("last requires a series.");
        }, 1));

        // length? [1 2 3] -> 3
        ctx.Set("length?", new Native((args, refinements, _, _, _) =>
        {
            if (args[0] is Series s) return new Integer(s.Length);
            throw new Exception("length? requires a series.");
        }, 1));

        // find [series] [value]
        ctx.Set("find", new Native((args, refinements, _, _, _) =>
        {
            if (args[0] is not Series s) throw new Exception("find requires a series.");
            Value target = args[1];

            bool caseSens = refinements.Contains("case");
            bool any = refinements.Contains("any");
            bool last = refinements.Contains("last");
            bool tail = refinements.Contains("tail");
            bool match = refinements.Contains("match");

            if (s is Text t)
            {
                string input = t.Content;
                string search = target is Text targetText ? targetText.Content : target.ToUserString();
                
                int foundPos = -1;
                int matchLength = search.Length;

                if (any)
                {
                    // Convert glob (*, ?) to Regex
                    string pattern = System.Text.RegularExpressions.Regex.Escape(search).Replace("\\*", ".*").Replace("\\?", ".");
                    var options = caseSens ? System.Text.RegularExpressions.RegexOptions.None : System.Text.RegularExpressions.RegexOptions.IgnoreCase;
                    
                    if (match)
                    {
                        var regex = new System.Text.RegularExpressions.Regex("^" + pattern, options);
                        // We need to match the whole substring from current index to some point?
                        // Actually Rebol's find/any/match is quite specific. 
                        // For simplicity, let's just try to match from current index.
                        var m = regex.Match(input, t.Index);
                        if (m.Success && m.Index == t.Index)
                        {
                            foundPos = m.Index;
                            matchLength = m.Length;
                        }
                    }
                    else if (last)
                    {
                        var regex = new System.Text.RegularExpressions.Regex(pattern, options);
                        var matches = regex.Matches(input);
                        var lastMatch = matches.Cast<System.Text.RegularExpressions.Match>().LastOrDefault(m => m.Index >= t.Index);
                        if (lastMatch != null)
                        {
                            foundPos = lastMatch.Index;
                            matchLength = lastMatch.Length;
                        }
                    }
                    else
                    {
                        var regex = new System.Text.RegularExpressions.Regex(pattern, options);
                        var m = regex.Match(input, t.Index);
                        if (m.Success)
                        {
                            foundPos = m.Index;
                            matchLength = m.Length;
                        }
                    }
                }
                else
                {
                    var comparison = caseSens ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
                    if (match)
                    {
                        if (input.AsSpan(t.Index).StartsWith(search, comparison))
                        {
                            foundPos = t.Index;
                        }
                    }
                    else if (last)
                    {
                        foundPos = input.LastIndexOf(search, comparison);
                        if (foundPos < t.Index) foundPos = -1;
                    }
                    else
                    {
                        foundPos = input.IndexOf(search, t.Index, comparison);
                    }
                }

                if (foundPos >= 0)
                {
                    return t.At(foundPos + (tail ? matchLength : 0));
                }
                return new Word("none");
            }

            if (s is Block b)
            {
                int foundIdx = -1;
                int matchLen = 1; // Default for block elements

                // Comparison helper
                bool IsMatch(Value v1, Value v2)
                {
                    if (v1 is Text t1 && v2 is Text t2)
                    {
                        return string.Equals(t1.Content, t2.Content, caseSens ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
                    }
                    return v1.ToString() == v2.ToString();
                }

                if (match)
                {
                    if (s.Index < b.Children.Count && IsMatch(b.Children[s.Index], target))
                    {
                        foundIdx = s.Index;
                    }
                }
                else if (last)
                {
                    for (int i = b.Children.Count - 1; i >= s.Index; i--)
                    {
                        if (IsMatch(b.Children[i], target))
                        {
                            foundIdx = i;
                            break;
                        }
                    }
                }
                else
                {
                    for (int i = s.Index; i < b.Children.Count; i++)
                    {
                        if (IsMatch(b.Children[i], target))
                        {
                            foundIdx = i;
                            break;
                        }
                    }
                }

                if (foundIdx >= 0)
                {
                    return b.At(foundIdx + (tail ? matchLen : 0));
                }
                return new Word("none");
            }

            return new Word("none");
        }, 2));

        // append [1 2] 3 -> [1 2 3]
        ctx.Set("append", new Native((args, refinements, _, _, _) =>
        {
            if (args[0] is Block b)
            {
                b.Children.Add(args[1]);
                return b; // Return the modified block
            }
            if (args[0] is Text t)
            {
                t.Content += args[1].ToUserString();
                return t;
            }
            throw new Exception("append requires a block or text as the first argument.");
        }, 2));

        // join [base] [value]
        ctx.Set("join", new Native((args, refs, context, interpreter, _) =>
        {
            // For now, we'll focus on string concatenation
            // but in a full Rebol clone, join [1 2] 3 would return [1 2 3]
            string baseStr = args[0].ToUserString();
            string appendStr = args[1].ToUserString();

            return new Text(baseStr + appendStr);
        }, 2));

        // rejoin [block]
        ctx.Set("rejoin", new Native((args, refs, context, interpreter, _) =>
        {
            if (args[0] is not Block b)
                throw new Exception("rejoin expects a block.");

            var sb = new System.Text.StringBuilder();
            int index = b.Index;

            // Evaluate each expression in the block and append to string
            while (index < b.Children.Count)
            {
                var evaluated = interpreter.Next(b, ref index, context);
                sb.Append(evaluated.ToUserString());
            }

            return new Text(sb.ToString());
        }, 1));

        // pick [series] [index]
        ctx.Set("pick", new Native((args, refs, _, _, _) =>
        {
            if (args[0] is not Series s) throw new Exception("pick requires a series.");
            if (args[1] is not Integer i) throw new Exception("pick requires an integer index.");

            return GetAt(s, (int)i.Number - 1);
        }, 2));

        // poke [series] [index] [value]
        ctx.Set("poke", new Native((args, refs, _, _, _) =>
        {
            if (args[0] is not Block b) throw new Exception("poke currently only supports blocks.");
            if (args[1] is not Integer i) throw new Exception("poke requires an integer index.");
            
            int targetIdx = b.Index + (int)i.Number - 1;
            if (targetIdx >= 0 && targetIdx < b.Children.Count)
            {
                b.Children[targetIdx] = args[2];
                return args[2];
            }
            throw new Exception("poke index out of range.");
        }, 3));

        // index? [series]
        ctx.Set("index?", new Native((args, refs, _, _, _) =>
        {
            if (args[0] is Series s) return new Integer(s.Index + 1);
            throw new Exception("index? requires a series.");
        }, 1));

        // copy [value]
        ctx.Set("copy", new Native((args, refs, _, _, _) =>
        {
            if (args[0] is Block b)
            {
                // Create a new block with a shallow copy of the children (from current index)
                return new Block(b.Children.Skip(b.Index));
            }
            if (args[0] is Text t)
            {
                return new Text(t.Content[t.Index..]);
            }
            // For non-series, copy is identity
            return args[0];
        }, 1));
    }
}
