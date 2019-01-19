using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace GitHubSync
{
    public abstract class MapperBase : IEnumerable<KeyValuePair<Parts, IEnumerable<Parts>>>
    {
        Dictionary<Parts, List<Parts>> dic = new Dictionary<Parts, List<Parts>>();

        protected void Add_Internal(Parts from, Parts to)
        {
            if (from.Type != to.Type)
            {
                throw new ArgumentException($"Cannot map [{from.Type}: {@from.Url}] to [{to.Type}: {to.Url}]. ");
            }

            if (!dic.TryGetValue(from, out var parts))
            {
                parts = new List<Parts>();
                dic.Add(from, parts);
            }

            parts.Add(to);
        }

        public IEnumerator<KeyValuePair<Parts, IEnumerable<Parts>>> GetEnumerator()
        {
            return dic
                .Select(e => new KeyValuePair<Parts, IEnumerable<Parts>>(e.Key, e.Value))
                .GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerable<Parts> this[Parts from]
        {
            get
            {
                if (dic.TryGetValue(@from, out var l))
                {
                    return l;
                }
                return Enumerable.Empty<Parts>();
            }
        }

        public IEnumerable<Parts> this[Uri from]
        {
            get
            {
                foreach (var kvp in dic)
                {
                    if (kvp.Key.Url != from.ToString())
                    {
                        continue;
                    }

                    return kvp.Value;
                }

                return Enumerable.Empty<Parts>();
            }
        }

        public IDictionary<string, IList<Tuple<Parts, Parts>>> Transpose()
        {
            var d = new Dictionary<string, IList<Tuple<Parts, Parts>>>();

            foreach (var kvp in dic)
            {
                var source = kvp.Key;

                foreach (var destination in kvp.Value)
                {
                    var orb = $"{destination.Owner}/{destination.Repository}/{destination.Branch}";

                    if (!d.TryGetValue(orb, out var items))
                    {
                        items = new List<Tuple<Parts, Parts>>();
                        d.Add(orb, items);
                    }

                    items.Add(new Tuple<Parts, Parts>(destination, source));
                }
            }

            return d;
        }
    }
}