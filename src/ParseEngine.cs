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

    // --- Private value used to inject copy/set callbacks into the rule sequence ---
    private class ParseAction(Action<int> action) : Value
    {
        public Action<int> Action { get; } = action;
        public override string ToString() => "<parse-action>";
    }

    // --- Non-local exit exceptions for loop control ---
    private class ParseBreakException : Exception { }
    private class ParseRejectException : Exception { }
    private class ParseThenException(bool matchResult, int inputIndex) : Exception
    {
        public bool MatchResult { get; } = matchResult;
        public int InputIndex { get; } = inputIndex;
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

    // -----------------------------------------------------------------------
    // MatchBlock: split on | and try each alternative in order.
    // Handles the ParseThenException that commits to the current branch.
    // -----------------------------------------------------------------------
    private bool MatchBlock(Block ruleBlock, ref int inputIndex)
    {
        var alternatives = SplitAlternatives(ruleBlock);

        foreach (var sequence in alternatives)
        {
            int tempIndex = inputIndex;
            try
            {
                if (MatchSequence(sequence, 0, ref tempIndex))
                {
                    inputIndex = tempIndex;
                    return true;
                }
            }
            catch (ParseThenException pte)
            {
                // "then" commits to this branch — stop trying further alternatives
                if (pte.MatchResult) inputIndex = pte.InputIndex;
                return pte.MatchResult;
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

    // -----------------------------------------------------------------------
    // MatchSequence: dispatch on the current rule element type/keyword.
    // -----------------------------------------------------------------------
    private bool MatchSequence(List<Value> sequence, int seqIndex, ref int inputIndex)
    {
        if (seqIndex == sequence.Count)
            return true;

        var current = sequence[seqIndex];

        // ── 1. to / thru ──────────────────────────────────────────────────
        if (current is Word searchWord && (searchWord.Name == "to" || searchWord.Name == "thru"))
        {
            if (seqIndex + 1 >= sequence.Count)
                throw new Exception($"Parse command '{searchWord.Name}' must be followed by a pattern.");

            var pattern = sequence[seqIndex + 1];
            bool isThru = searchWord.Name == "thru";
            return MatchToOrThruBacktracking(pattern, isThru, inputIndex, ref inputIndex, sequence, seqIndex + 2);
        }

        // ── 2. copy / set ─────────────────────────────────────────────────
        if (current is Word extractWord && (extractWord.Name == "copy" || extractWord.Name == "set"))
        {
            if (seqIndex + 1 >= sequence.Count || sequence[seqIndex + 1] is not Word varWord)
                throw new Exception($"Parse command '{extractWord.Name}' must be followed by a word variable name.");

            if (seqIndex + 2 >= sequence.Count)
                throw new Exception($"Parse command '{extractWord.Name}' is missing the rule to match.");

            string varName = varWord.Name;
            string mode = extractWord.Name;

            int consumedCount = 1;
            var ruleElement = sequence[seqIndex + 2];
            if (ruleElement is Word rw && (rw.Name == "any" || rw.Name == "some" || rw.Name == "opt" || rw.Name == "while"))
            {
                consumedCount = 2;
            }
            else if (ruleElement is Integer)
            {
                consumedCount = seqIndex + 3 < sequence.Count && sequence[seqIndex + 3] is Integer ? 3 : 2;
            }

            var ruleSequence = sequence.GetRange(seqIndex + 2, consumedCount);
            var restOfSequence = sequence.GetRange(seqIndex + 2 + consumedCount, sequence.Count - (seqIndex + 2 + consumedCount));

            int start = inputIndex;
            var action = new ParseAction((endIndex) =>
            {
                if (mode == "copy")
                {
                    if (_isBlockMode)
                        _context.Set(varName, new Block(InputBlock.Children.GetRange(start, endIndex - start)));
                    else
                        _context.Set(varName, new Text(InputText.Substring(start, endIndex - start)));
                }
                else if (mode == "set")
                {
                    if (endIndex > start)
                        _context.Set(varName, _isBlockMode ? InputBlock.Children[start] : (Value)new Character(InputText[start]));
                    else
                        _context.Set(varName, new Word("none"));
                }
            });

            var combined = ruleSequence.Concat([action]).Concat(restOfSequence).ToList();
            return MatchSequence(combined, 0, ref inputIndex);
        }

        // ── 3. not / ahead (lookaheads) ───────────────────────────────────
        if (current is Word lookaheadWord && (lookaheadWord.Name == "not" || lookaheadWord.Name == "ahead"))
        {
            if (seqIndex + 1 >= sequence.Count)
                throw new Exception($"Parse keyword '{lookaheadWord.Name}' must be followed by a rule.");

            bool isNot = lookaheadWord.Name == "not";
            var lookaheadRule = sequence[seqIndex + 1];
            int tempIndex = inputIndex;
            bool matched;
            try { matched = MatchElement(lookaheadRule, ref tempIndex); }
            catch { matched = false; }

            bool result = isNot ? !matched : matched;
            // Lookahead never advances input
            if (result)
                return MatchSequence(sequence, seqIndex + 2, ref inputIndex);
            return false;
        }

        // ── 4. if (paren conditional) ─────────────────────────────────────
        if (current is Word ifWord && ifWord.Name == "if")
        {
            if (seqIndex + 1 >= sequence.Count || sequence[seqIndex + 1] is not Paren condParen)
                throw new Exception("Parse keyword 'if' must be followed by a paren expression.");

            var interpreter = new Interpreter();
            var condResult = interpreter.Evaluate(condParen, _context);
            if (!IsTruthy(condResult)) return false;
            return MatchSequence(sequence, seqIndex + 2, ref inputIndex);
        }

        // ── 5. then (commit: skip remaining alternatives on failure) ───────
        if (current is Word thenWord && thenWord.Name == "then")
        {
            int tempIndex = inputIndex;
            bool rest = MatchSequence(sequence, seqIndex + 1, ref tempIndex);
            if (rest)
            {
                inputIndex = tempIndex;
                return true;
            }
            throw new ParseThenException(false, inputIndex);
        }

        // ── 6. into (sub-parse a nested series) ───────────────────────────
        if (current is Word intoWord && intoWord.Name == "into")
        {
            if (seqIndex + 1 >= sequence.Count || sequence[seqIndex + 1] is not Block subRuleBlock)
                throw new Exception("Parse keyword 'into' must be followed by a block rule.");

            if (inputIndex >= InputLength) return false;

            Series? subSeries = null;
            if (_isBlockMode)
            {
                var inputVal = InputBlock.Children[inputIndex];
                if (inputVal is Series s) subSeries = s;
                else return false;
            }
            else
            {
                // In string mode, 'into' is unusual but treat current position substring as a Text
                return false; // not commonly supported in string mode
            }

            var subEngine = new ParseEngine(subSeries, _isCase, _context);
            int subIndex = subSeries.Index;
            int subLen = subSeries is Block sb ? sb.Children.Count : ((Text)subSeries).Content.Length;
            if (subEngine.Match(subRuleBlock, ref subIndex) && subIndex == subLen)
            {
                inputIndex++;
                return MatchSequence(sequence, seqIndex + 2, ref inputIndex);
            }
            return false;
        }

        // ── 7. quote (literal match, bypasses word evaluation) ────────────
        if (current is Word quoteWord && quoteWord.Name == "quote")
        {
            if (seqIndex + 1 >= sequence.Count)
                throw new Exception("Parse keyword 'quote' must be followed by a value.");

            var literal = sequence[seqIndex + 1];
            int tempIndex = inputIndex;
            // Match literal value directly (as if it were a constant, not a variable)
            bool matched = MatchLiteral(literal, ref tempIndex);
            if (matched)
            {
                if (MatchSequence(sequence, seqIndex + 2, ref tempIndex))
                {
                    inputIndex = tempIndex;
                    return true;
                }
            }
            return false;
        }

        // ── 8. insert (mutate: insert value at current position) ──────────
        if (current is Word insertWord && insertWord.Name == "insert")
        {
            if (seqIndex + 1 >= sequence.Count)
                throw new Exception("Parse keyword 'insert' must be followed by a value.");

            var insertVal = sequence[seqIndex + 1];
            // Resolve word references for the value to insert
            Value resolved = insertVal is Word iw ? _context.Get(iw.Name) : insertVal;

            if (_isBlockMode)
            {
                InputBlock.Children.Insert(inputIndex, resolved);
                inputIndex++; // advance past the newly inserted element
            }
            else
            {
                string toInsert = resolved is Text it ? it.Content
                    : resolved is Character ic ? ic.CharValue.ToString()
                    : resolved.ToString();
                var text = (Text)_inputSeries;
                text.Content = text.Content.Substring(0, inputIndex) + toInsert + text.Content.Substring(inputIndex);
                inputIndex += toInsert.Length;
            }
            return MatchSequence(sequence, seqIndex + 2, ref inputIndex);
        }

        // ── 9. remove (mutate: match rule then delete matched portion) ─────
        if (current is Word removeWord && removeWord.Name == "remove")
        {
            if (seqIndex + 1 >= sequence.Count)
                throw new Exception("Parse keyword 'remove' must be followed by a rule.");

            var removeRule = sequence[seqIndex + 1];
            int start = inputIndex;
            int tempIndex = inputIndex;
            bool matched;
            try { matched = MatchElement(removeRule, ref tempIndex); }
            catch { matched = false; }

            if (!matched) return false;

            int end = tempIndex;
            if (_isBlockMode)
            {
                InputBlock.Children.RemoveRange(start, end - start);
                // inputIndex stays at start (now pointing at what was after the removed range)
                inputIndex = start;
            }
            else
            {
                var text = (Text)_inputSeries;
                text.Content = text.Content.Substring(0, start) + text.Content.Substring(end);
                inputIndex = start;
            }
            return MatchSequence(sequence, seqIndex + 2, ref inputIndex);
        }

        // ── 10. change (mutate: match rule then replace matched portion) ───
        if (current is Word changeWord && changeWord.Name == "change")
        {
            if (seqIndex + 2 >= sequence.Count)
                throw new Exception("Parse keyword 'change' must be followed by a rule and a replacement value.");

            var changeRule = sequence[seqIndex + 1];
            var changeVal = sequence[seqIndex + 2];
            Value resolvedVal = changeVal is Word cw ? _context.Get(cw.Name) : changeVal;

            int start = inputIndex;
            int tempIndex = inputIndex;
            bool matched;
            try { matched = MatchElement(changeRule, ref tempIndex); }
            catch { matched = false; }

            if (!matched) return false;

            int end = tempIndex;
            if (_isBlockMode)
            {
                InputBlock.Children.RemoveRange(start, end - start);
                InputBlock.Children.Insert(start, resolvedVal);
                inputIndex = start + 1;
            }
            else
            {
                string replacement = resolvedVal is Text rt ? rt.Content
                    : resolvedVal is Character rc ? rc.CharValue.ToString()
                    : resolvedVal.ToString();
                var text = (Text)_inputSeries;
                text.Content = text.Content.Substring(0, start) + replacement + text.Content.Substring(end);
                inputIndex = start + replacement.Length;
            }
            return MatchSequence(sequence, seqIndex + 3, ref inputIndex);
        }

        // ── 11. fail ──────────────────────────────────────────────────────
        if (current is Word failWord && failWord.Name == "fail")
        {
            return false;
        }

        // ── 12. break ─────────────────────────────────────────────────────
        if (current is Word breakWord && breakWord.Name == "break")
        {
            throw new ParseBreakException();
        }

        // ── 13. reject ────────────────────────────────────────────────────
        if (current is Word rejectWord && rejectWord.Name == "reject")
        {
            throw new ParseRejectException();
        }

        // ── 14. any / some / opt / while ──────────────────────────────────
        if (current is Word loopWord && (loopWord.Name == "any" || loopWord.Name == "some" || loopWord.Name == "opt" || loopWord.Name == "while"))
        {
            if (seqIndex + 1 >= sequence.Count)
                throw new Exception($"Parse rule modifier '{loopWord.Name}' must be followed by a rule.");

            int min = loopWord.Name == "some" ? 1 : 0;
            int max = loopWord.Name == "opt" ? 1 : int.MaxValue;
            bool noProgressGuard = loopWord.Name != "while"; // while has no no-progress guard

            var repeatedRule = sequence[seqIndex + 1];
            return MatchRepetition(repeatedRule, 0, min, max, noProgressGuard, ref inputIndex, sequence, seqIndex + 2);
        }

        // ── 15. Numeric count or range repetition ─────────────────────────
        if (current is Integer countVal &&
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

            return MatchRepetition(repeatedRule, 0, min, max, true, ref inputIndex, sequence, nextSeqIndex);
        }

        // ── 16. Regular rule element ───────────────────────────────────────
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

    // -----------------------------------------------------------------------
    // IsARuleElement: can this value serve as a rule in numeric repetition?
    // -----------------------------------------------------------------------
    private bool IsARuleElement(Value val)
    {
        if (val is Block || val is Character || val is Text || val is Bitset)
            return true;

        if (val is Word w)
        {
            string name = w.Name;
            if (name == "any" || name == "some" || name == "opt" || name == "while" ||
                name == "skip" || name == "end" || name == "none" ||
                name == "to" || name == "thru" || name == "copy" || name == "set" ||
                name == "not" || name == "ahead" || name == "into" || name == "quote" ||
                name == "insert" || name == "remove" || name == "change" ||
                name == "fail" || name == "break" || name == "reject" ||
                name == "integer!" || name == "string!" || name == "text!" || name == "char!" ||
                name == "word!" || name == "block!" || name == "logic!")
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

    // -----------------------------------------------------------------------
    // MatchRepetition: greedy repetition with backtracking.
    // noProgressGuard = true means stop if the rule consumed nothing (any/some).
    // noProgressGuard = false means allow zero-progress (while).
    // Handles ParseBreakException (exit with success) and
    // ParseRejectException (exit with failure).
    // -----------------------------------------------------------------------
    private bool MatchRepetition(Value rule, int count, int min, int max, bool noProgressGuard, ref int inputIndex, List<Value> sequence, int nextSeqIndex)
    {
        if (count < max)
        {
            int tempIndex = inputIndex;
            bool elementMatched;
            try
            {
                elementMatched = MatchElement(rule, ref tempIndex);
            }
            catch (ParseBreakException)
            {
                // break: exit loop immediately with success (don't need to meet min)
                int successIndex = inputIndex;
                if (MatchSequence(sequence, nextSeqIndex, ref successIndex))
                {
                    inputIndex = successIndex;
                    return true;
                }
                return false;
            }
            catch (ParseRejectException)
            {
                // reject: exit loop immediately with failure
                return false;
            }

            bool madeProgress = tempIndex > inputIndex;
            if (elementMatched && (!noProgressGuard || madeProgress))
            {
                if (MatchRepetition(rule, count + 1, min, max, noProgressGuard, ref tempIndex, sequence, nextSeqIndex))
                {
                    inputIndex = tempIndex;
                    return true;
                }
            }
        }

        // Check minimum requirements, then match rest of sequence
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

    // -----------------------------------------------------------------------
    // MatchToOrThruBacktracking
    // -----------------------------------------------------------------------
    private bool MatchToOrThruBacktracking(Value pattern, bool isThru, int scanStart, ref int inputIndex, List<Value> sequence, int nextSeqIndex)
    {
        int length = InputLength;
        for (int i = scanStart; i <= length; i++)
        {
            int tempIndex = i;
            bool matched = false;

            if (pattern is Word w && w.Name == "end")
                matched = (i == length);
            else
                matched = MatchElement(pattern, ref tempIndex);

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

    // -----------------------------------------------------------------------
    // ValuesEqual: structural equality for block-mode matching
    // -----------------------------------------------------------------------
    private bool ValuesEqual(Value v1, Value v2)
    {
        if (v1 == null || v2 == null) return false;
        if (ReferenceEquals(v1, v2)) return true;

        if (v1 is Integer i1 && v2 is Integer i2) return i1.Number == i2.Number;
        if (v1 is Decimal d1 && v2 is Decimal d2) return d1.Number == d2.Number;

        if (v1 is Character c1 && v2 is Character c2)
            return _isCase ? (c1.CharValue == c2.CharValue)
                           : (char.ToLowerInvariant(c1.CharValue) == char.ToLowerInvariant(c2.CharValue));

        if (v1 is Text t1 && v2 is Text t2)
            return string.Equals(t1.Content, t2.Content, _isCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);

        if (v1 is Word w1 && v2 is Word w2)
            return string.Equals(w1.Name, w2.Name, StringComparison.OrdinalIgnoreCase);

        if (v1 is LitWord lw1 && v2 is LitWord lw2)
            return string.Equals(lw1.Name, lw2.Name, StringComparison.OrdinalIgnoreCase);

        if (v1 is LitWord lw && v2 is Word w) return string.Equals(lw.Name, w.Name, StringComparison.OrdinalIgnoreCase);
        if (v1 is Word w_ && v2 is LitWord lw_) return string.Equals(w_.Name, lw_.Name, StringComparison.OrdinalIgnoreCase);

        return string.Equals(v1.ToString(), v2.ToString(), _isCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // IsTruthy: used by the 'if' keyword
    // -----------------------------------------------------------------------
    private static bool IsTruthy(Value v)
    {
        if (v is Logic lg) return lg.Condition;
        if (v is Word w && w.Name == "none") return false;
        return true; // any other value is truthy
    }

    // -----------------------------------------------------------------------
    // MatchLiteral: used by 'quote' — matches value directly without resolving
    // words as rule references.
    // -----------------------------------------------------------------------
    private bool MatchLiteral(Value literal, ref int inputIndex)
    {
        if (_isBlockMode)
        {
            if (inputIndex >= InputLength) return false;
            var inputVal = InputBlock.Children[inputIndex];
            if (ValuesEqual(literal, inputVal))
            {
                inputIndex++;
                return true;
            }
            return false;
        }
        else
        {
            // In string mode, quote is equivalent to a character or string literal
            return MatchElement(literal, ref inputIndex);
        }
    }

    // -----------------------------------------------------------------------
    // MatchElement: match a single rule element against input.
    // -----------------------------------------------------------------------
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
                switch (wordRule.Name)
                {
                    case "none": return true;
                    case "skip": inputIndex++; return true;
                    case "end":  return inputIndex == InputLength;
                    case "fail": return false;
                    case "break": throw new ParseBreakException();
                    case "reject": throw new ParseRejectException();
                }

                string name = wordRule.Name;
                bool isDatatype = name == "integer!" || name == "string!" || name == "text!" ||
                                  name == "char!" || name == "word!" || name == "block!" || name == "logic!";
                if (isDatatype)
                {
                    bool typeMatched = name switch
                    {
                        "integer!" => inputValue is Integer,
                        "string!"  => inputValue is Text,
                        "text!"    => inputValue is Text,
                        "char!"    => inputValue is Character,
                        "word!"    => inputValue is Word || inputValue is LitWord || inputValue is SetWord || inputValue is GetWord,
                        "block!"   => inputValue is Block,
                        "logic!"   => inputValue is Logic,
                        _          => false
                    };
                    if (typeMatched) { inputIndex++; return true; }
                    return false;
                }

                try
                {
                    Value resolved = _context.Get(wordRule.Name);
                    return MatchElement(resolved, ref inputIndex);
                }
                catch (ParseBreakException) { throw; }
                catch (ParseRejectException) { throw; }
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
                return MatchBlock(blockRule, ref inputIndex);
            }

            if (element is Bitset bitsetRule)
            {
                if (inputValue is Character ch)
                {
                    bool match = _isCase
                        ? bitsetRule.Contains(ch.CharValue)
                        : bitsetRule.Contains(char.ToLowerInvariant(ch.CharValue)) || bitsetRule.Contains(char.ToUpperInvariant(ch.CharValue));
                    if (match) { inputIndex++; return true; }
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
                    if (val is Series s) { inputIndex = s.Index; return true; }
                    throw new Exception($":{gw.Name} is not a series.");
                }
                catch (Exception ex) when (ex.Message.Contains("no value"))
                {
                    throw new Exception($"Undefined word ':{gw.Name}' in parse rules.");
                }
            }

            if (ValuesEqual(element, inputValue)) { inputIndex++; return true; }
            return false;
        }
        else
        {
            // ── String / Text mode ──

            if (element is Character targetChar)
            {
                if (inputIndex >= InputLength) return false;
                char inputChar = InputText[inputIndex];
                bool match = _isCase ? (inputChar == targetChar.CharValue)
                                     : (char.ToLowerInvariant(inputChar) == char.ToLowerInvariant(targetChar.CharValue));
                if (match) { inputIndex++; return true; }
                return false;
            }

            if (element is Bitset bitset)
            {
                if (inputIndex >= InputLength) return false;
                char inputChar = InputText[inputIndex];
                bool match = _isCase ? bitset.Contains(inputChar)
                                     : bitset.Contains(char.ToLowerInvariant(inputChar)) || bitset.Contains(char.ToUpperInvariant(inputChar));
                if (match) { inputIndex++; return true; }
                return false;
            }

            if (element is Text targetStr)
            {
                string strVal = targetStr.Content;
                if (inputIndex + strVal.Length > InputLength) return false;
                string inputSub = InputText.Substring(inputIndex, strVal.Length);
                bool match = _isCase ? (inputSub == strVal)
                                     : string.Equals(inputSub, strVal, StringComparison.OrdinalIgnoreCase);
                if (match) { inputIndex += strVal.Length; return true; }
                return false;
            }

            if (element is Block blockRule)
            {
                if (blockRule is Paren p)
                {
                    var interpreter = new Interpreter();
                    interpreter.Evaluate(p, _context);
                    return true;
                }
                return MatchBlock(blockRule, ref inputIndex);
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
                    if (val is Series s) { inputIndex = s.Index; return true; }
                    throw new Exception($":{gw.Name} is not a series.");
                }
                catch (Exception ex) when (ex.Message.Contains("no value"))
                {
                    throw new Exception($"Undefined word ':{gw.Name}' in parse rules.");
                }
            }

            if (element is Word w)
            {
                switch (w.Name)
                {
                    case "none":   return true;
                    case "skip":   if (inputIndex < InputLength) { inputIndex++; return true; } return false;
                    case "end":    return inputIndex == InputLength;
                    case "fail":   return false;
                    case "break":  throw new ParseBreakException();
                    case "reject": throw new ParseRejectException();
                }

                try
                {
                    Value resolved = _context.Get(w.Name);
                    return MatchElement(resolved, ref inputIndex);
                }
                catch (ParseBreakException) { throw; }
                catch (ParseRejectException) { throw; }
                catch
                {
                    throw new Exception($"Undefined word '{w.Name}' in parse rules.");
                }
            }

            throw new Exception($"Unsupported rule type '{element.GetType().Name}' in parse rules.");
        }
    }
}
