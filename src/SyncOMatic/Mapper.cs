namespace SyncOMatic
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    public class Mapper : MapperBase
    {
        public Mapper Add(Parts from, Parts to)
        {
            Add_Internal(from, to);
            return this;
        }

        public Mapper Add(Parts from, params Parts[] tos)
        {
            foreach (var to in tos)
            {
                Add_Internal(from, to);
            }

            return this;
        }
    }

    public class Diff : MapperBase
    {
        internal Diff()
        { }

        internal void Add(Parts from, Parts to)
        {
            Add_Internal(from, to);
        }
    }

    public abstract class MapperBase : IEnumerable<KeyValuePair<Parts, IEnumerable<Parts>>>
    {
        private Dictionary<Parts, List<Parts>> dic = new Dictionary<Parts, List<Parts>>();

        protected void Add_Internal(Parts from, Parts to)
        {
            if (from.Type != to.Type)
            {
                throw new ArgumentException(string.Format("Cannot map [{0}: {1}] to [{2}: {3}]. ", from.Type, from.Url, to.Type, to.Url));
            }

            List<Parts> l;

            if (!dic.TryGetValue(from, out l))
            {
                l = new List<Parts>();
                dic.Add(from, l);
            }

            l.Add(to);
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
                List<Parts> l;

                if (!dic.TryGetValue(from, out l))
                {
                    return Enumerable.Empty<Parts>();
                }

                return l;
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
                    var orb = destination.Owner + "/" + destination.Repository + "/" + destination.Branch;

                    IList<Tuple<Parts, Parts>> items;

                    if (!d.TryGetValue(orb, out items))
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
