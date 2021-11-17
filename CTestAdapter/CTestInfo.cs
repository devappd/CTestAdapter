namespace CTestAdapter
{
    public class CTestInfo
    {
        public Backtracegraph backtraceGraph { get; set; }
        public string kind { get; set; }
        public Test[] tests { get; set; }
        public Version version { get; set; }
    }

    public class Backtracegraph
    {
        public string[] commands { get; set; }
        public string[] files { get; set; }
        public Node[] nodes { get; set; }
    }

    public class Node
    {
        public int file { get; set; }
        public int command { get; set; }
        public int line { get; set; }
        public int parent { get; set; }
    }

    public class Version
    {
        public int major { get; set; }
        public int minor { get; set; }
    }

    public class Test
    {
        public int backtrace { get; set; }
        public string[] command { get; set; }
        public string name { get; set; }
        public Property1[] properties { get; set; }
    }

    public class Property1
    {
        public string name { get; set; }
        public object value { get; set; }
    }
}
