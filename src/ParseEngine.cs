using System;
using System.Collections.Generic;
using System.Linq;

namespace Ragnar;

public class ParseEngine
{
    private readonly string _input;
    private readonly bool _isCase;
    private readonly Context _context;

    private class ParseAction(Action<int> action) : Value
    {
        public Action<int> Action { get; } = action;
        public override string ToString() => "<parse-action>";
    }

    public ParseEngine(string input, bool isCase, Context context)
    {
        _input = input;
        _isCase = isCase;
        _context = context;
    }

    public bool Match(Block rules, ref int index)
    {
        return MatchBlock(rules, ref index);
    }

    private bool MatchBlock(Block ruleBlock, ref int inputIndex)
    {
        var alternatives = SplitAlternatives(ruleBlock);

        foreach (var sequence in alternatives)
        {
            int tempIndex = inputIndex;
            if (MatchSequence(sequence, 0, ref tempIndex))
            {
                inputIndex = tempIndex;
                return true;
            }
        }

        return false;
    }

    private List<List<Value>> SplitAlternatives(Block block)
    {
        var list = new List<List<Value>>();
        var current = new List<Value>();
        foreach (var child in block.Children)
        {
            if (child is Word w && w.Name == "|")
            {
                list.Add(current);
                current = new List<Value>();
            }
            else
            {
                current.Add(child);
            }
        }
        list.Add(current);
        return list;
    }

    private bool MatchSequence(List<Value> sequence, int seqIndex, ref int inputIndex)
    {
        if (seqIndex == sequence.Count)
        {
            return true;
        }

        var current = sequence[seqIndex];

        // 1. Check for to/thru search commands
        if (current is Word searchWord && (searchWord.Name == "to" || searchWord.Name == "thru"))
        {
            if (seqIndex + 1 >= sequence.Count)
                throw new Exception($"Parse command '{searchWord.Name}' must be followed by a pattern.");

            var pattern = sequence[seqIndex + 1];
            bool isThru = searchWord.Name == "thru";
            return MatchToOrThruBacktracking(pattern, isThru, inputIndex, ref inputIndex, sequence, seqIndex + 2);
        }
        // 2. Check for copy/set assignment commands
        else if (current is Word extractWord && (extractWord.Name == "copy" || extractWord.Name == "set"))
        {
            if (seqIndex + 1 >= sequence.Count || sequence[seqIndex + 1] is not Word varWord)
                throw new Exception($"Parse command '{extractWord.Name}' must be followed by a word variable name.");

            if (seqIndex + 2 >= sequence.Count)
                throw new Exception($"Parse command '{extractWord.Name}' is missing the rule to match.");

            string varName = varWord.Name;
            string mode = extractWord.Name;

            int consumedCount = 1;
            var ruleElement = sequence[seqIndex + 2];
            if (ruleElement is Word rw && (rw.Name == "any" || rw.Name == "some" || rw.Name == "opt"))
            {
                consumedCount = 2;
            }
            else if (ruleElement is Integer)
            {
                if (seqIndex + 3 < sequence.Count && sequence[seqIndex + 3] is Integer)
                {
                    consumedCount = 3;
                }
                else
                {
                    consumedCount = 2;
                }
            }

            var ruleSequence = sequence.GetRange(seqIndex + 2, consumedCount);
            var restOfSequence = sequence.GetRange(seqIndex + 2 + consumedCount, sequence.Count - (seqIndex + 2 + consumedCount));

            int start = inputIndex;
            var action = new ParseAction((endIndex) =>
            {
                if (mode == "copy")
                {
                    string val = _input.Substring(start, endIndex - start);
                    _context.Set(varName, new Text(val));
                }
                else if (mode == "set")
                {
                    if (endIndex > start)
                        _context.Set(varName, new Character(_input[start]));
                    else
                        _context.Set(varName, new Word("none"));
                }
            });

            var combined = ruleSequence.Concat([action]).Concat(restOfSequence).ToList();
            return MatchSequence(combined, 0, ref inputIndex);
        }
        // 3. Check for modifiers (any, some, opt)
        else if (current is Word w && (w.Name == "any" || w.Name == "some" || w.Name == "opt"))
        {
            if (seqIndex + 1 >= sequence.Count)
                throw new Exception($"Parse rule modifier '{w.Name}' must be followed by a rule.");

            int min = w.Name switch
            {
                "any" => 0,
                "some" => 1,
                "opt" => 0,
                _ => 0
            };
            int max = w.Name switch
            {
                "any" => int.MaxValue,
                "some" => int.MaxValue,
                "opt" => 1,
                _ => 1
            };

            var repeatedRule = sequence[seqIndex + 1];
            return MatchRepetition(repeatedRule, 0, min, max, ref inputIndex, sequence, seqIndex + 2);
        }
        // 4. Check for numeric count or range repetition
        else if (current is Integer countVal)
        {
            int min = (int)countVal.Number;
            int max = min;
            int nextSeqIndex = seqIndex + 2;
            Value repeatedRule;

            if (seqIndex + 1 >= sequence.Count)
                throw new Exception("Parse rule count must be followed by a rule or another count.");

            if (sequence[seqIndex + 1] is Integer maxVal)
            {
                max = (int)maxVal.Number;
                if (seqIndex + 2 >= sequence.Count)
                    throw new Exception("Parse rule range must be followed by a rule.");
                repeatedRule = sequence[seqIndex + 2];
                nextSeqIndex = seqIndex + 3;
            }
            else
            {
                repeatedRule = sequence[seqIndex + 1];
            }

            return MatchRepetition(repeatedRule, 0, min, max, ref inputIndex, sequence, nextSeqIndex);
        }
        // 5. Regular rule element
        else
        {
            int tempIndex = inputIndex;
            if (MatchElement(current, ref tempIndex))
            {
                if (MatchSequence(sequence, seqIndex + 1, ref tempIndex))
                {
                    inputIndex = tempIndex;
                    return true;
                }
            }
            return false;
        }
    }

    private bool MatchRepetition(Value rule, int count, int min, int max, ref int inputIndex, List<Value> sequence, int nextSeqIndex)
    {
        // Try greedily matching the rule
        if (count < max)
        {
            int tempIndex = inputIndex;
            if (MatchElement(rule, ref tempIndex) && tempIndex > inputIndex)
            {
                if (MatchRepetition(rule, count + 1, min, max, ref tempIndex, sequence, nextSeqIndex))
                {
                    inputIndex = tempIndex;
                    return true;
                }
            }
        }

        // Check if we met the minimum requirements, then match the rest of the sequence
        if (count >= min)
        {
            int tempIndex = inputIndex;
            if (MatchSequence(sequence, nextSeqIndex, ref tempIndex))
            {
                inputIndex = tempIndex;
                return true;
            }
        }

        return false;
    }

    private bool MatchToOrThruBacktracking(Value pattern, bool isThru, int scanStart, ref int inputIndex, List<Value> sequence, int nextSeqIndex)
    {
        for (int i = scanStart; i <= _input.Length; i++)
        {
            int tempIndex = i;
            bool matched = false;

            if (pattern is Word w && w.Name == "end")
            {
                matched = (i == _input.Length);
            }
            else
            {
                matched = MatchElement(pattern, ref tempIndex);
            }

            if (matched)
            {
                int matchedPos = isThru ? tempIndex : i;
                int restIndex = matchedPos;
                if (MatchSequence(sequence, nextSeqIndex, ref restIndex))
                {
                    inputIndex = restIndex;
                    return true;
                }
            }
        }
        return false;
    }

    private bool MatchElement(Value element, ref int inputIndex)
    {
        if (element is ParseAction parseAction)
        {
            parseAction.Action(inputIndex);
            return true;
        }
        else if (element is Character targetChar)
        {
            if (inputIndex >= _input.Length) return false;
            char inputChar = _input[inputIndex];
            bool match = _isCase
                ? (inputChar == targetChar.CharValue)
                : (char.ToLowerInvariant(inputChar) == char.ToLowerInvariant(targetChar.CharValue));

            if (match)
            {
                inputIndex++;
                return true;
            }
            return false;
        }
        else if (element is Text targetStr)
        {
            string strVal = targetStr.Content;
            if (inputIndex + strVal.Length > _input.Length) return false;
            string inputSub = _input.Substring(inputIndex, strVal.Length);
            bool match = _isCase
                ? (inputSub == strVal)
                : string.Equals(inputSub, strVal, StringComparison.OrdinalIgnoreCase);

            if (match)
            {
                inputIndex += strVal.Length;
                return true;
            }
            return false;
        }
        else if (element is Block blockRule)
        {
            if (blockRule is Paren p)
            {
                var interpreter = new Interpreter();
                interpreter.Evaluate(p, _context);
                return true;
            }
            else
            {
                return MatchBlock(blockRule, ref inputIndex);
            }
        }
        else if (element is SetWord sw)
        {
            _context.Set(sw.Name, new Text(_input, inputIndex));
            return true;
        }
        else if (element is GetWord gw)
        {
            try
            {
                Value val = _context.Get(gw.Name);
                if (val is Series s)
                {
                    inputIndex = s.Index;
                    return true;
                }
                throw new Exception($":{gw.Name} is not a series.");
            }
            catch
            {
                throw new Exception($"Undefined word ':{gw.Name}' in parse rules.");
            }
        }
        else if (element is Word w)
        {
            if (w.Name == "none")
            {
                return true;
            }
            if (w.Name == "skip")
            {
                if (inputIndex < _input.Length)
                {
                    inputIndex++;
                    return true;
                }
                return false;
            }
            if (w.Name == "end")
            {
                return inputIndex == _input.Length;
            }

            // Word lookup in context to resolve sub-rule
            try
            {
                Value resolved = _context.Get(w.Name);
                return MatchElement(resolved, ref inputIndex);
            }
            catch
            {
                throw new Exception($"Undefined word '{w.Name}' in parse rules.");
            }
        }

        throw new Exception($"Unsupported rule type '{element.GetType().Name}' in this iteration.");
    }
}
