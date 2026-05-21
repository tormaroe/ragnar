using System;
using System.Collections.Generic;
using System.Linq;

namespace Ragnar;

public class ParseEngine
{
    private readonly Series _inputSeries;
    private readonly bool _isBlockMode;
    private readonly bool _isCase;
    private readonly Context _context;

    private class ParseAction(Action<int> action) : Value
    {
        public Action<int> Action { get; } = action;
        public override string ToString() => "<parse-action>";
    }

    public ParseEngine(Series inputSeries, bool isCase, Context context)
    {
        _inputSeries = inputSeries;
        _isBlockMode = inputSeries is Block;
        _isCase = isCase;
        _context = context;
    }

    private string InputText => ((Text)_inputSeries).Content;
    private Block InputBlock => (Block)_inputSeries;
    private int InputLength => _isBlockMode ? InputBlock.Children.Count : InputText.Length;

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
                    if (_isBlockMode)
                    {
                        var subChildren = InputBlock.Children.GetRange(start, endIndex - start);
                        _context.Set(varName, new Block(subChildren));
                    }
                    else
                    {
                        string val = InputText.Substring(start, endIndex - start);
                        _context.Set(varName, new Text(val));
                    }
                }
                else if (mode == "set")
                {
                    if (endIndex > start)
                    {
                        if (_isBlockMode)
                        {
                            _context.Set(varName, InputBlock.Children[start]);
                        }
                        else
                        {
                            _context.Set(varName, new Character(InputText[start]));
                        }
                    }
                    else
                    {
                        _context.Set(varName, new Word("none"));
                    }
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
        else if (current is Integer countVal && 
                 ((seqIndex + 1 < sequence.Count && IsARuleElement(sequence[seqIndex + 1])) ||
                  (seqIndex + 1 < sequence.Count && sequence[seqIndex + 1] is Integer && seqIndex + 2 < sequence.Count && IsARuleElement(sequence[seqIndex + 2]))))
        {
            int min = (int)countVal.Number;
            int max = min;
            int nextSeqIndex = seqIndex + 2;
            Value repeatedRule;

            if (sequence[seqIndex + 1] is Integer maxVal && seqIndex + 2 < sequence.Count && IsARuleElement(sequence[seqIndex + 2]))
            {
                max = (int)maxVal.Number;
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

    private bool IsARuleElement(Value val)
    {
        if (val is Block || val is Character || val is Text || val is Bitset)
            return true;

        if (val is Word w)
        {
            string name = w.Name;
            if (name == "any" || name == "some" || name == "opt" || name == "skip" || name == "end" || name == "none" ||
                name == "to" || name == "thru" || name == "copy" || name == "set" ||
                name == "integer!" || name == "string!" || name == "text!" || name == "char!" || name == "word!" || name == "block!" || name == "logic!")
            {
                return true;
            }

            try
            {
                var resolved = _context.Get(name);
                return resolved is Block || resolved is Character || resolved is Text || resolved is Bitset;
            }
            catch
            {
                return false;
            }
        }

        return false;
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
        int length = InputLength;
        for (int i = scanStart; i <= length; i++)
        {
            int tempIndex = i;
            bool matched = false;

            if (pattern is Word w && w.Name == "end")
            {
                matched = (i == length);
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

    private bool ValuesEqual(Value v1, Value v2)
    {
        if (v1 == null || v2 == null) return false;
        if (ReferenceEquals(v1, v2)) return true;

        if (v1 is Integer i1 && v2 is Integer i2)
            return i1.Number == i2.Number;

        if (v1 is Decimal d1 && v2 is Decimal d2)
            return d1.Number == d2.Number;

        if (v1 is Character c1 && v2 is Character c2)
            return _isCase
                ? (c1.CharValue == c2.CharValue)
                : (char.ToLowerInvariant(c1.CharValue) == char.ToLowerInvariant(c2.CharValue));

        if (v1 is Text t1 && v2 is Text t2)
            return string.Equals(t1.Content, t2.Content, _isCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);

        if (v1 is Word w1 && v2 is Word w2)
            return string.Equals(w1.Name, w2.Name, StringComparison.OrdinalIgnoreCase);

        if (v1 is LitWord lw1 && v2 is LitWord lw2)
            return string.Equals(lw1.Name, lw2.Name, StringComparison.OrdinalIgnoreCase);

        if (v1 is LitWord lw && v2 is Word w)
            return string.Equals(lw.Name, w.Name, StringComparison.OrdinalIgnoreCase);
        if (v1 is Word w_ && v2 is LitWord lw_)
            return string.Equals(w_.Name, lw_.Name, StringComparison.OrdinalIgnoreCase);

        return string.Equals(v1.ToString(), v2.ToString(), _isCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
    }

    private bool MatchElement(Value element, ref int inputIndex)
    {
        if (element is ParseAction parseAction)
        {
            parseAction.Action(inputIndex);
            return true;
        }

        if (_isBlockMode)
        {
            if (inputIndex >= InputLength) return false;
            Value inputValue = InputBlock.Children[inputIndex];

            if (element is LitWord lw)
            {
                if (inputValue is Word w && string.Equals(w.Name, lw.Name, StringComparison.OrdinalIgnoreCase))
                {
                    inputIndex++;
                    return true;
                }
                return false;
            }

            if (element is Word wordRule)
            {
                if (wordRule.Name == "none")
                {
                    return true;
                }
                if (wordRule.Name == "skip")
                {
                    inputIndex++;
                    return true;
                }
                if (wordRule.Name == "end")
                {
                    return inputIndex == InputLength;
                }

                string name = wordRule.Name;
                bool isDatatype = name == "integer!" || name == "string!" || name == "text!" || name == "char!" || name == "word!" || name == "block!" || name == "logic!";
                if (isDatatype)
                {
                    bool typeMatched = name switch
                    {
                        "integer!" => inputValue is Integer,
                        "string!" => inputValue is Text,
                        "text!" => inputValue is Text,
                        "char!" => inputValue is Character,
                        "word!" => inputValue is Word || inputValue is LitWord || inputValue is SetWord || inputValue is GetWord,
                        "block!" => inputValue is Block,
                        "logic!" => inputValue is Logic,
                        _ => false
                    };

                    if (typeMatched)
                    {
                        inputIndex++;
                        return true;
                    }
                    return false;
                }

                try
                {
                    Value resolved = _context.Get(wordRule.Name);
                    return MatchElement(resolved, ref inputIndex);
                }
                catch
                {
                    throw new Exception($"Undefined word '{wordRule.Name}' in parse rules.");
                }
            }

            if (element is Block blockRule)
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

            if (element is Bitset bitsetRule)
            {
                if (inputValue is Character ch)
                {
                    bool match = _isCase
                        ? bitsetRule.Contains(ch.CharValue)
                        : bitsetRule.Contains(char.ToLowerInvariant(ch.CharValue)) || bitsetRule.Contains(char.ToUpperInvariant(ch.CharValue));
                    if (match)
                    {
                        inputIndex++;
                        return true;
                    }
                }
                return false;
            }

            if (element is SetWord sw)
            {
                _context.Set(sw.Name, _inputSeries.At(inputIndex));
                return true;
            }
            if (element is GetWord gw)
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

            if (ValuesEqual(element, inputValue))
            {
                inputIndex++;
                return true;
            }

            return false;
        }
        else
        {
            if (element is Character targetChar)
            {
                if (inputIndex >= InputLength) return false;
                char inputChar = InputText[inputIndex];
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
            else if (element is Bitset bitset)
            {
                if (inputIndex >= InputLength) return false;
                char inputChar = InputText[inputIndex];
                bool match = _isCase
                    ? bitset.Contains(inputChar)
                    : bitset.Contains(char.ToLowerInvariant(inputChar)) || bitset.Contains(char.ToUpperInvariant(inputChar));
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
                if (inputIndex + strVal.Length > InputLength) return false;
                string inputSub = InputText.Substring(inputIndex, strVal.Length);
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
                _context.Set(sw.Name, _inputSeries.At(inputIndex));
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
                    if (inputIndex < InputLength)
                    {
                        inputIndex++;
                        return true;
                    }
                    return false;
                }
                if (w.Name == "end")
                {
                    return inputIndex == InputLength;
                }

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
}
