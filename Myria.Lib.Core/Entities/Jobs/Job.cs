namespace Myria.Lib.Core.Entities.Jobs
{
    public class Job
    {
        public string Id   { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Type { get; set; } = "";  // e.g. "Gathering", "Crafting"
    }
}
