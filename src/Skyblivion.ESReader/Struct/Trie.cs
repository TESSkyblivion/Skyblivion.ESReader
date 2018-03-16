using System.Collections.Generic;

namespace Skyblivion.ESReader.Struct
{
    /*
     * Class Trie
     * Based off implementation of https://github.com/fran6co/phptrie/
     */
    public class Trie
    {
        private Dictionary<string, Trie> trie = new Dictionary<string, Trie>();
        private object value = null;
        /*
        * Trie constructor
         *
         *  This is for internal use
        */
        public Trie(object value = null)
        {
            this.value = value;
        }

        /*
        * Add value to the trie
         *
         *  string The key
         *  mixed The value
         *  Overwrite existing value
        */
        public void add(string str, object value, bool overWrite = true)
        {
            if (str == "")
            {
                if (this.value == null || overWrite) { this.value = value; }
                return;
            }
            foreach (var kvp in this.trie)
            {
                var prefix = kvp.Key;
                var trie = kvp.Value;
                int prefixLength = prefix.Length;
                string head = str.Substring(0, prefixLength);
                int headLength = head.Length;
                bool equals = true;
                string equalPrefix = "";
                for (int i = 0; i < prefixLength; ++i)
                { //Split
                    if (i >= headLength)
                    {
                        Trie equalTrie = new Trie(value);
                        this.trie[equalPrefix] = equalTrie;
                        equalTrie.trie[prefix.Substring(i)] = trie;
                        this.trie.Remove(prefix);
                        return;
                    }
                    else if (prefix[i] != head[i])
                    {
                        if (i > 0)
                        {
                            Trie equalTrie = new Trie();
                            this.trie[equalPrefix] = equalTrie;
                            equalTrie.trie[prefix.Substring(i)] = trie;
                            equalTrie.trie[str.Substring(i)] = new Trie(value);
                            this.trie.Remove(prefix);
                            return;
                        }

                        equals = false;
                        break;
                    }

                    equalPrefix += head[i];
                }

                if (equals)
                {
                    trie.add(str.Substring(prefixLength), value, overWrite);
                    return;
                }
            }
            this.trie[str] = new Trie(value);
        }

        /*
        * Search the Trie with a string
        */
        public object search(string str)
        {
            if (str == "")
            {
                return this.value;
            }
            foreach (var kvp in this.trie)
            {
                var prefix = kvp.Key;
                var trie = kvp.Value;
                int prefixLength = prefix.Length;
                string head = str.Substring(0, prefixLength);
                if (head == prefix)
                {
                    return trie.search(str.Substring(prefixLength));
                }
            }
            return null;
        }

        public TrieIterator searchPrefix(string str)
        {
            if (str == "") { return new TrieIterator(this); }
            int stringLength = str.Length;
            foreach (var kvp in this.trie)
            {
                var prefix = kvp.Key;
                var trie = kvp.Value;
                int prefixLength = prefix.Length;
                string headPrefix, stringPrefix;
                if (prefixLength > stringLength)
                {
                    headPrefix = prefix.Substring(0, stringLength);
                    stringPrefix = str;
                }
                else
                {
                    headPrefix = prefix;
                    stringPrefix = str.Substring(0, prefixLength);
                }

                if (headPrefix == stringPrefix)
                {
                    return trie.searchPrefix(str.Substring(prefixLength));
                }
            }
            return new TrieIterator(null);
        }

        public object _value()
        {
            return this.value;
        }

        public Dictionary<string, Trie> subnodes()
        {
            return this.trie;
        }
    }
}