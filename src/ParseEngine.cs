using System;
using System.Collections.Generic;
using System.Linq;

namespace Ragnar;

public class ParseEngine
{
    private readonly string _input;
    private readonly bool _isCase;
    private readonly Context _context;

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

        // 1. Check for modifiers (any, some, opt)
        if (current is Word w && (w.Name == "any" || w.Name == "some" || w.Name == "opt"))
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
        // 2. Check for numeric count or range repetition
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
        // 3. Regular rule element
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

    private bool MatchElement(Value element, ref int inputIndex)
    {
        if (element is Character targetChar)
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
            return MatchBlock(blockRule, ref inputIndex);
        }
        else if (element is Word w)
        {
            if (w.Name == "none")
            {
                return true;
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
