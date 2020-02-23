namespace Config
{
    using System.Collections.Generic;

    internal class Repository
    {
        public Repository()
        {
            Branch = "master";

            Templates = new List<string>();
        }

        public string Name { get; set; }

        public string Url { get; set; }

        public string Branch { get; set; }

        public bool AutoMerge { get; set; }

        public List<string> Templates { get; private set; }

        public override string ToString()
        {
            return $"{Name} ({Url})";
        }
    }
}