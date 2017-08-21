using System.Collections.Generic;

namespace FailAnalyzer
{
    public class Player
    {
        public Player(string name)
        {
            Name = name;
            Fails = new Dictionary<string, int>();
        }

        public string Name { get; }

        public Dictionary<string, int> Fails { get; set; }
    }
}
