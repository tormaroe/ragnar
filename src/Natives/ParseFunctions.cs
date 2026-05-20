using System;
using System.Collections.Generic;
using System.Linq;

namespace Ragnar.Natives;

public static class ParseFunctions
{
    public static void Add(Context ctx)
    {
        ctx.Set("parse", new Native((args, refinements, _, _, _) =>
        {
            if (args.Count < 2)
                throw new Exception("parse requires two arguments: series and rules.");
                
            var series = args[0];
            var rules = args[1];
            
            if (series is not Text t)
                throw new Exception("parse first argument must be a string (Text) in this iteration.");
                
            string input = t.Content;
            bool isAll = refinements.Contains("all");
            bool isCase = refinements.Contains("case");
            
            if (input.Length == 0)
            {
                return new Block();
            }
            
            var delimiterChars = new HashSet<char>();
            
            if (rules is Word w && w.Name == "none")
            {
                if (!isAll)
                {
                    char[] defaultDelims = [' ', '\t', '\n', '\r', ',', ';'];
                    foreach (var c in defaultDelims)
                    {
                        delimiterChars.Add(c);
                    }
                }
            }
            else if (rules is Text delimText)
            {
                string delimStr = delimText.Content;
                foreach (char c in delimStr)
                {
                    if (isCase)
                    {
                        delimiterChars.Add(c);
                    }
                    else
                    {
                        delimiterChars.Add(char.ToLowerInvariant(c));
                        delimiterChars.Add(char.ToUpperInvariant(c));
                    }
                }
                
                if (!isAll)
                {
                    char[] defaultWhitespaces = [' ', '\t', '\n', '\r'];
                    foreach (var c in defaultWhitespaces)
                    {
                        delimiterChars.Add(c);
                    }
                }
            }
            else
            {
                throw new Exception("parse rules must be none or a string in this iteration.");
            }
            
            if (delimiterChars.Count == 0)
            {
                return new Block([new Text(input)]);
            }
            
            var resultList = new List<Value>();
            
            if (isAll)
            {
                int lastIndex = 0;
                for (int i = 0; i < input.Length; i++)
                {
                    if (delimiterChars.Contains(input[i]))
                    {
                        resultList.Add(new Text(input[lastIndex..i]));
                        lastIndex = i + 1;
                    }
                }
                resultList.Add(new Text(input[lastIndex..]));
            }
            else
            {
                int lastIndex = 0;
                bool inDelimiter = true;
                
                for (int i = 0; i < input.Length; i++)
                {
                    bool isDelim = delimiterChars.Contains(input[i]);
                    if (isDelim)
                    {
                        if (!inDelimiter)
                        {
                            resultList.Add(new Text(input[lastIndex..i]));
                            inDelimiter = true;
                        }
                    }
                    else
                    {
                        if (inDelimiter)
                        {
                            lastIndex = i;
                            inDelimiter = false;
                        }
                    }
                }
                
                if (!inDelimiter)
                {
                    resultList.Add(new Text(input[lastIndex..]));
                }
            }
            
            return new Block(resultList);
        }, 2).WithTitle("Parses a series using rules."));
    }
}
