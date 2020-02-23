namespace Config
{
    public class Template
    {
        public Template()
        {
            Branch = "master";
        }

        public string Name { get; set; }

        public string Url { get; set; }

        public string Branch { get; set; }

        public override string ToString()
        {
            return $"{Name} ({Url})";
        }
    }
}