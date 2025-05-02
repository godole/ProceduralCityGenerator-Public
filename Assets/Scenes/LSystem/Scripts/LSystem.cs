using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LSystem
{
    public class Rule
    {
        public float weight;
        public string previous;
        public string next;
    }

    private Rule r1;
    private Rule r2;

    private Dictionary<string, List<Rule>> _rules = new Dictionary<string, List<Rule>>();
    private Dictionary<string, float> _weights = new Dictionary<string, float>();

    public string Parse(int step, string startString, List<Rule> rules)
    {
        string result = string.Empty;

        for (int s = 0; s < step; s++)
        {
            result = string.Empty;
            for (int index = 0; index < startString.Length; index++)
            {
                foreach (var rule in rules)
                {
                    if (rule.previous.Equals(startString[index].ToString()))
                    {
                        result += rule.next;
                    }
                }
            }

            startString = result;
            
        }
        

        return result;
    }

    public void AddRule(Rule rule)
    {
        if (_rules.ContainsKey(rule.previous))
        {
            _rules.Add(rule.previous, new List<Rule>());
        }
    }
}
